using System.IO;
using Moq;
using NuGet.Extensions.Tests.Mocks;
using NuGet.Extensions.ExtensionMethods;
using NUnit.Framework;
using MockPackageRepository = NuGet.Extensions.Tests.TestObjects.MockPackageRepository;
using PackageUtility = NuGet.Extensions.Tests.TestObjects.PackageUtility;

namespace NuGet.Extensions.Tests.Extensions
{
    [TestFixture]
    public class IPackageManagerExtensionsTests
    {
        [TestCase("Assembly.Common", "2.1.9", "2.1.9", false, false, false, Description = "On Disk is same version", Result = true)]
        [TestCase("Assembly.Common", "2.1.9", "2.1.9", false, true, true, Description = "On Disk is same version", Result = true)]
        [TestCase("Assembly.Common", "2.1.9", "2.1.9", false, true, false, Description = "On Disk is same version", Result = false)]
        [TestCase("Assembly.Common", "2.1.8", "2.1.9", false, false, false, Description = "On disk is older", Result = false)]
        [TestCase("Assembly.Common", "2.1.8", "2.1.9", false, true, true, Description = "On disk is older", Result = false)]
        [TestCase("Assembly.Common", "2.1.9", "2.1.8", false, false, false, Description = "On disk is newer", Result = false)]
        [TestCase("Assembly.Common", "", "2.1.8", false, false, false, Description = "No on disk results in false", Result = false)]
        public bool CanDetermineVersionlessPackageIsInstalled(string id, string onDiskVersion, string packageVersion, bool installedUsingMultipleVersion, bool currentlyAllowMultipleVersions, bool exhaustive)
        {
            var mfs = new Mock<MockFileSystem>() { CallBase = true };
            mfs.Setup(m => m.Root).Returns(@"c:\packages");

            var installPathResolver = new DefaultPackagePathResolver(mfs.Object, installedUsingMultipleVersion);
            var currentPathResolver = new DefaultPackagePathResolver(mfs.Object, currentlyAllowMultipleVersions);

            var testPackage = new DataServicePackage()
            {
                Version = packageVersion,
                Id = id
            };

            var filePackage = new DataServicePackage()
            {
                Version = onDiskVersion,
                Id = id
            };

            IPackage zipPackage = null;
            if (!string.IsNullOrEmpty(onDiskVersion))
            {
                string baseLocation = installPathResolver.GetInstallPath(filePackage);
                string fileName = installPathResolver.GetPackageFileName(filePackage.Id, SemanticVersion.Parse(filePackage.Version));
                string filePath = Path.Combine(baseLocation, fileName);
                zipPackage = PackageUtility.GetZipPackage(filePackage.Id, filePackage.Version);
                mfs.Setup(m => m.FileExists(filePath)).Returns(true);
                mfs.Setup(m => m.OpenFile(filePath)).Returns(zipPackage.GetStream());
                mfs.Object.AddFile(filePath, zipPackage.GetStream());
            }

            var pm = new PackageManager(new MockPackageRepository(), currentPathResolver, mfs.Object);
            var exists = pm.IsPackageInstalled(testPackage,exhaustive: exhaustive);
            //var test = testPackage.IsPackageInstalled(allowMultipleVersions, pm, mfs.Object);
            return exists;
        }

    }
}
