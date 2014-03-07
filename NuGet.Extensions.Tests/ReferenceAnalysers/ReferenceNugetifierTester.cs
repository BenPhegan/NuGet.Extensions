using System.Collections.Generic;
using System.IO;
using Moq;
using NuGet.Common;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.ReferenceAnalysers;
using NuGet.Extensions.Tests.Mocks;

namespace NuGet.Extensions.Tests.ReferenceAnalysers
{
    public class ReferenceNugetifierTester {
        private const string DefaultProjectPath = "c:\\isany.csproj";

        public static List<ManifestDependency> GetManifestDependencies(ReferenceNugetifier nugetifier, ISharedPackageRepository sharedPackageRepository = null, List<string> projectReferences = null, PackageReferenceFile packageReferenceFile = null)
        {
            sharedPackageRepository = sharedPackageRepository ?? new Mock<ISharedPackageRepository>().Object;
            projectReferences = projectReferences ?? new List<string>();
            packageReferenceFile = packageReferenceFile ?? GetPackageReferenceFile(GetMockFileSystem(GetMockSolutionRoot()));
            return nugetifier.AddNugetMetadataForReferences(sharedPackageRepository, projectReferences, packageReferenceFile, "packages.config", true);
        }

        public static ReferenceNugetifier BuildNugetifier(IFileSystem projectFileSystem = null, IVsProject vsProject = null, IPackageRepository packageRepository = null)
        {
            var console = new Mock<IConsole>();
            var projectFileInfo = new FileInfo(DefaultProjectPath);
            var solutionRoot = GetMockSolutionRoot();
            projectFileSystem = projectFileSystem ?? GetMockFileSystem(solutionRoot);
            vsProject = vsProject ?? new Mock<IVsProject>().Object;
            packageRepository = packageRepository ?? new MockPackageRepository();
            return new ReferenceNugetifier(vsProject, packageRepository, projectFileSystem, console.Object);
        }

        private static PackageReferenceFile GetPackageReferenceFile(IFileSystem projectFileSystem)
        {
            return new PackageReferenceFile(projectFileSystem, projectFileSystem.Root);
        }

        private static MockFileSystem GetMockFileSystem(DirectoryInfo solutionRoot)
        {
            return new MockFileSystem(solutionRoot.FullName);
        }

        private static DirectoryInfo GetMockSolutionRoot()
        {
            return new DirectoryInfo("c:\\isAnyFolder");
        }
    }
}