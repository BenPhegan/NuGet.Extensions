using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using NuGet.Commands;
using NuGet.Common;

namespace NuGet.Extensions.Commands
{
    [Command("fixreferences", "Fixes packages.config and project references by trying to ensure that a package reference can be found on a particular feed.  If not, an attempt will be made to adjust the version so it is found.")]
    public class FixReferences : Command
    {
        private readonly IFileSystem _fileSystem;
        private readonly ICollection<string> _sources = new Collection<string>();
        private IPackageRepository _repository;

        [Option("The directory to parse for packages.config")]
        public string Directory { get; set; }

        [Option("Fix the project reference HintPaths as well.")]
        public bool HintPaths { get; set; }

        [Option("Sources to use for version details.")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [ImportingConstructor]
        public FixReferences(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public FixReferences(IFileSystem fileSystem, IPackageRepository repository)
        {
            _fileSystem = fileSystem;
            _repository = repository;
        }

        public override void ExecuteCommand()
        {
            if (string.IsNullOrEmpty(Directory))
            {
                Directory = Environment.CurrentDirectory;
            }

            if (_repository == null)
            {
                _repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            }

            var packageFiles = _fileSystem.GetFiles(Directory, "packages.config", true).ToList();
            Parallel.ForEach(packageFiles, packageFile =>
                {
                    var newReferences = new List<PackageReference>();
                    var packageFileDetails = new PackageReferenceFile(_fileSystem, packageFile);
                    foreach (var packageReference in packageFileDetails.GetPackageReferences())
                    {
                        var exists = _repository.FindPackage(packageReference.Id, packageReference.Version);
                        if (exists != null)
                        {
                            newReferences.Add(packageReference);
                        }
                        else
                        {
                            var package = _repository.FindPackagesById(packageReference.Id).FirstOrDefault();
                            newReferences.Add(package != null ? new PackageReference(package.Id, package.Version, new VersionSpec(), new FrameworkName(".NET Framework, Version=4.0")) : packageReference);
                        }
                    }

                    //TODO Clear the file (must be an easier way).
                    foreach (var packageReference in packageFileDetails.GetPackageReferences())
                    {
                        packageFileDetails.DeleteEntry(packageReference.Id, packageReference.Version);
                    }

                    //Add the new references.
                    foreach (var packageReference in newReferences)
                    {
                        packageFileDetails.AddEntry(packageReference.Id, packageReference.Version);
                    }
                });
        }
    }
}
