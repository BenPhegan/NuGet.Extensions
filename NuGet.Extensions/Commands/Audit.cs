using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Extensions.FeedAudit;

namespace NuGet.Extensions.Commands
{
    [Command("audit","Checks a single package or all the packages on a feed to ensure that they depend on the right packages.")]
    public class Audit : Command
    {
        private readonly IPackageRepositoryFactory _factory;
        private readonly IPackageSourceProvider _provider;
        private readonly List<string> _sources = new List<string>();

        [Option("A list of sources to search")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option("Fail only on possible runtime assembly bind failure (unresolved assembly dependency)", AltName = "r")]
        public bool RunTimeFailOnly { get; set; }

        [Option("Semi-colon delimited set of package IDs or wildcards that you do NOT want to audit.", AltName = "x")]
        public string Exceptions { get; set; }

        [Option("Output filename", AltName = "o")]
        public string Output { get; set; }

        [Option("Package to audit (will check locally for file before checking feed)", AltName = "p")]
        public string Package { get; set; }

        [Option("Include unlisted packages", AltName = "u")]
        public Boolean Unlisted { get; set; }

        [Option("Check GAC for unresolved assemblies", AltName = "g")]
        public Boolean Gac { get; set; }

        [Option("Check the feed for unresolved assemblies (expensive)", AltName = "cu")]
        public Boolean CheckFeedForUnresolvedAssemblies { get; set; }

        [Option("Output filename for feed unresolved assemblies", AltName = "uo")]
        public string UnresolvedOutput { get; set; }

        [Option("Verbose output", AltName = "v")]
        public bool Verbose { get; set; }

        [ImportingConstructor]
        public Audit(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            _factory = packageRepositoryFactory;
            _provider = sourceProvider;
        }

        public override void ExecuteCommand()
        {
            var excludedPackageIds = GetLowerInvariantExcludedPackageIds();
            var excludedWildcards = GetExcludedWildcards(Exceptions);
            var repository = GetRepository();
            var feedAuditor = new FeedAuditor(repository, excludedPackageIds, excludedWildcards, Unlisted, CheckFeedForUnresolvedAssemblies, Gac);
            feedAuditor.StartPackageAudit += (o, e) => Console.WriteLine("Starting audit of package: {0}", e.Package.Id);
            feedAuditor.StartPackageListDownload += (o, e) => Console.WriteLine("Downloading package list...");
            feedAuditor.FinishedPackageListDownload += (o, e) => Console.WriteLine("Finished downloading package list...");
            feedAuditor.PackageIgnored += (o, e) => Console.WriteLine("Ignoring package: {0} based on {1}", e.IgnoredPackage.Id, e.Wildcard ? "wildcard..." : "string match...");
            List<PackageAuditResult> results = null;
            if (String.IsNullOrEmpty(Package))
                results = feedAuditor.Audit();
            else
            {
                var actualPackage = File.Exists(Package) ? new ZipPackage(Package) : repository.FindPackagesById(Package).FirstOrDefault();
                if (actualPackage != null)
                    results = feedAuditor.Audit(actualPackage);
                else
                    throw new ApplicationException(string.Format("Could not find package locally or on feed: {0}",Package));
            }
            var auditFlags = GetAuditFlags(RunTimeFailOnly, CheckFeedForUnresolvedAssemblies, Gac);
            var outputer = new FeedAuditResultsOutputManager(results, auditFlags);
            outputer.Output(string.IsNullOrEmpty(Output) ? System.Console.Out : new StreamWriter(Path.GetFullPath(Output)));

            if (CheckFeedForUnresolvedAssemblies && feedAuditor.UnresolvableAssemblyReferences.Count > 0)
            {
                var writer = !String.IsNullOrEmpty(UnresolvedOutput) ? new StreamWriter(Path.GetFullPath(UnresolvedOutput)) : System.Console.Out;
                writer.WriteLine("Following unresolvable references could not be found on the feed...");
                writer.WriteLine();
                foreach (var assembly in feedAuditor.UnresolvableAssemblyReferences)
                {
                    writer.WriteLine("\t{0}", assembly.FullName);
                }
                //TODO this is pretty ugly, perhaps need to look at what we are providing as part of the UnresolvableAssemblyReferences
                //HACK the code below is duplicated in two places, ugly.
                if (Verbose)
                    outputer.OutputUnresolvableReferences(!String.IsNullOrEmpty(UnresolvedOutput) ? new StreamWriter(Path.GetFullPath(UnresolvedOutput)) : System.Console.Out);           
            }

            if (RunTimeFailOnly ? CheckFeedForUnresolvedAssemblies && Gac ? feedAuditor.UnresolvableAssemblyReferences.Count > 0 : CheckPossibleRuntimeFailures(results) : CheckAllPossibleFailures(results))
                throw new CommandLineException("There were audit failures, please check audit report");
        }

        private IEnumerable<Regex> GetExcludedWildcards(string exceptions)
        {
            var wildcards = exceptions.Split(';').Select(s => s.ToLowerInvariant());
            return new List<Regex>(wildcards.Select(w => new Wildcard(w)));
        }

        private static AuditEventTypes GetAuditFlags(bool runTimeOnly, bool checkFeedResolvable, bool gac)
        {
            var events = (AuditEventTypes) 0;
            if (runTimeOnly || checkFeedResolvable || gac)
            {
                if (runTimeOnly)
                    events |= AuditEventTypes.UnresolvedAssemblyReferences;
                if (checkFeedResolvable)
                    events |= AuditEventTypes.FeedResolvableReferences;
                if (gac)
                    events |= AuditEventTypes.GacResolvableReferences;
                return events;
            }
            return AuditEventTypes.ResolvedAssemblyReferences
                   | AuditEventTypes.UnloadablePackageFiles
                   | AuditEventTypes.UnresolvedAssemblyReferences
                   | AuditEventTypes.UnresolvedDependencies
                   | AuditEventTypes.UnusedPackageDependencies
                   | AuditEventTypes.UsedPackageDependencies;
        }

        private static bool CheckPossibleRuntimeFailures(IEnumerable<PackageAuditResult> results)
        {
            return results.Any(r => r.UnresolvedAssemblyReferences.Any());
        }

        private static bool CheckAllPossibleFailures(IEnumerable<PackageAuditResult> results)
        {
            return results.Any(r => r.UnloadablePackageFiles.Any()
                                                     || r.UnresolvedAssemblyReferences.Any()
                                                     || r.UnresolvedDependencies.Any()
                                                     || r.UnusedPackageDependencies.Any());
        }

        private IEnumerable<string> GetLowerInvariantExcludedPackageIds()
        {
            var exceptions = new List<string>();
            if (!String.IsNullOrEmpty(Exceptions))
            {
                exceptions.AddRange(Exceptions.Split(';').Select(s => s.ToLowerInvariant()));
            }
            return exceptions.Where(e => !e.Contains('*') && !e.Contains('?'));
        }

        private IPackageRepository GetRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(_factory, _provider, Source);
            repository.Logger = Console;
            return repository;
        }
    }
}
