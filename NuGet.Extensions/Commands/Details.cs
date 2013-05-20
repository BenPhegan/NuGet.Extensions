using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using NuGet.Commands;
using NuGet.Common;

namespace NuGet.Extensions.Commands
{
    [Command("details", "Lists details of a packages or packages on a feed.", MinArgs = 0)]
    public class Details : Command
    {
        private const int _indent = 25;
        private readonly List<string> _sources = new List<string>();

        [Option("A list of sources to search")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option("Check all versions, by default only the latest will be searched")]
        public bool AllVersions { get; set; }

        [Option("Don't show file details")]
        public bool NoFiles { get; set; }

        [Option("Don't show author details")]
        public bool NoAuthors { get; set; }

        [Option("Don't show dependency details")]
        public bool NoDependencies { get; set; }

        [ImportingConstructor]
        public Details(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            Contract.Assert(packageRepositoryFactory != null);
            Contract.Assert(sourceProvider != null);

            RepositoryFactory = packageRepositoryFactory;
            SourceProvider = sourceProvider;
        }

        public override void ExecuteCommand()
        {
            //Working on a single package...

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var repository = GetRepository();
            IQueryable<IPackage> packageSource = GetPackageList(repository);
            var packages = FilterPackageList(packageSource);

            foreach (var package in packages)
            {
                LogDetails(package);
            }

            Console.WriteLine();
            sw.Stop();
            OutputElapsedTime(sw);
            Environment.Exit(0);
        }

        private void LogDetails(IPackage package)
        {
            Console.WriteLine();
            Console.WriteLine("Package ID:".PadRight(_indent) + string.Format("{0}", package.Id));
            Console.WriteLine("Package Version:".PadRight(_indent) + string.Format("{0}", package.Version));
            Console.WriteLine("Package Description:".PadRight(_indent) + string.Format("{0}", package.Description));
            if (!string.IsNullOrEmpty(package.Tags)) Console.WriteLine("Package Tags:".PadRight(_indent - 1) + string.Format("{0}", package.Tags));
            if (!NoAuthors) OutputAuthors(package);
            if (!NoFiles) OutputContent(package);
            if (!NoDependencies) OutputDependencies(package);
            Console.WriteLine();
        }

        private void OutputDependencies(IPackageMetadata package)
        {
            if (package.DependencySets.Any())
            {
                Console.WriteLine("Package Dependencies:");
                foreach (var dependencySet in package.DependencySets)
                {
                    foreach (var dependency in dependencySet.Dependencies)
                    {
                        Console.WriteLine("".PadLeft(_indent) + string.Format("{0} - {1} {2}",dependencySet.TargetFramework, dependency.Id, dependency.VersionSpec));
                    }
                }
            }
        }

        private void OutputContent(IPackage package)
        {
            Console.WriteLine("Package Content:");
            foreach (var file in package.GetFiles())
            {
                Console.WriteLine("".PadLeft(_indent) + string.Format("{0}", file.Path));
            }
        }

        private void OutputAuthors(IPackage package)
        {
            if (package.Authors.Any())
            {
                Console.WriteLine("Package Authors:");
                foreach (var author in package.Authors)
                {
                    Console.WriteLine("".PadRight(_indent) + string.Format("{0}", author));
                }
            }
        }

        private IEnumerable<IPackage> FilterPackageList(IQueryable<IPackage> packageSource)
          {
              IQueryable<IPackage> packages;
              if (Arguments.Count > 0 && !string.IsNullOrEmpty(Arguments[0]))
              {
                  packages = packageSource.Where(p => p.Id.Equals(Arguments[0], StringComparison.OrdinalIgnoreCase));
              }
              else
              {
                  packages = packageSource.OrderBy(p => p.Id);
              }
              return packages;
          }

          private IQueryable<IPackage> GetPackageList(IPackageRepository repository)
        {
            IQueryable<IPackage> packages;
            if (AllVersions)
            {
                packages = repository.GetPackages();
            }
            else
            {
                packages = repository.GetPackages().Where(p => p.IsLatestVersion);
            }
            return packages;
        }

        private void OutputElapsedTime(Stopwatch sw)
        {
            Console.WriteLine("Completed search in {0} seconds", sw.Elapsed.TotalSeconds);
        }
        private IPackageRepository GetRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;
            return repository;
        }

        protected virtual IFileSystem CreateFileSystem(string root)
        {
            return new PhysicalFileSystem(root);
        }
    }
}
