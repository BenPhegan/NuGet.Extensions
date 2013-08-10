namespace NuGet.Extras.Packages
{
    /// <summary>
    /// Provides package resolution services.
    /// </summary>
    public interface IPackageResolutionManager
    {
        /// <summary>
        /// Resolves a package from either a local repository (typically the .\packages directory), or an AggregateRepository (if one is used).
        /// If the AggregateRepository includes a DataServicePackageRepository, attempts are made to resolve packages from in-memory caches that are built based on previous requests.
        /// To ensure caching across more than one PackageManager, both SourceRepository and LocalRepository need to be passed in per resolution.
        /// </summary>
        /// <param name="localRepository">Generally reflects a .\packages directory, such as PackageManager.SourceRepository</param>
        /// <param name="sourceRepository">Generally an AggregateRepository from a PackageManager.SourceRepository</param>
        /// <param name="packageId">The Id to resolve.</param>
        /// <param name="version">The Version to resolve</param>
        /// <param name="allowPrereleaseVersions">Allow Pre-release packages.</param>
        /// <returns></returns>
        IPackage ResolvePackage(IPackageRepository localRepository, IPackageRepository sourceRepository, string packageId, SemanticVersion version, bool allowPrereleaseVersions);

        /// <summary>
        /// Searches for a specific package on any LocalPackageRepository objects within an AggregateRepository or LocalPackageRepository.
        /// </summary>
        /// <param name="localRepository">Generally reflects a .\packages directory, such as PackageManager.SourceRepository</param>
        /// <param name="sourceRepository">Generally an AggregateRepository from a PackageManager.SourceRepository</param>
        /// <param name="package">The IPackage to check for.</param>
        /// <param name="allowPrereleaseVersions">Allow pre-release.</param>
        /// <param name="allowUnlisted">Allow unlisted.</param>
        /// <returns>Any IPackage found first in the localRepository, failing that the LocalRepository Repositories in the sourceRepository.</returns>
        IPackage FindPackageInAllLocalSources(IPackageRepository localRepository, IPackageRepository sourceRepository, IPackage package, bool allowPrereleaseVersions = false, bool allowUnlisted = false);

        /// <summary>
        /// Checks for a package, will only use DataServicePackageRepository objects (if provided an AggregateRepository, it will select just the remote repositories).  
        /// Results in expensive network/OData calls.
        /// Eventually it would be nice to replace this because we could query OData for a specific version.
        /// </summary>
        /// <param name="sourceRepository"></param>
        /// <param name="packageId"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        IPackage FindPackageInRemoteSources(IPackageRepository sourceRepository, string packageId, SemanticVersion version);

        /// <summary>
        /// Resolves the installable version.
        /// </summary>
        /// <param name="remoteRepository"> </param>
        /// <param name="packageReference">The package.</param>
        /// <returns></returns>
        SemanticVersion ResolveInstallableVersion(IPackageRepository remoteRepository, PackageReference packageReference);

        /// <summary>
        /// Resolves the installable version.
        /// </summary>
        /// <param name="remoteRepository"> </param>
        /// <param name="packageReference"> </param>
        /// <returns></returns>
        IPackage ResolveLatestInstallablePackage(IPackageRepository remoteRepository, PackageReference packageReference);
    }
}