using System.Collections.Generic;
using System.IO;
using Moq;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.Tests.Mocks;

namespace NuGet.Extensions.Tests.ReferenceAnalysers
{
    public class ProjectReferenceTestData {
        private const string AssemblyInPackageRepository = "Assembly11.dll";
        public const string PackageInRepository = "Test1";

        public static Mock<IVsProject> ConstructMockProject(IBinaryReference[] binaryReferences = null, string outputAssembly = null)
        {
            var project = new Mock<IVsProject>();
            project.Setup(proj => proj.GetBinaryReferences()).Returns(binaryReferences ?? new IBinaryReference[0]);
            outputAssembly = outputAssembly ?? "randomAssemblyName" + Path.GetTempFileName();
            project.SetupGet(p => p.AssemblyName).Returns(outputAssembly);
            return project;
        }

        public static Mock<IBinaryReference> ConstructMockDependency(string includeName = null, string includeVersion = null)
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

        public static MockPackageRepository CreateMockRepository()
        {
            var mockRepo = new MockPackageRepository();
            mockRepo.AddPackage(PackageUtility.CreatePackage(PackageInRepository, isLatest: true, assemblyReferences: new List<string> { AssemblyInPackageRepository, "Assembly12.dll" }, dependencies: new List<PackageDependency>()));
            mockRepo.AddPackage(PackageUtility.CreatePackage("Test2", isLatest: true, assemblyReferences: new List<string> { "Assembly21.dll", "Assembly22.dll" }, dependencies: new List<PackageDependency> { new PackageDependency(PackageInRepository) }));
            return mockRepo;
        }
    }
}