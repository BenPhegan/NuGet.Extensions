using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NuGet.Extras.Repositories
{
    /// <summary>
    /// Implements IRepositoryManager
    /// </summary>
    public class RepositoryManager : IRepositoryManager
    {
        /// <summary>
        /// Gets the repository config file details.
        /// </summary>
        public FileInfo RepositoryConfig { get; private set; }
        IFileSystem fileSystem;

        /// <summary>
        /// Gets the package reference files.
        /// </summary>
        public IEnumerable<PackageReferenceFile> PackageReferenceFiles { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryManager"/> class.
        /// </summary>
        /// <param name="repositoryConfig">The repository.config file to parse.</param>
        /// <param name="repositoryEnumerator">The repository enumerator.</param>
        /// <param name="fileSystem"> </param>
        /// <example>Can be a direct path to a repository.config file</example>
        ///   
        /// <example>Can be a path to a directory, which will recursively locate all contained repository.config files</example>
        public RepositoryManager(string repositoryConfig, IRepositoryEnumerator repositoryEnumerator, IFileSystem fileSystem)
        {
            Contract.Requires(fileSystem != null);
            this.fileSystem = fileSystem;

            if (fileSystem.FileExists(repositoryConfig) && repositoryConfig.EndsWith("repositories.config"))
                RepositoryConfig = new FileInfo(fileSystem.GetFullPath(repositoryConfig));
            else
                throw new ArgumentOutOfRangeException("repository");

            PackageReferenceFiles = repositoryEnumerator.GetPackageReferenceFiles(RepositoryConfig);// GetPackageReferenceFiles();
        }
    }
}
