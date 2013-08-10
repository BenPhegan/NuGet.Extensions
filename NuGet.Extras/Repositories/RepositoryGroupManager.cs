using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Extras.ExtensionMethods;

namespace NuGet.Extras.Repositories
{
    /// <summary>
    /// Provides functionality across multiple repositories.
    /// </summary>
    public class RepositoryGroupManager
    {
        /// <summary>
        /// The managed set of RepositoryManagers.
        /// </summary>
        public IEnumerable<RepositoryManager> RepositoryManagers { get; private set; }
        IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryGroupManager"/> class.  Provides nested operations over RepositoryManager instances.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="fileSystem"> </param>
        /// <example>Can be a direct path to a repository.config file</example>
        ///   
        /// <example>Can be a path to a directory, which will recursively locate all contained repository.config files</example>
        public RepositoryGroupManager(string repository, IFileSystem fileSystem)
        {
            Contract.Requires(fileSystem != null);
            this.fileSystem = fileSystem;
            
            if (fileSystem.DirectoryExists(repository))
            {
                // we're dealing with a directory
                //TODO Does this by default do a recursive???
                RepositoryManagers = new ConcurrentBag<RepositoryManager>(fileSystem.GetFiles(repository, "repositories.config", SearchOption.AllDirectories).Select(file => new RepositoryManager(file, new RepositoryEnumerator(fileSystem), fileSystem)).ToList());
            }
            else if (fileSystem.FileExists(repository) && Path.GetFileName(repository) == "repositories.config")
                RepositoryManagers = new ConcurrentBag<RepositoryManager>(new List<RepositoryManager> { new RepositoryManager(repository, new RepositoryEnumerator(fileSystem), fileSystem) });
            else
                throw new ArgumentOutOfRangeException("repository");
        }

        /// <summary>
        /// Installs the packages.
        /// </summary>
        public void InstallPackages()
        {

        }
    }
}
