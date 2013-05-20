using System;
using System.Collections.Generic;
using System.IO;
namespace NuGet.Extras.Repositories
{
    /// <summary>
    /// Provides a few services across a Repository
    /// </summary>
    public interface IRepositoryManager
    {
        /// <summary>
        /// Returns the full set of PackagesReferenceFiles
        /// </summary>
        IEnumerable<PackageReferenceFile> PackageReferenceFiles { get; }

        /// <summary>
        /// Provides the Repository.config file.
        /// </summary>
        FileInfo RepositoryConfig { get; }
    }
}
