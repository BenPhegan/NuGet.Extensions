using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Extras.Comparers;
using NuGet.Extras.ExtensionMethods;
using NuGet.Extras.PackageReferences;
using NuGet.Extras.Packages;
using NuGet.Extras.Repositories;
using NuGet.Extras.Caches;

//HACK to enable us to test some of the Internal stuff more easily.  Still not sure how many kittens die when we use this.....
[assembly: InternalsVisibleTo("NuGet.Extensions.Tests")]

namespace NuGet.Extensions.Commands
{
    [Command(typeof(GetResources), "get", "GetCommandDescription", MinArgs = 1)]
    public class Get : Command
    {
        private IPackageRepository _cacheRepository;
        private readonly List<string> _sources = new List<string>();
        private INuGetCacheManager _cacheManager;
        protected IFileSystem OutputFileSystem;
        private IPackageRepository _repository;
        private IPackageResolutionManager _packageResolutionManager;
        private IPackageCache _packageCache;

        [Option(typeof(GetResources), "GetCommandSourceDescription")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option(typeof(GetResources), "GetCommandOutputDirectoryDescription")]
        public string OutputDirectory { get; set; }

        [Option(typeof(GetResources), "GetCommandExcludeVersionDescription", AltName = "x")]
        public bool ExcludeVersion { get; set; }

        [Option(typeof(GetResources), "GetCommandLatestDescription")]
        public bool Latest { get; set; }

        [Option(typeof(GetResources), "GetCommandPrerelease")]
        public bool Prerelease { get; set; }

        [Option(typeof(GetResources), "GetCommandUseCacheDescription")]
        public bool NoCache { get; set; }

        [Option(typeof(GetResources), "GetCommandNoFeedSpecificCache", AltName = "nfsp")]
        public bool NoFeedSpecificCache { get; set; }

        [Option(typeof(GetResources), "GetCommandCleanDescription")]
        public bool Clean { get; set; }

        [Option(typeof(GetResources), "GetCommandIncludeDependenciesDescription", AltName = "r")]
        public bool IncludeDependencies { get; set; }

        public IPackageRepositoryFactory RepositoryFactory { get; private set; }

        public IPackageSourceProvider SourceProvider { get; private set; }

        /// <remarks>
        /// Meant for unit testing.
        /// </remarks>
        protected IPackageRepository CacheRepository
        {
            get { return _cacheRepository; }
        }

        private bool AllowMultipleVersions
        {
            get { return !ExcludeVersion; }
        }

        [ImportingConstructor]
        public Get(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            Contract.Assert(packageRepositoryFactory != null);
            Contract.Assert(sourceProvider != null);

            RepositoryFactory = packageRepositoryFactory;
            SourceProvider = sourceProvider;
        }

