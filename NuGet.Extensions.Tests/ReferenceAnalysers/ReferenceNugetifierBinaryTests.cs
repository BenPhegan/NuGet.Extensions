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
        private const string AssemblyInPackageRepository = "Assembly11.dll";
        private const string PackageInRepository = "Test1";

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
            var singleDependency = ConstructMockDependency();
            var projectWithSingleDependency = ConstructMockProject(new[] {singleDependency.Object});
            var packageRepositoryWithOnePackage = CreateMockRepository();

            var nugetifier = BuildNugetifier(vsProject: projectWithSingleDependency.Object, packageRepository: packageRepositoryWithOnePackage);
            var nugettedDependencies = CallNugetifyReferences(nugetifier);

            Assert.That(nugettedDependencies, Is.Not.Empty);
            Assert.That(nugettedDependencies.Single().Id, Contains.Substring(PackageInRepository));
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

        private static Mock<IVsProject> ConstructMockProject(IBinaryReference[] binaryReferences)
        {
            var projectWithSingleDependency = new Mock<IVsProject>();
            projectWithSingleDependency.Setup(proj => proj.GetBinaryReferences()).Returns(binaryReferences);
            return projectWithSingleDependency;
        }

        private static Mock<IBinaryReference> ConstructMockDependency(string includeName = null, string includeVersion = null)
        {
            includeName = includeName ?? AssemblyInPackageRepository;

            var dependency = new Mock<IBinaryReference>();
            dependency.SetupGet(d => d.IncludeName).Returns(includeName);
            dependency.SetupGet(d => d.IncludeVersion).Returns(includeVersion ?? "0.0.0.0");
            dependency.Setup(d => d.IsForAssembly(It.IsAny<string>())).Returns(true);

            string anydependencyHintpath = includeName;
            dependency.Setup(d => d.TryGetHintPath(out anydependencyHintpath)).Returns(true);

            return dependency;
        }

        private static MockPackageRepository CreateMockRepository()
        {
            var mockRepo = new MockPackageRepository();
            mockRepo.AddPackage(PackageUtility.CreatePackage(PackageInRepository, isLatest: true, assemblyReferences: new List<string> { AssemblyInPackageRepository, "Assembly12.dll" }, dependencies: new List<PackageDependency>()));
            mockRepo.AddPackage(PackageUtility.CreatePackage("Test2", isLatest: true, assemblyReferences: new List<string> { "Assembly21.dll", "Assembly22.dll" }, dependencies: new List<PackageDependency> { new PackageDependency(PackageInRepository) }));
            return mockRepo;
        }
    }
}
