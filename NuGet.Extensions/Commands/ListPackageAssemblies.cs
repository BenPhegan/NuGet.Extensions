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
using NuGet.Extras.Repositories;

namespace NuGet.Extensions.Commands
{
      [Command("listpackageassemblies", "Lists assemblies within a package or packages on a feed.", MinArgs = 0)]
    public class ListPackageAssemblies : Command
    {
        private readonly List<string> _sources = new List<string>();
        public IPackageRepositoryFactory RepositoryFactory { get; set; }
        public IPackageSourceProvider SourceProvider { get; set; }
        private RepositoryAssemblyResolver resolver;
        private IFileSystem fileSystem;

        [Option("A list of sources to search")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option("Check all versions, by default only the latest will be searched")]
        public bool AllVersions { get; set; }

        [ImportingConstructor]
        public ListPackageAssemblies(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            Contract.Assert(packageRepositoryFactory != null);
            Contract.Assert(sourceProvider != null);

            RepositoryFactory = packageRepositoryFactory;
            SourceProvider = sourceProvider;
        }

        public override void ExecuteCommand()
        {
            if (!string.IsNullOrEmpty(Arguments[0]))
            {
                //Working on a single package...

                Stopwatch sw = new Stopwatch();
                sw.Start();
                var repository = GetRepository();
                IQueryable<IPackage> packageSource = GetPackageList(repository);
                foreach (var package in packageSource.Where(p => p.Id.Equals(Arguments[0], StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("{0}", package.Id);
                    foreach (var file in package.GetFiles())
                    {
                        Console.WriteLine("     {0}",file.Path);
                    }
                }
                Console.WriteLine();
                sw.Stop();
                OutputElapsedTime(sw);
                Environment.Exit(0);

            }
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