        public Get(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider, IPackageRepository cacheRepository, IFileSystem fileSystem, IPackageCache packageCache)
            :this(packageRepositoryFactory, sourceProvider)
        {
            _cacheRepository = cacheRepository;
            OutputFileSystem = fileSystem;
            _packageCache = packageCache;
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        public override void ExecuteCommand()
        {
            //Probably need some better return code logic here...
            if (Arguments[0] == null) return;
            try
            {
                if (Source.Count == 1 && !NoFeedSpecificCache && !NoCache)
                {
                    _cacheManager = new NuGetCacheManager(Console);
                    _cacheManager.SetFeedSpecificCacheDirectory(_sources[0]);
                }

                if (!NoCache)
                    if (_cacheRepository == null)
                        _cacheRepository = MachineCache.Default;

                //TODO This needs injecting....
                if (_packageCache == null)
                    _packageCache = new MemoryBasedPackageCache(Console);

                _repository = GetRepository();
                _packageResolutionManager = new PackageResolutionManager(Console, Latest, _packageCache);

                //HACK need to inject somehow...
                _packageResolutionManager = _packageResolutionManager ?? new PackageResolutionManager(Console, Latest, new MemoryBasedPackageCache(Console));

                //Working on a package.config
                if (Path.GetFileName(Arguments[0]).Equals(Constants.PackageReferenceFile, StringComparison.OrdinalIgnoreCase))
                {
                    OutputFileSystem = CreateFileSystem(Path.GetPathRoot(Arguments[0]));
                    GetByPackagesConfig(OutputFileSystem);
                }
                else
                {
                    OutputFileSystem = CreateFileSystem(Directory.GetParent(Arguments[0]).FullName);
                    GetByDirectoryPath(OutputFileSystem);
                }
            }
            catch (Exception e)
            {
                Console.WriteError(e.Message);
            }
        }

        private void GetByDirectoryPath(IFileSystem baseFileSystem)
        {
            if (baseFileSystem.DirectoryExists(Arguments[0]))
            {
                var repositoryGroupManager = new RepositoryGroupManager(Arguments[0], baseFileSystem);
                var repositoryManagers = new ConcurrentBag<RepositoryManager>(repositoryGroupManager.RepositoryManagers);

                var totalTime = new Stopwatch();
                totalTime.Start();

                int totalPackageUpdates = 0;

                bool exitWithFailure = false;
                Array.ForEach(repositoryManagers.ToArray(), (repositoryManager) =>
                    {
                        string packagesConfigDiretory = null;

                        if (repositoryManager.RepositoryConfig.Directory != null)
                            packagesConfigDiretory = repositoryManager.RepositoryConfig.Directory.FullName;
                        
                        using (var packageAggregator = new PackageAggregator(baseFileSystem, repositoryManager, new PackageEnumerator()))
                        {
                            packageAggregator.Compute((min, max) =>
                                {
                                    totalPackageUpdates += Convert.ToInt16(min);
                                    Console.WriteLine("Getting {0} distinct packages from a total of {1} from {2}",min, max,repositoryManager.RepositoryConfig.FullName);
                                },
                                Latest ? PackageReferenceEqualityComparer.IdAndAllowedVersions : PackageReferenceEqualityComparer.IdVersionAndAllowedVersions , new PackageReferenceSetResolver());

                            if (packageAggregator.PackageResolveFailures.Any())
                            {
                                LogAllPackageConstraintSatisfactionErrors(repositoryGroupManager, packageAggregator);
                                exitWithFailure = true;
                            }
                            else
                            {
                                var tempPackageConfig = packageAggregator.Save(packagesConfigDiretory);
                                InstallPackagesFromConfigFile(packagesConfigDiretory, GetPackageReferenceFile(baseFileSystem, tempPackageConfig.FullName));
                            }
                        }
                    });

                totalTime.Stop();
                if (exitWithFailure)
                {
                    Console.WriteError(string.Format("Failed : {0} package directories, {1} packages in {2} seconds",
                                                     repositoryManagers.Count, totalPackageUpdates,
                                                     totalTime.Elapsed.TotalSeconds));
                    Environment.Exit(1);
                }
                else
                {
                    Console.WriteLine(string.Format("Updated : {0} package directories, {1} packages in {2} seconds",
                                                    repositoryManagers.Count, totalPackageUpdates,
                                                    totalTime.Elapsed.TotalSeconds));
                }
            }
        }

        private void GetByPackagesConfig(IFileSystem fileSystem)
        {
            if (fileSystem.FileExists(Arguments[0]))
            {
                //Try and infer the output directory if it is null
                OutputDirectory = OutputDirectory ?? ResolvePackagesDirectory(fileSystem, Path.GetDirectoryName(Arguments[0]));

                if (!string.IsNullOrEmpty(OutputDirectory))
                    InstallPackagesFromConfigFile(OutputDirectory, GetPackageReferenceFile(fileSystem, Arguments[0]));
                else
                    Console.WriteError(string.Format("Could not find packages directory based on {0}", Arguments[0]));
            }
            else
            {
                Console.WriteError(String.Format("Could not find file : {0}", Arguments[0]));
            }
        }

        private string ResolvePackagesDirectory(IFileSystem fileSystem, string parentPath)
        {
            var possiblePackagesPath = Path.Combine(parentPath, "packages");
            if (fileSystem.DirectoryExists(possiblePackagesPath))
                return possiblePackagesPath;

            if (Path.GetPathRoot(parentPath) == parentPath)
                return null;

            return ResolvePackagesDirectory(fileSystem, Directory.GetParent(parentPath).FullName);
        }

        /// <summary>
        /// Logs all package constraint satisfaction errors.
        /// </summary>
        /// <param name="repositoryGroupManager">The repository group manager.</param>
        /// <param name="packageAggregator">The package aggregator.</param>
        private void LogAllPackageConstraintSatisfactionErrors(RepositoryGroupManager repositoryGroupManager, PackageAggregator packageAggregator)
        {
            //HACK #UGLY #WTF There must be a better way....
            foreach (var p in packageAggregator.PackageResolveFailures)
            {
                foreach (var rp in repositoryGroupManager.RepositoryManagers)
                {
                    foreach (var prf in rp.PackageReferenceFiles)
                    {
                        if (prf.GetPackageReferences().Contains(p))
                        {
                            LogSatisfactionFailure(p, prf);
                        }
                    }
                }
            }
        }

        //TODO HACK HACK #WTF #EVIL Check the access to a private property.  This requires a change to the PackageReferenceFile otherwise, which would take a long time.....
        /// <summary>
        /// Logs the satisfaction failure.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <param name="prf">The PRF.</param>
        private void LogSatisfactionFailure(PackageReference p, PackageReferenceFile prf)
        {
            Console.WriteError("{0} {1}{2} in {3} could not be satisfied.", p.Id, p.Version, p.VersionConstraint != null ? " using constraint " + p.VersionConstraint : string.Empty, GetPackageConfigLocationUsingUltimateEvil(prf));
        }

        private static string GetPackageConfigLocationUsingUltimateEvil(PackageReferenceFile prf)
        {
            var blah = prf.GetPrivateProperty<IFileSystem>("FileSystem");
            return blah.GetFullPath(prf.GetPrivateField<string>("_path"));
        }

        protected virtual PackageReferenceFile GetPackageReferenceFile(IFileSystem filesystem, string path)
        {
            return new PackageReferenceFile(filesystem, path);
        }

        /// <summary>
        /// Gets the repository.
        /// </summary>
        /// <returns></returns>
        private IPackageRepository GetRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            bool ignoreFailingRepositories = repository.IgnoreFailingRepositories;
            if (!NoCache)
            {
                repository = new AggregateRepository(new[] { CacheRepository, repository }){ IgnoreFailingRepositories = ignoreFailingRepositories, Logger = Console};
            }
            repository.Logger = Console;
            return repository;
        }

