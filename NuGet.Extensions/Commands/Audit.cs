using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
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

        [Option("Semi-colon delimited set of package IDs that you do NOT want to audit.", AltName = "x")]
        public string Exceptions { get; set; }

        [Option("Output filename", AltName = "o")]
        public string Output { get; set; }

        [Option("Package to audit (will check locally for file before checking feed)", AltName = "p")]
        public string Package { get; set; }

        [Option("Include unlisted packages", AltName = "u")]
        public Boolean Unlisted { get; set; }

        [ImportingConstructor]
        public Audit(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            _factory = packageRepositoryFactory;
            _provider = sourceProvider;
        }

        public override void ExecuteCommand()
        {
            var excludedPackageIds = GetLowerInvariantExcludedPackageIds();
            var repository = GetRepository();
            var feedAuditor = new FeedAuditor(repository, excludedPackageIds, Unlisted);
            feedAuditor.StartPackageAudit += (o, e) => Console.WriteLine("Starting audit of package: {0}", e.Package.Id);
            feedAuditor.StartPackageListDownload += (o, e) => Console.WriteLine("Downloading package list...");
            feedAuditor.FinishedPackageListDownload += (o, e) => Console.WriteLine("Finished downloading package list...");
            if (String.IsNullOrEmpty(Package))
                feedAuditor.Audit();
            else
            {
                var actualPackage = File.Exists(Package) ? new ZipPackage(Package) : repository.FindPackagesById(Package).FirstOrDefault();
                if (actualPackage != null)
                    feedAuditor.Audit(actualPackage);
                else
                    throw new ApplicationException(string.Format("Could not find package locally or on feed: {0}",Package));
            }
            var outputer = new FeedAuditResultsOutputManager(feedAuditor.AuditResults);
            outputer.Output(string.IsNullOrEmpty(Output) ? System.Console.Out : new StreamWriter(Path.GetFullPath(Output)));

            if (RunTimeFailOnly ? CheckPossibleRuntimeFailures(feedAuditor) : CheckAllPossibleFailures(feedAuditor))
                throw new CommandLineException("There were audit failures, please check audit report");
        }

        private static bool CheckPossibleRuntimeFailures(FeedAuditor feedAuditor)
        {
            return feedAuditor.AuditResults.Any(r => r.UnresolvedAssemblyReferences.Any());
        }

        private static bool CheckAllPossibleFailures(FeedAuditor feedAuditor)
        {
            return feedAuditor.AuditResults.Any(r => r.UnloadablePackageFiles.Any()
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
            return exceptions;
        }

        private IPackageRepository GetRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(_factory, _provider, Source);
            repository.Logger = Console;
            return repository;
        }
    }
}
