using System;
using System.Linq;
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
        public void SingleDependencyGetsNugetted()
        {
            var singleDependency = ProjectReferenceTestData.ConstructMockDependency();
            var projectWithSingleDependency = ProjectReferenceTestData.ConstructMockProject(new[] {singleDependency.Object});
            var packageRepositoryWithOnePackage = ProjectReferenceTestData.CreateMockRepository();

            var nugetifier = ReferenceNugetifierTester.BuildNugetifier(vsProject: projectWithSingleDependency.Object, packageRepository: packageRepositoryWithOnePackage);
            var nugettedDependencies = ReferenceNugetifierTester.GetManifestDependencies(nugetifier);

            Assert.That(nugettedDependencies, Is.Not.Empty);
            Assert.That(nugettedDependencies.Single().Id, Contains.Substring(ProjectReferenceTestData.PackageInRepository));
        }
    }
}
