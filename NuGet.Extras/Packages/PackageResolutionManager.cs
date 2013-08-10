using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Extras.Caches;
using NuGet.Extras.Comparers;
using NuGet.Extras.ExtensionMethods;

namespace NuGet.Extras.Packages
{
    /// <summary>
    /// Provides package resolution and caching services over PackageRepositories.
    /// </summary>
    public class PackageResolutionManager : IPackageResolutionManager
    {
        private readonly ILogger Console;
        private readonly Boolean Latest;
        private IPackageCache _cache;

        /// <summary>
        /// Creates a new PackageResolutionManager.
        /// </summary>
        /// <param name="console"></param>
        /// <param name="latest"></param>
        /// <param name="cache"> </param>
        public PackageResolutionManager(ILogger console, Boolean latest, IPackageCache cache)
        {
            Console = console;
            Latest = latest;
            _cache = cache;
        }

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
        public IPackage ResolvePackage(IPackageRepository localRepository, IPackageRepository sourceRepository, string packageId, SemanticVersion version, bool allowPrereleaseVersions)
        {
            return ResolvePackage(localRepository, sourceRepository, constraintProvider: NullConstraintProvider.Instance, packageId: packageId, version: version,
                                  allowPrereleaseVersions: allowPrereleaseVersions);
        }

        private IPackage ResolvePackage(IPackageRepository localRepository, IPackageRepository sourceRepository, IPackageConstraintProvider constraintProvider,
                                       string packageId, SemanticVersion version, bool allowPrereleaseVersions)
        {
            if (String.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException("Argument cannot be null or empty: {0}", "packageId");
            }

            IPackage package = null;

            // If we're looking for an exact version of a package then try local first (where local is typically the target .\packages directory)
            if (version != null)
            {
                package = localRepository.FindPackage(packageId, version, allowPrereleaseVersions, allowUnlisted: true);
            }

            //Check the object cache first, else fall back to on disk cache then remote call
            if (package == null && !_cache.TryCacheHitByVersion(packageId, version, out package))
            {
                //Check the cache first by splitting the AggregateRepository into local/remote and querying seperately
                package = package ?? FindPackageInAggregateLocalSources(sourceRepository, constraintProvider, packageId, version, allowPrereleaseVersions);

                //If we are still null, check the remote for the package....
                package = package ?? FindPackageInRemoteSources(sourceRepository, packageId, version);
            }

            //Not sure if this is still necessary, as we already know the version....
            if (package != null)
            {
                package = localRepository.FindPackage(package.Id, package.Version, allowPrereleaseVersions, allowUnlisted: true) ?? package;
            }


            // We still didn't find it so throw
            if (package == null)
            {
                if (version != null)
                {
                    throw new InvalidOperationException( String.Format("Unknown Package Version: {0} {1}", packageId, version));
                }
                throw new InvalidOperationException(String.Format("Unknown Package Id: {0}", packageId));
            }

            return package;
        }


        /// <summary>
        /// Searches for a specific package on any LocalPackageRepository objects within an AggregateRepository or LocalPackageRepository.
        /// </summary>
        /// <param name="localRepository">Generally reflects a .\packages directory, such as PackageManager.SourceRepository</param>
        /// <param name="sourceRepository">Generally an AggregateRepository from a PackageManager.SourceRepository</param>
        /// <param name="package">The IPackage to check for.</param>
        /// <param name="allowPrereleaseVersions">Allow pre-release.</param>
        /// <param name="allowUnlisted">Allow unlisted.</param>
        /// <returns>Any IPackage found first in the localRepository, failing that the LocalRepository Repositories in the sourceRepository.</returns>
        public IPackage FindPackageInAllLocalSources(IPackageRepository localRepository, IPackageRepository sourceRepository, IPackage package, bool allowPrereleaseVersions = false, bool allowUnlisted = false)
        {
            IPackage cachedPackage = localRepository.FindPackage(package.Id, package.Version, allowPrereleaseVersions, allowUnlisted);
            cachedPackage = cachedPackage ?? FindPackageInAggregateLocalSources(sourceRepository, NullConstraintProvider.Instance, package.Id, package.Version, allowPrereleaseVersions);
            return cachedPackage;
        }


