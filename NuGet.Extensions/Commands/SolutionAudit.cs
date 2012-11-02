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
    [Command("solutionaudit","Audits a solution against its stated packages.config NuGet dependencies.")]
    public class SolutionAudit : Command
    {
        private readonly IPackageRepositoryFactory _factory;
        private readonly IPackageSourceProvider _provider;
        private readonly List<string> _sources = new List<string>();

        [Option("A list of sources to search")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option("Solution to audit", AltName = "p")]
        public string Solution { get; set; }

        [ImportingConstructor]
        public SolutionAudit(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            _factory = packageRepositoryFactory;
            _provider = sourceProvider;
        }

        public override void ExecuteCommand()
        {
            if (String.IsNullOrEmpty(Solution))
                throw new ArgumentException("Solution is a required argument.");

            var repository = GetRepository();

            //First, check the solution output files

            //Then, get their metadata (runtime requirements) per project

            //Then, compare against that projects packages.config.
            
        }

        private IPackageRepository GetRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(_factory, _provider, Source);
            repository.Logger = Console;
            return repository;
        }
    }
}
