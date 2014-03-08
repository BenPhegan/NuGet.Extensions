using System;
using System.Linq;
using Moq;
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
            var nugettedDependencies = ReferenceNugetifierTester.GetManifestDependencies(nugetifier);
            Assert.That(nugettedDependencies, Is.Empty);
        }

        [Test]
        public void SingleDependencyListedInManifestDependencies()
        {
            var singleDependency = ProjectReferenceTestData.ConstructMockDependency();
            var projectWithSingleDependency = ProjectReferenceTestData.ConstructMockProject(new[] {singleDependency.Object});
            var packageRepositoryWithOnePackage = ProjectReferenceTestData.CreateMockRepository();

            var nugetifier = ReferenceNugetifierTester.BuildNugetifier(vsProject: projectWithSingleDependency, packageRepository: packageRepositoryWithOnePackage);
            var nugettedDependencies = ReferenceNugetifierTester.GetManifestDependencies(nugetifier);

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

        [Test]
        public void TwoDependenciesWithCorrespondingPackagesGetNugetted()
        {
            var defaultDependency = ProjectReferenceTestData.ConstructMockDependency(ProjectReferenceTestData.AssemblyInPackageRepository);
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
