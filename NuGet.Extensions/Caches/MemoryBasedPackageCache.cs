using System.Collections.Generic;
using System.Linq;
using NuGet.Extensions.Caches;

namespace NuGet.Extensions.Caches
{
    /// <summary>
    /// Implements a simple package cache in memory.
    /// </summary>
    public class MemoryBasedPackageCache : IPackageCache
    {
        private readonly ILogger _console;
        
        private readonly Dictionary<string, List<IPackage>> _fullVersionPackageCache = new Dictionary<string, List<IPackage>>();
        private readonly Dictionary<string, IPackage> _latestPackageCache = new Dictionary<string, IPackage>();
        private readonly Dictionary<string, Dictionary<IVersionSpec, IPackage>> _latestPackageConstraintCache = new Dictionary<string, Dictionary<IVersionSpec, IPackage>>();

        /// <summary>
        /// Creates an instance of a MemoryBasedPackageCache.
        /// </summary>
        /// <param name="console"></param>
        public MemoryBasedPackageCache(ILogger console)
        {
            _console = console;
        }

        /// <summary>
        /// Attempts to return an already resolved Id/VersionConstraint pair from the cache.  Attempts to prevent unecessary network calls.
        /// </summary>
        /// <param name="packageReference"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        public bool TryCacheHitByVersionConstraint(PackageReference packageReference, out IPackage package)
        {
            if (_latestPackageConstraintCache.ContainsKey(packageReference.Id))
            {
                if (_latestPackageConstraintCache[packageReference.Id].ContainsKey(packageReference.VersionConstraint))
                {
                    package = _latestPackageConstraintCache[packageReference.Id][packageReference.VersionConstraint];
                    _console.Log(MessageLevel.Info, "Using cached latest constrained version : {0} {1} using constraint {2}",
                                package.Id, package.Version.ToString(), packageReference.VersionConstraint);
                    return true;
                }
            }
            package = null;
            return false;
        }

        /// <summary>
        /// Checks to see if we have already retrieved a Latest version for a particular Package ID.  Attempts to avoid unecessary network calls.
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        public bool TryCacheHitByIsLatest(string packageId, out IPackage package)
        {
            if (_latestPackageCache.ContainsKey(packageId))
            {
                package = _latestPackageCache[packageId];
                _console.Log(MessageLevel.Info, "Using cached latest version : {0} {1}", package.Id, package.Version.ToString());
                return true;
            }
            package = null;
            return false;
        }


        /// <summary>
        /// Tries to find a particular package Id and version combination from a cache of IPackages returned from DataServicePackageRepository queries.
        /// Attempts to limit the number of OData queries that return all versions for a particular Id.
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="version"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        public bool TryCacheHitByVersion(string packageId, SemanticVersion version, out IPackage package)
        {
            package = null;
            if (_fullVersionPackageCache.ContainsKey(packageId.ToLowerInvariant()))
            {
                IPackage cachedPackage = _fullVersionPackageCache[packageId.ToLowerInvariant()].FirstOrDefault(p => p.Version == version);
                if (cachedPackage != null)
                {
                    package = cachedPackage;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds a list of resolved packages against a single package ID.  Usually used to cache a query that is simply by ID.
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="packages"></param>
        /// <returns></returns>
        public bool AddCacheEntryByList(string packageId, IEnumerable<IPackage> packages)
        {
            if (!_fullVersionPackageCache.ContainsKey(packageId.ToLowerInvariant()))
            {
                _fullVersionPackageCache.Add(packageId.ToLowerInvariant(), packages.ToList().OrderByDescending(p => p.Version).ToList());
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a cache entry when you are checking for the latest version of a package.  Will be cached as the latest for that specific package until the cache is thrown away.
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public bool AddCacheEntryByIsLatest(IPackage package)
        {
            if (!_latestPackageCache.ContainsKey(package.Id))
            {
                _latestPackageCache.Add(package.Id, package);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a package that has been resolved as the latest within a constraint.  Saves doing the math and resolution again.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="constraint"></param>
        /// <returns></returns>
        public bool AddCacheEntryByConstraint(IPackage package, IVersionSpec constraint)
        {
            if (!_latestPackageConstraintCache.ContainsKey(package.Id))
                _latestPackageConstraintCache.Add(package.Id, new Dictionary<IVersionSpec, IPackage>());

            if (!_latestPackageConstraintCache[package.Id].ContainsKey(constraint))
            {
                _latestPackageConstraintCache[package.Id].Add(constraint, package);
                return true;
            }
            return false;

        }
    }
}
