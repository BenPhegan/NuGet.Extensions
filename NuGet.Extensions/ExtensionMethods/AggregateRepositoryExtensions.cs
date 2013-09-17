using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using NuGet.Extensions.ExtensionMethods;

namespace NuGet.Extensions.ExtensionMethods
{
    /// <summary>
    /// Provides a set of extension methods that extend the AggregateRepository.
    /// </summary>
    public static class AggregateRepositoryExtensions
    {

        /// <summary>
        /// Finds the latest package in a repository by Package Id
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="packageId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IPackage FindLatestPackage(this AggregateRepository repository, string packageId)
        {
            if (packageId == null)
            {
                throw new ArgumentNullException("packageId");
            }

            var localRepo = repository.GetLocalOnlyAggregateRepository();
            IPackage localPackage = null;
            if (localRepo != null)
                localPackage = localRepo.FindPackagesById(packageId).OrderByDescending(p => p.Version).Where(p => p.IsLatestVersion).FirstOrDefault();

            var remoteRepo = repository.GetRemoteOnlyAggregateRepository();
            IPackage remotePackage = null;
            if (remoteRepo != null)
            {
                //Copying logic from AggregateRepository, but refining the search
                Func<IPackageRepository, IPackage> findLatestPackage = Wrap(r => r.GetPackages().Where(p => p.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase) && p.IsLatestVersion).FirstOrDefault());
                var repo = remoteRepo.Repositories.Select(findLatestPackage);
                remotePackage = repo.Where(p => p != null).OrderByDescending(p => p.Version).FirstOrDefault();
            }

            if (localPackage != null && remotePackage != null)
                return localPackage.Version >= remotePackage.Version ? localPackage : remotePackage;
            return localPackage ?? remotePackage;
        }

        /// <summary>
        /// Returns a an AggregateRepository minus any DataServicePackageRepositories.  Useful if you want to use a command that will not work across these types.
        /// Snappy name, I know.
        /// </summary>
        /// <param name="repository"></param>
        /// <returns></returns>
        public static AggregateRepository GetLocalOnlyAggregateRepository(this AggregateRepository repository)
        {
            //Craziness as the MachineCache and LocalPackageRepository do not support IsLatestVersion
            var repoList = Flatten(repository.Repositories);
            //TODO this is not all the remotes that could be used...but it is the most common.
            return new AggregateRepository(repoList.Where(r => r.GetType() != typeof(DataServicePackageRepository) && r.GetType() != typeof(MachineCache)));
        }

        /// <summary>
        /// Returns a an AggregateRepository minus any LocalPackageRepositories or MachineCache repositories.  Useful if you want to use a command that will not work across these types.
        /// Snappy name, I know.
        /// </summary>
        /// <param name="repository"></param>
        /// <returns></returns>
        public static AggregateRepository GetRemoteOnlyAggregateRepository(this AggregateRepository repository)
        {
            //Craziness as the MachineCache and LocalPackageRepository do not support IsLatestVersion
            var repoList = Flatten(repository.Repositories);
            return new AggregateRepository(repoList.Where(r => r.GetType() != typeof(LocalPackageRepository) && r.GetType() != typeof(MachineCache)));
        }

        /// <summary>
        /// Finds the latest package in a repository constrained by an Id and an IVersionSpec
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="packageId"></param>
        /// <param name="versionSpec"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IPackage FindLatestPackage(this AggregateRepository repository, string packageId, IVersionSpec versionSpec)
        {
            if (packageId == null)
            {
                throw new ArgumentNullException("packageId");
            }
            //TODO Return the latest package between the versionsConstraint...
            var remoteOnlyAggregateRepository = repository.GetRemoteOnlyAggregateRepository();

            return remoteOnlyAggregateRepository.FindPackagesById(packageId).Where(p => IVersionExtensions.Satisfies(versionSpec, p.Version)).OrderByDescending(p => p.Version).FirstOrDefault();
        }

        //HACK Stolen from NuGet.AggregateRepository.
        internal static IEnumerable<IPackageRepository> Flatten(IEnumerable<IPackageRepository> repositories)
        {
            return repositories.SelectMany(repository =>
            {
                var aggrgeateRepository = repository as AggregateRepository;
                if (aggrgeateRepository != null)
                {
                    return aggrgeateRepository.Repositories.ToArray();
                }
                return new[] { repository };
            });
        }

        //HACK Stolen from NuGet.AggregateRepository.
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to suppress any exception that we may encounter.")]
        private static Func<IPackageRepository, T> Wrap<T>(Func<IPackageRepository, T> factory, T defaultValue = default(T))
        {
            return repository =>
            {
                try
                {
                    return factory(repository);
                }
                catch (Exception)
                {
                    return defaultValue;
                }
            };
        }
    }
}