        /// <summary>
        /// Installs the packages from config file.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="file">The file.</param>
        private void InstallPackagesFromConfigFile(string packagesDirectory, PackageReferenceFile file)
        {
            var packageReferences = file.GetPackageReferences().ToList();

            //We need to create a damn filesystem at the packages directory, so that the ROOT is correct.  Ahuh...
            var fileSystem = CreateFileSystem(packagesDirectory);

            if (!NoCache)
                Console.WriteLine("Using cache....");
            PackageManager packageManager = CreatePackageManager(fileSystem, useMachineCache: !NoCache);

            if (Clean)
                packageManager.CleanPackageFolders();

            bool installedAny = false;
            foreach (var packageReference in packageReferences)
            {
                if (String.IsNullOrEmpty(packageReference.Id))
                {
                    // GetPackageReferences returns all records without validating values. We'll throw if we encounter packages
                    // with malformed ids / Versions.
                    throw new InvalidDataException(String.Format(CultureInfo.CurrentCulture, GetResources.GetCommandInvalidPackageReference, Arguments[0]));
                }
                else if (packageReference.Version == null)
                {
                    throw new InvalidDataException(String.Format(CultureInfo.CurrentCulture, GetResources.GetCommandPackageReferenceInvalidVersion, packageReference.Id));
                }

                IPackage package = _packageResolutionManager.ResolveLatestInstallablePackage(_repository, packageReference);
                if (package == null)
                {
                    SemanticVersion version = _packageResolutionManager.ResolveInstallableVersion(_repository, packageReference);
                    installedAny |= InstallPackage(packageManager, fileSystem, packageReference.Id, version ?? packageReference.Version);
                }
                else
                {
                    //We got it straight from the server, check whether we get a cache hit, else just install
                    var resolvedPackage = _packageResolutionManager.FindPackageInAllLocalSources(packageManager.LocalRepository, packageManager.SourceRepository, package);
                    packageManager.InstallPackage(resolvedPackage ?? package, true, false);
                }
                // Note that we ignore dependencies here because packages.config already contains the full closure
            }

            if (!installedAny && packageReferences.Any())
            {
                Console.WriteLine(GetResources.GetCommandNothingToInstall, Constants.PackageReferenceFile);
            }
        }


