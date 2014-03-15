using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NuGet.Common;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.ReferenceAnalysers;
using NuGet.Extensions.Tests.Mocks;
using NuGet.Extensions.Tests.MSBuild;

namespace NuGet.Extensions.Tests.ReferenceAnalysers
{
    public class ReferenceNugetifierTester 
    {
        public static List<IPackage> AddReferenceMetadata(ProjectNugetifier nugetifier, Mock<ISharedPackageRepository> repositoriesConfig = null, DirectoryInfo solutionDir = null)
        {
            var sharedPackageRepository = repositoriesConfig ?? new Mock<ISharedPackageRepository>();
            solutionDir = solutionDir ?? GetMockDirectory();
            var packages = nugetifier.NugetifyReferences(solutionDir);
            nugetifier.AddNugetReferenceMetadata(sharedPackageRepository.Object, packages);
            return packages.ToList();
        }

        public static ProjectNugetifier BuildNugetifier(IFileSystem projectFileSystem = null, Mock<IVsProject> vsProject = null, IPackageRepository packageRepository = null)
        {
            var console = new ConsoleMock();
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

        public static ICollection<IPackage> NugetifyReferencesInProject(ProjectNugetifier nugetifier)
        {
            return nugetifier.NugetifyReferences(GetMockDirectory()).ToArray();
        }
    }
}