        /// <summary>
        /// Checks for a package, will only use DataServicePackageRepository objects (if provided an AggregateRepository, it will select just the remote repositories).  
        /// Results in expensive network/OData calls.
        /// Eventually it would be nice to replace this because we could query OData for a specific version.
        /// </summary>
        /// <param name="sourceRepository"></param>
        /// <param name="packageId"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public IPackage FindPackageInRemoteSources(IPackageRepository sourceRepository, string packageId, SemanticVersion version)
        {
            IPackage package = null;
            var aggregate = sourceRepository as AggregateRepository;
            if (aggregate != null)
            {
                package = FindPackage(aggregate.GetRemoteOnlyAggregateRepository(), packageId, version, null, false, false);
            }
            return package;
        }

        private IPackage FindPackage(IPackageRepository repository, string packageId, SemanticVersion version, IPackageConstraintProvider constraintProvider,
                                                 bool allowPrereleaseVersions, bool allowUnlisted)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            if (packageId == null)
            {
                throw new ArgumentNullException("packageId");
            }

            // if an explicit version is specified, disregard the 'allowUnlisted' argument
            // and always allow unlisted packages.
            if (version != null)
            {
                allowUnlisted = true;
            }

            IEnumerable<IPackage> packages = repository.FindPackagesById(packageId);

            _cache.AddCacheEntryByList(packageId, packages);


            if (!allowUnlisted)
            {
                packages = packages.Where(PackageExtensions.IsListed);
            }

            if (version != null)
            {
                packages = packages.Where(p => p.Version == version);
            }
            else if (constraintProvider != null)
            {
                packages = FilterPackagesByConstraints(constraintProvider, packages, packageId, allowPrereleaseVersions);
            }

            return packages.FirstOrDefault();
        }


        /// <summary>
        /// Stolen from NuGet
        /// </summary>
        /// <param name="constraintProvider"></param>
        /// <param name="packages"></param>
        /// <param name="packageId"></param>
        /// <param name="allowPrereleaseVersions"></param>
        /// <returns></returns>
        private static IEnumerable<IPackage> FilterPackagesByConstraints(
            IPackageConstraintProvider constraintProvider,
            IEnumerable<IPackage> packages,
            string packageId,
            bool allowPrereleaseVersions)
        {
            constraintProvider = constraintProvider ?? NullConstraintProvider.Instance;

            // Filter packages by this constraint
            IVersionSpec constraint = constraintProvider.GetConstraint(packageId);
            if (constraint != null)
            {
                packages = packages.FindByVersion(constraint);
            }
            if (!allowPrereleaseVersions)
            {
                packages = packages.Where(p => p.IsReleaseVersion());
            }

            return packages;
        }

        /// <summary>
        /// Will try and return a specific package from all provided local sources.  Will ignore OData feeds, and will not result in network calls.
        /// </summary>
        /// <param name="sourceRepository"></param>
        /// <param name="constraintProvider"></param>
        /// <param name="packageId"></param>
        /// <param name="version"></param>
        /// <param name="allowPrereleaseVersions"></param>
        /// <returns></returns>
        private static IPackage FindPackageInAggregateLocalSources(IPackageRepository sourceRepository,
                                                         IPackageConstraintProvider constraintProvider, string packageId,
                                                         SemanticVersion version, bool allowPrereleaseVersions)
        {
            IPackage package = null;
            var localAggregate = sourceRepository as AggregateRepository;
            if (localAggregate != null)
            {
                package = localAggregate.GetLocalOnlyAggregateRepository().FindPackage(packageId, version,
                                                                                       constraintProvider,
                                                                                       allowPrereleaseVersions,
                                                                                       allowUnlisted: false);
            }
            return package;
        }


