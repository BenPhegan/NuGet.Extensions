using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Moq;
using NuGet.Extensions.Tests.Mocks;
using NUnit.Framework;

namespace NuGet.Extensions.Tests.ReferenceAnalysers
{
    [TestFixture]
    public class ReferenceNugetifierBinaryTests
    {
        [Test]
        public void EmptyProjectHasNoNuggettedDependencies()
        {
            var nugetifier = ReferenceNugetifierTester.BuildNugetifier();
            var nugettedDependencies = ReferenceNugetifierTester.AddReferenceMetadata(nugetifier);
            Assert.That(nugettedDependencies, Is.Empty);
        }

        [Test]
        public void NugettedProjectHasPackagesConfigAddedToRepositoriesConfig()
        {
            const string projectRoot = "c:\\projectRoot";
            var fileSystem = new MockFileSystem(projectRoot);
            var singleDependency = ProjectReferenceTestData.ConstructMockDependency();
            var projectWithSingleDependency = ProjectReferenceTestData.ConstructMockProject(new[] { singleDependency.Object });
            var packageRepositoryWithOnePackage = ProjectReferenceTestData.CreateMockRepository();
            var repositoriesConfig = new Mock<ISharedPackageRepository>();

            var nugetifier = ReferenceNugetifierTester.BuildNugetifier(vsProject: projectWithSingleDependency, packageRepository: packageRepositoryWithOnePackage, projectFileSystem: fileSystem);
            var nugettedDependencies = ReferenceNugetifierTester.AddReferenceMetadata(nugetifier, repositoriesConfig, new DirectoryInfo(projectRoot));

            Assert.That(nugettedDependencies, Is.Not.Empty);
            repositoriesConfig.Verify(r => r.RegisterRepository(It.Is<string>(path => path.StartsWith(projectRoot))));
        }

        [Test]
        public void SingleDependencyListedInManifestDependencies()
        {
            var singleDependency = ProjectReferenceTestData.ConstructMockDependency();
            var projectWithSingleDependency = ProjectReferenceTestData.ConstructMockProject(new[] {singleDependency.Object});
            var packageRepositoryWithOnePackage = ProjectReferenceTestData.CreateMockRepository();

            var nugetifier = ReferenceNugetifierTester.BuildNugetifier(vsProject: projectWithSingleDependency, packageRepository: packageRepositoryWithOnePackage);
            var nugettedDependencies = ReferenceNugetifierTester.NugetifyReferencesInProject(nugetifier);

            Assert.That(nugettedDependencies, Is.Not.Empty);
            Assert.That(nugettedDependencies.Single().Id, Contains.Substring(ProjectReferenceTestData.PackageInRepository));
        }

        [Test]
        public void PackageFoundForDependencyWithNoHintPath()
        {
            var singleDependency = ProjectReferenceTestData.ConstructMockDependency(hasHintpath: false);
            var projectWithSingleDependency = ProjectReferenceTestData.ConstructMockProject(new[] { singleDependency.Object });
            var packageRepositoryWithOnePackage = ProjectReferenceTestData.CreateMockRepository();

            var nugetifier = ReferenceNugetifierTester.BuildNugetifier(vsProject: projectWithSingleDependency, packageRepository: packageRepositoryWithOnePackage);
            var nugettedDependencies = ReferenceNugetifierTester.NugetifyReferencesInProject(nugetifier);

            Assert.That(nugettedDependencies, Is.Not.Empty);
            Assert.That(nugettedDependencies.Single().Id, Contains.Substring(ProjectReferenceTestData.PackageInRepository));
        }

        [Test]
        public void SingleDependencyWithCorrespondingPackageGetsNugetted()
        {
            var singleDependency = ProjectReferenceTestData.ConstructMockDependency();
            var projectWithSingleDependency = ProjectReferenceTestData.ConstructMockProject(new[] { singleDependency.Object });
            var packageRepositoryWithCorrespondingPackage = ProjectReferenceTestData.CreateMockRepository();

            var nugetifier = ReferenceNugetifierTester.BuildNugetifier(vsProject: projectWithSingleDependency, packageRepository: packageRepositoryWithCorrespondingPackage);
            ReferenceNugetifierTester.NugetifyReferencesInProject(nugetifier);
            singleDependency.Verify(binaryDependency => binaryDependency.ConvertToNugetReferenceWithHintPath(It.IsAny<string>()), Times.Once());
        }

        [TestCase("net40")]
        [TestCase("net35-client")]
        [TestCase(null)]
        public void TargetFrameworkAppearsInPackagesConfig(string targetFrameworkString)
        {
            FrameworkName targetFramework = targetFrameworkString != null ? VersionUtility.ParseFrameworkName(targetFrameworkString) : null;
            var singleDependency = ProjectReferenceTestData.ConstructMockDependency();
            var projectWithSingleDependency = ProjectReferenceTestData.ConstructMockProject(new[] { singleDependency.Object });
            var packageRepositoryWithCorrespondingPackage = ProjectReferenceTestData.CreateMockRepository();
            var projectFileSystem = new MockFileSystem();
            var nugetifier = ReferenceNugetifierTester.BuildNugetifier(vsProject: projectWithSingleDependency, packageRepository: packageRepositoryWithCorrespondingPackage, projectFileSystem: projectFileSystem);

            ReferenceNugetifierTester.AddReferenceMetadata(nugetifier, targetFrameWork: targetFramework);

            var packageConfigs = projectFileSystem.Paths.Select(pathAndData => new PackageReferenceFile(projectFileSystem, pathAndData.Key));
            var packageReferences = packageConfigs.SelectMany(config => config.GetPackageReferences()).ToList();
            Assert.That(packageReferences, Is.Not.Empty);
            Assert.That(packageReferences, Has.All.Matches<PackageReference>(x => Equals(targetFramework, x.TargetFramework)));
        }

        [Test]
        public void TwoDependenciesWithCorrespondingPackagesGetNugetted()
        {
            var defaultDependency = ProjectReferenceTestData.ConstructMockDependency(ProjectReferenceTestData.AssemblyFilenameInPackageRepository);
            var secondDependency = ProjectReferenceTestData.ConstructMockDependency(ProjectReferenceTestData.AnotherAssemblyInPackageRepository);
            var projectWithSingleDependency = ProjectReferenceTestData.ConstructMockProject(new[] { defaultDependency.Object, secondDependency.Object });
            var packageRepositoryWithCorrespondingPackage = ProjectReferenceTestData.CreateMockRepository();

            var nugetifier = ReferenceNugetifierTester.BuildNugetifier(vsProject: projectWithSingleDependency, packageRepository: packageRepositoryWithCorrespondingPackage);
            ReferenceNugetifierTester.NugetifyReferencesInProject(nugetifier);
            defaultDependency.Verify(d => d.ConvertToNugetReferenceWithHintPath(It.IsAny<string>()), Times.Once());
            secondDependency.Verify(d => d.ConvertToNugetReferenceWithHintPath(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void SingleDependencyWithoutCorrespondingPackageNotNugetted()
        {
            var singleDependency = ProjectReferenceTestData.ConstructMockDependency();
            var projectWithSingleDependency = ProjectReferenceTestData.ConstructMockProject(new[] { singleDependency.Object });

            var nugetifier = ReferenceNugetifierTester.BuildNugetifier(vsProject: projectWithSingleDependency);
            ReferenceNugetifierTester.NugetifyReferencesInProject(nugetifier);

            singleDependency.Verify(binaryDependency => binaryDependency.ConvertToNugetReferenceWithHintPath(It.IsAny<string>()), Times.Never());
        }
    }
}
