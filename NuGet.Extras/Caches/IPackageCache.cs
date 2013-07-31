using System.Collections.Generic;

namespace NuGet.Extras.Caches
{
    /// <summary>
    /// Provides package caching to reduce network round trips on package resolution and download.
    /// </summary>
    public interface IPackageCache
    {
        /// <summary>
        /// Attempts to return an already resolved Id/VersionConstraint pair from the cache.  Attempts to prevent unecessary network calls.
        /// </summary>
        /// <param name="packageReference"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        bool TryCacheHitByVersionConstraint(PackageReference packageReference, out IPackage package);

        /// <summary>
        /// Checks to see if we have already retrieved a Latest version for a particular Package ID.  Attempts to avoid unecessary network calls.
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        bool TryCacheHitByIsLatest(string packageId, out IPackage package);

        /// <summary>
        /// Tries to find a particular package Id and version combination from a cache of IPackages returned from DataServicePackageRepository queries.
        /// Attempts to limit the number of OData queries that return all versions for a particular Id.
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="version"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        bool TryCacheHitByVersion(string packageId, SemanticVersion version, out IPackage package);

        /// <summary>
        /// Adds a list of resolved packages against a single package ID.  Usually used to cache a query that is simply by ID.
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="packages"></param>
        /// <returns></returns>
        bool AddCacheEntryByList(string packageId, IEnumerable<IPackage> packages);

        /// <summary>
        /// Adds a cache entry when you are checking for the latest version of a package.  Will be cached as the latest for that specific package until the cache is thrown away.
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        bool AddCacheEntryByIsLatest(IPackage package);

        /// <summary>
        /// Adds a package that has been resolved as the latest within a constraint.  Saves doing the math and resolution again.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="constraint"></param>
        /// <returns></returns>
        bool AddCacheEntryByConstraint(IPackage package, IVersionSpec constraint);
    }
}