        /// <summary>
        /// Resolves the installable version.
        /// </summary>
        /// <param name="remoteRepository"> </param>
        /// <param name="packageReference">The package.</param>
        /// <returns></returns>
        public SemanticVersion ResolveInstallableVersion(IPackageRepository remoteRepository, PackageReference packageReference)
        {
            IPackage package;
            if (Latest)
            {
                package = ResolveLatestInstallablePackage(remoteRepository, packageReference);
                return package != null ? package.Version : null;
            }

            Console.Log(MessageLevel.Info, "Using specific version : {0} {1}", packageReference.Id, packageReference.Version.ToString());
            return packageReference.Version;
        }

        /// <summary>
        /// Resolves the installable version.
        /// </summary>
        /// <param name="remoteRepository"> </param>
        /// <param name="packageReference"> </param>
        /// <returns></returns>
        public IPackage ResolveLatestInstallablePackage(IPackageRepository remoteRepository, PackageReference packageReference)
        {
            IPackage package = null;
            if (Latest)
            {
                //We can only work on an AggregateRepository, just return null if we dont get one.
                var aggregateRepository = remoteRepository as AggregateRepository;
                if (aggregateRepository == null) return null;

                var versionSpecComparer = new VersionSpecEqualityComparer(packageReference.VersionConstraint);
                var defaultVersionSpec = new VersionSpec();

                if (versionSpecComparer.Equals(defaultVersionSpec))
                {
                    //First, check to see if we have already checked for this ID, and if so, return the right version
                    if (_cache.TryCacheHitByIsLatest(packageReference.Id, out package)) return package;

                    //TODO need to check where we need to get the bools for this call....assuming a bit much here.
                    Console.Log(MessageLevel.Info, "Checking for latest package: {0}", packageReference.Id);


                    try
                    {
                        IPackage p = aggregateRepository.FindLatestPackage(packageReference.Id);
                        if (p != null)
                        {
                            Console.Log(MessageLevel.Info, "Using latest version : {0} {1}", p.Id, p.ToString());
                            //Only add if there is no constraint....
                            _cache.AddCacheEntryByIsLatest(p);
                            package = p;
                        }
                        else
                        {
                            Console.Log(MessageLevel.Info,
                                        "Latest version requested, however {0} cannot be found on feed.",
                                        packageReference.Id);
                            return null;
                        }
                    }
                    catch (NullReferenceException)
                    {
                        Console.Log(MessageLevel.Error,
                                    "One of the feeds threw an error, a package called {0} could not be found.",
                                    packageReference.Id);
                        return null;
                    }
                }

                else
                {
                    Console.Log(MessageLevel.Info, "Checking for latest package: {0} using constraint {1}",
                                packageReference.Id, packageReference.VersionConstraint);
                    if (_cache.TryCacheHitByVersionConstraint(packageReference, out package)) return package;

                    try
                    {
                        IPackage p = aggregateRepository.FindLatestPackage(packageReference.Id,
                                                                           packageReference.VersionConstraint);
                        if (p != null)
                        {
                            package = p;
                            _cache.AddCacheEntryByConstraint(package, packageReference.VersionConstraint);
                            Console.Log(MessageLevel.Info, "Using constrained version : {0} {1}, constrained by {2}",
                                        packageReference.Id, p.Version.ToString(), packageReference.VersionConstraint);
                        }
                        else
                        {
                            Console.Log(MessageLevel.Error,
                                        "Latest version requested, however {0} cannot be satisfied on feed using constraint {1}.",
                                        packageReference.Id, packageReference.VersionConstraint);
                            return null;
                        }
                    }
                    catch (NullReferenceException)
                    {
                        Console.Log(MessageLevel.Error,
                                    "One of the feeds threw an error, a package called {0} could not be found.",
                                    packageReference.Id);
                        return null;
                    }
                }
            }
            return package;
        }



 
    }
}