        /// <summary>
        /// Installs the package.
        /// </summary>
        /// <param name="packageManager">The package manager.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="packageId">The package id.</param>
        /// <param name="version">The version.</param>
        /// <param name="allowPreReleaseVersion"> </param>
        /// <returns></returns>
        internal bool InstallPackage(PackageManager packageManager, IFileSystem fileSystem, string packageId, SemanticVersion version, Boolean allowPreReleaseVersion = true)
        {
            if (packageManager.IsPackageInstalled(packageId, version))
            {
                return false;
            }

            if (!AllowMultipleVersions)
            {
                var installedPackage = packageManager.LocalRepository.FindPackage(packageId);
                if (installedPackage != null)
                {
                    if (version != null && installedPackage.Version >= version)
                    {
                        // If the package is already installed (or the version being installed is lower), then we do not need to do anything. 
                        return false;
                    }
                    else if (packageManager.SourceRepository.Exists(packageId, version))
                    {
                        // If the package is already installed, but
                        // (a) the version we require is different from the one that is installed, 
                        // (b) side-by-side is disabled
                        // we need to uninstall it.
                        // However, before uninstalling, make sure the package exists in the source repository. 
                        packageManager.UninstallPackage(installedPackage, forceRemove: true, removeDependencies: false);
                    }
                }
            }
            //TODO Will need to expose the last boolean here......
            var package = _packageResolutionManager.ResolvePackage(packageManager.LocalRepository, _repository, packageId, version, allowPreReleaseVersion);
            packageManager.InstallPackage(package, ignoreDependencies: true, allowPrereleaseVersions: allowPreReleaseVersion);
            //packageManager.InstallPackage(packageId, version, !IncludeDependencies, allowPreReleaseVersion);
            return true;
        }

        /// <summary>
        /// Creates the package manager.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="useMachineCache">if set to <c>true</c> [use machine cache].</param>
        /// <returns></returns>
        protected virtual PackageManager CreatePackageManager(IFileSystem fileSystem, bool useMachineCache = false)
        {
            var pathResolver = new DefaultPackagePathResolver(fileSystem, useSideBySidePaths: AllowMultipleVersions);
            var packageManager = new PackageManager(_repository, pathResolver, fileSystem, new LocalPackageRepository(pathResolver, fileSystem));
            packageManager.Logger = Console;

            return packageManager;
        }

        protected virtual IFileSystem CreateFileSystem(string pathRoot)
        {
            // Use the passed in install path if any, and default to the current dir
            return new PhysicalFileSystem(pathRoot);
        }
    }
}
