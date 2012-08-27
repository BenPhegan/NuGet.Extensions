using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Extensions.FeedAudit;

namespace NuGet.Extensions.Commands
{
    [Command("feedaudit","Checks the packages on a feed to ensure that they depend on the right packages.")]
    public class FeedAudit : Command
    {
        private readonly IPackageRepositoryFactory _factory;
        private readonly IPackageSourceProvider _provider;
        private readonly List<string> _sources = new List<string>();

        [Option("A list of sources to search")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option("Semi-colon delimited set of package IDs that you do NOT want to audit.", AltName = "x")]
        public string Exceptions { get; set; }

        [Option("Output filename", AltName = "o")]
        public string Output { get; set; }

        [ImportingConstructor]
        public FeedAudit(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            _factory = packageRepositoryFactory;
            _provider = sourceProvider;
        }

        public override void ExecuteCommand()
        {
            var excludedPackageIds = GetLowerInvariantExcludedPackageIds();
            var repository = GetRepository();
            var feedAuditor = new FeedAuditor(repository, excludedPackageIds);
            feedAuditor.AuditFeed();
            var outputer = new FeedAuditResultsOutputManager(feedAuditor.AuditResults);
            outputer.Output(string.IsNullOrEmpty(Output) ? System.Console.Out : new StreamWriter(Path.GetFullPath(Output)));

            if (AuditFailed(feedAuditor))
                throw new CommandLineException("There were audit failures, please check audit report");
        }

        private static bool AuditFailed(FeedAuditor feedAuditor)
        {
            return feedAuditor.AuditResults.Any(r => r.UnloadablePackageFiles.Any()
                                                     || r.UnresolvedAssemblyReferences.Any()
                                                     || r.UnresolvedDependencies.Any()
                                                     || r.UnusedPackageDependencies.Any());
        }

        private List<string> GetLowerInvariantExcludedPackageIds()
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
