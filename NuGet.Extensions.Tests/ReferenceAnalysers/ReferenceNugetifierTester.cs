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

        public static List<ManifestDependency> CallNugetifyReferences(ReferenceNugetifier nugetifier, ISharedPackageRepository sharedPackageRepository = null, string defaultProjectPath = null, List<string> projectReferences = null)
        {
            sharedPackageRepository = sharedPackageRepository ?? new Mock<ISharedPackageRepository>().Object;
            defaultProjectPath = defaultProjectPath ?? DefaultProjectPath;
            projectReferences = projectReferences ?? new List<string>();
            return nugetifier.NugetifyReferences(sharedPackageRepository, defaultProjectPath, projectReferences);
        }

        public static ReferenceNugetifier BuildNugetifier(IFileSystem projectFileSystem = null, IVsProject vsProject = null, PackageReferenceFile packageReferenceFile = null, IPackageRepository packageRepository = null)
        {
            var console = new Mock<IConsole>();
            var projectFileInfo = new FileInfo(DefaultProjectPath);
            var solutionRoot = new DirectoryInfo("c:\\isAnyFolder");
            projectFileSystem = projectFileSystem ?? new MockFileSystem(solutionRoot.FullName);
            vsProject = vsProject ?? new Mock<IVsProject>().Object;
            packageReferenceFile = packageReferenceFile ?? new PackageReferenceFile(projectFileSystem, solutionRoot.FullName);
            packageRepository = packageRepository ?? new MockPackageRepository();
            return new ReferenceNugetifier(console.Object, true, projectFileInfo, solutionRoot, projectFileSystem, vsProject, packageReferenceFile, packageRepository, "packages.config");
        }
    }
}