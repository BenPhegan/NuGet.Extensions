using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NuGet.Common;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.ReferenceAnalysers;
using NuGet.Extensions.Tests.Mocks;
using NUnit.Framework;

namespace NuGet.Extensions.Tests.ReferenceAnalysers
{
    [TestFixture]
    public class ReferenceNugetifierBinaryTests
    {
        private const string DefaultProjectPath = "c:\\isany.csproj";

        [Test]
        public void EmptyProjectHasNoNuggettedDependencies()
        {
            var nugetifier = BuildNugetifier();
            var nugettedDependencies = CallNugetifyReferences(nugetifier);
            Assert.That(nugettedDependencies, Is.Empty);
        }

        [Test]
        public void SingleDependencyGetsNugetted()
        {
            var singleDependency = ProjectReferenceTestData.ConstructMockDependency();
            var projectWithSingleDependency = ProjectReferenceTestData.ConstructMockProject(new[] {singleDependency.Object});
            var packageRepositoryWithOnePackage = ProjectReferenceTestData.CreateMockRepository();

            var nugetifier = BuildNugetifier(vsProject: projectWithSingleDependency.Object, packageRepository: packageRepositoryWithOnePackage);
            var nugettedDependencies = CallNugetifyReferences(nugetifier);

            Assert.That(nugettedDependencies, Is.Not.Empty);
            Assert.That(nugettedDependencies.Single().Id, Contains.Substring(ProjectReferenceTestData.PackageInRepository));
        }

        private static List<ManifestDependency> CallNugetifyReferences(ReferenceNugetifier nugetifier, ISharedPackageRepository sharedPackageRepository = null, string defaultProjectPath = null, List<string> projectReferences = null)
        {
            sharedPackageRepository = sharedPackageRepository ?? new Mock<ISharedPackageRepository>().Object;
            defaultProjectPath = defaultProjectPath ?? DefaultProjectPath;
            projectReferences = projectReferences ?? new List<string>();
            return nugetifier.NugetifyReferences(sharedPackageRepository, defaultProjectPath, projectReferences);
        }

        private static ReferenceNugetifier BuildNugetifier(IFileSystem projectFileSystem = null, IVsProject vsProject = null, PackageReferenceFile packageReferenceFile = null, IPackageRepository packageRepository = null)
        {
            var console = new Mock<IConsole>();
            var projectFileInfo = new FileInfo(DefaultProjectPath);
            var solutionRoot = new DirectoryInfo("c:\\isAnyFolder");
            projectFileSystem = projectFileSystem ?? new MockFileSystem(solutionRoot.FullName);
            vsProject = vsProject ?? new Mock<IVsProject>().Object;
            packageReferenceFile = packageReferenceFile ?? new PackageReferenceFile(projectFileSystem, solutionRoot.FullName);
            packageRepository = packageRepository ?? new MockPackageRepository();
            return new ReferenceNugetifier(console.Object, true, projectFileInfo, solutionRoot, projectFileSystem, vsProject, packageReferenceFile, packageRepository);
        }
    }
}
