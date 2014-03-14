using System.Collections.Generic;
using System.IO;
using Moq;
using NuGet.Common;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.ReferenceAnalysers;
using NuGet.Extensions.Tests.Mocks;

namespace NuGet.Extensions.Tests.ReferenceAnalysers
{
    public class ReferenceNugetifierTester 
    {
        public static List<ManifestDependency> GetManifestDependencies(ProjectNugetifier nugetifier, ISharedPackageRepository sharedPackageRepository = null)
        {
            sharedPackageRepository = sharedPackageRepository ?? new Mock<ISharedPackageRepository>().Object;
            return nugetifier.AddNugetReferenceMetadata(sharedPackageRepository, true);
        }

        public static ProjectNugetifier BuildNugetifier(IFileSystem projectFileSystem = null, Mock<IVsProject> vsProject = null, IPackageRepository packageRepository = null)
        {
            var console = new Mock<IConsole>();
            var solutionRoot = GetMockDirectory();
            projectFileSystem = projectFileSystem ?? GetMockFileSystem(solutionRoot);
            vsProject = vsProject ?? new Mock<IVsProject>();
            vsProject.SetupGet(p => p.ProjectDirectory).Returns(new DirectoryInfo(projectFileSystem.Root));
            packageRepository = packageRepository ?? new MockPackageRepository();
            return new ProjectNugetifier(vsProject.Object, packageRepository, projectFileSystem, console.Object, new HintPathGenerator());
        }

        private static MockFileSystem GetMockFileSystem(DirectoryInfo solutionRoot)
        {
            return new MockFileSystem(solutionRoot.FullName);
        }

        private static DirectoryInfo GetMockDirectory()
        {
            return new DirectoryInfo("c:\\isAnyFolder");
        }

        public static void NugetifyReferencesInProject(ProjectNugetifier nugetifier)
        {
            nugetifier.NugetifyReferences(GetMockDirectory());
        }
    }
}