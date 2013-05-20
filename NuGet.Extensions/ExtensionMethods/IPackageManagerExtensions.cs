using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Extensions.ExtensionMethods;

namespace NuGet.Extensions.ExtensionMethods
{
    /// <summary>
    /// IPackageManager Extensions
    /// </summary>
    public static class IPackageManagerExtensions
    {
        /// <summary>
        /// Cleans the package folders.  Requires that the PackageManager uses an IFileSystem that has the Root set to the packages folder.
        /// </summary>
        public static void CleanPackageFolders(this IPackageManager packageManager)
        {
            var immediateDirectoryName = Path.GetFileName(packageManager.FileSystem.Root);
            if (immediateDirectoryName != null && immediateDirectoryName.Equals("packages", StringComparison.OrdinalIgnoreCase))
            {
                var directories = new ConcurrentBag<string>(packageManager.FileSystem.GetDirectories(packageManager.FileSystem.Root).ToList());
                packageManager.Logger.Log(MessageLevel.Warning, String.Format("Deleting {0} package directories from {1}.", directories.Count, packageManager.FileSystem.Root));
                Parallel.ForEach(directories, (directory) => packageManager.FileSystem.DeleteDirectory(directory, true));
            }
        }

        /// <summary>
        /// Checks whether an IPackage exists within a PackageManager.  By default, will use the UseSideBySide settings of the DefaultPackagePathProvider the PackageManager is instantiated with.
        /// If passed in TRUE for exhaustive, will check both with and without UseSideBySide set.
        /// </summary>
        /// <param name="packageManager"></param>
        /// <param name="packageId"> </param>
        /// <param name="version"> </param>
        /// <param name="exhaustive"></param>
        /// <returns></returns>
        public static bool IsPackageInstalled(this PackageManager packageManager, string packageId, SemanticVersion version, bool exhaustive = false)
        {
            IPackage package = new DataServicePackage
                        {
                            Version = version.ToString(),
                            Id = packageId
                        };

            return packageManager.IsPackageInstalled(package, exhaustive);
        }

        /// <summary>
        /// Checks whether an IPackage exists within a PackageManager.  By default, will use the UseSideBySide settings of the DefaultPackagePathProvider the PackageManager is instantiated with.
        /// If passed in TRUE for exhaustive, will check both with and without UseSideBySide set.
        /// </summary>
        /// <param name="packageManager"></param>
        /// <param name="package"></param>
        /// <param name="exhaustive"></param>
        /// <returns></returns>
        public static bool IsPackageInstalled(this PackageManager packageManager,  IPackage package, bool exhaustive = false)
        {

            var pathsDictionary = new Dictionary<string, bool>();
            //Oh god oh god. The <center> cannot hold it is too late.
            var useSideBySide = packageManager.PathResolver.GetPrivateField<bool>("_useSideBySidePaths");
            pathsDictionary.Add(Path.Combine(packageManager.PathResolver.GetInstallPath(package), 
                packageManager.PathResolver.GetPackageFileName(package.Id, package.Version)), useSideBySide);

            //We need to also check the inverse, to see if it was installed with the other setting....
            if (exhaustive)
            {
                var inversePathResolver = new DefaultPackagePathResolver(packageManager.PathResolver.GetPrivateField<IFileSystem>("_fileSystem"), !useSideBySide);
                pathsDictionary.Add(Path.Combine(inversePathResolver.GetInstallPath(package), inversePathResolver.GetPackageFileName(package.Id, package.Version)), !useSideBySide);
            }

            foreach (var path in pathsDictionary.Where(path => packageManager.FileSystem.FileExists(path.Key)))
            {
                if (path.Value)
                {
                    return true;
                }
                
                //If not useSideBySide, we need to crack open the zip file.
                //Need to crack the package open at this point and check the version, otherwise we just need to download it regardless
                var zipPackage = new ZipPackage(packageManager.FileSystem.OpenFile(path.Key));
                if (zipPackage.Version == package.Version)
                {
                    return true;
                }
            }

            //Its not here.  Really.  We tried.
            return false;
        }
    }
}
