using System.Collections.Generic;
using System.IO;

namespace NuGet.Extensions.Repositories
{
    /// <summary>
    /// Allows enumeration of PackageReferenceFiles across a repository.
    /// </summary>
    public interface IRepositoryEnumerator
    {
        /// <summary>
        /// Gets the package reference files.
        /// </summary>
        /// <param name="repositoryConfig">The repository config.</param>
        /// <returns></returns>
        IEnumerable<PackageReferenceFile> GetPackageReferenceFiles(FileInfo repositoryConfig);
    }
}