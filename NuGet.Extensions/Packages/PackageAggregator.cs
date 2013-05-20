using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using NuGet.Extras.Repositories;
using NuGet.Extras.PackageReferences;
using NuGet.Extras.Comparers;
using NuGet.Extras.ExtensionMethods;

namespace NuGet.Extras.Packages
{
    /// <summary>
    /// Manages the saving of the aggregated packages.config file.
    /// </summary>
    public class PackageAggregator : IDisposable
    {
        private readonly IRepositoryManager _repositoryManager;
        private readonly IPackageEnumerator _packageEnumerator;
        private readonly bool _autoDelete;
        private IEnumerable<PackageReference> _packages;
        private IEnumerable<PackageReference> _packagesResolveFailures;
        private FileInfo _fileInfo;
        private IFileSystem _fileSystem;


        /// <summary>
        /// Creates a new instance of a PackageAggregator
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="repositoryManager"></param>
        /// <param name="packageEnumerator"></param>
        /// <param name="autoDelete"></param>
        public PackageAggregator(IFileSystem fileSystem, IRepositoryManager repositoryManager, IPackageEnumerator packageEnumerator, bool autoDelete = false)
        {
            _repositoryManager = repositoryManager;
            _packageEnumerator = packageEnumerator;
            _autoDelete = autoDelete;
            _packages = new List<PackageReference>();
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Gets the repository manager.
        /// </summary>
        public IRepositoryManager RepositoryManager
        {
            get { return _repositoryManager; }
        }

        /// <summary>
        /// Gets the packages.
        /// </summary>
        public IEnumerable<PackageReference> Packages
        {
            get { return _packages; }
        }

        /// <summary>
        /// Gets the Package resolution failures.
        /// </summary>
        public IEnumerable<PackageReference> PackageResolveFailures
        {
            get { return _packagesResolveFailures; }
        }

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_fileInfo != null)
                if (_fileInfo.Exists && _autoDelete)
                    _fileInfo.Delete();
        }

        #endregion


        /// <summary>
        /// Computes the list of PackageReference objects, based on the type of IPackageReferenceEqualityComparer passed in.
        /// </summary>
        /// <param name="logCount">How to log.</param>
        /// <param name="comparer">Provides the comparer used to get the distinct list of package references.</param>
        /// <param name="resolver">A resolver used to resolve the set of possible packages.</param>
        public void Compute(Action<string, string> logCount, PackageReferenceEqualityComparer comparer, IPackageReferenceSetResolver resolver)
        {
            _packages = _packageEnumerator.GetPackageReferences(_repositoryManager.PackageReferenceFiles, logCount, comparer);

            //TODO not sure this is correct...
            var returnLists = resolver.Resolve(_packages);
            _packages = returnLists.Item1;
            _packagesResolveFailures = returnLists.Item2;
        }

        /// <summary>
        /// Saves the packages to a packages.config file in the specified directory.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public FileInfo Save(string directory)
        {
            string fileName = Path.Combine(directory, "packages.config");
            XDocument xml = CreatePackagesConfigXml(_packages);
            _fileSystem.AddFile(fileName, xml.Save);
            _fileInfo = new FileInfo(fileName);
            return _fileInfo;
        }

        /// <summary>
        /// Saves the packages to a packages.config file in the temp directory.
        /// </summary>
        /// <returns></returns>
        public FileInfo Save()
        {
            return Save(Path.GetTempPath() + Guid.NewGuid().ToString());
        }

        private XDocument CreatePackagesConfigXml(IEnumerable<PackageReference> packages)
        {
            var doc = new XDocument();
            var packagesElement = new XElement("packages");

            foreach (PackageReference p in packages)
            {
                var packageXml = new XElement("package");
                packageXml.SetAttributeValue("id", p.Id);
                packageXml.SetAttributeValue("version", p.Version);
                if (p.VersionConstraint != null)
                    packageXml.SetAttributeValue("allowedVersions", p.VersionConstraint.ToString());
                packagesElement.Add(packageXml);
            }

            doc.Add(packagesElement);
            return doc;
        }
    }
}