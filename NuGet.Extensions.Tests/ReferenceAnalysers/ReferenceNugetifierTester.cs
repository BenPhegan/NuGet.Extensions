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
        public static List<ManifestDependency> GetManifestDependencies(ReferenceNugetifier nugetifier, ISharedPackageRepository sharedPackageRepository = null, List<string> projectReferences = null, PackageReferenceFile packageReferenceFile = null)
        {
            sharedPackageRepository = sharedPackageRepository ?? new Mock<ISharedPackageRepository>().Object;
            projectReferences = projectReferences ?? new List<string>();
            packageReferenceFile = packageReferenceFile ?? GetPackageReferenceFile(GetMockFileSystem(GetMockDirectory()));
            return nugetifier.AddNugetMetadataForReferences(sharedPackageRepository, projectReferences, packageReferenceFile, true);
        }

        public static ReferenceNugetifier BuildNugetifier(IFileSystem projectFileSystem = null, Mock<IVsProject> vsProject = null, IPackageRepository packageRepository = null)
        {
            var console = new Mock<IConsole>();
            var solutionRoot = GetMockDirectory();
            projectFileSystem = projectFileSystem ?? GetMockFileSystem(solutionRoot);
            vsProject = vsProject ?? new Mock<IVsProject>();
            vsProject.SetupGet(p => p.ProjectDirectory).Returns(GetMockDirectory());
            packageRepository = packageRepository ?? new MockPackageRepository();
            return new ReferenceNugetifier(vsProject.Object, packageRepository, projectFileSystem, console.Object);
        }

        private static PackageReferenceFile GetPackageReferenceFile(IFileSystem projectFileSystem)
        {
            return new PackageReferenceFile(projectFileSystem, projectFileSystem.Root);
        }

        private static MockFileSystem GetMockFileSystem(DirectoryInfo solutionRoot)
        {
            return new MockFileSystem(solutionRoot.FullName);
        }

        private static DirectoryInfo GetMockDirectory()
        {
            return new DirectoryInfo("c:\\isAnyFolder");
        }

        public static void NugetifyReferencesInProject(ReferenceNugetifier nugetifier)
        {
            nugetifier.NugetifyReferencesInProject(GetMockDirectory());
        }
    }
}