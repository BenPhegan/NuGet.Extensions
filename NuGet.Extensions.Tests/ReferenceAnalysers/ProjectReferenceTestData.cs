using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.Tests.Mocks;

namespace NuGet.Extensions.Tests.ReferenceAnalysers
{
    public class ProjectReferenceTestData
    {
        public const string AssemblyFilenameInPackageRepository = "Assembly11.dll";
        public const string AnotherAssemblyInPackageRepository = "Assembly21.dll";
        public const string PackageInRepository = "Test1";
        public static readonly Guid ProjectWithDependenciesGuid = new Guid("{5F49060A-3F64-4227-90C4-09F54783F1EC}");

        public static Mock<IVsProject> ConstructMockProject(IReference[] references = null, string outputAssembly = null)
        {
            var project = new Mock<IVsProject>();
            project.Setup(proj => proj.GetBinaryReferences()).Returns(references ?? new IReference[0]);
            outputAssembly = outputAssembly ?? "randomAssemblyName" + Path.GetTempFileName();
            project.SetupGet(p => p.AssemblyName).Returns(outputAssembly);
            return project;
        }

        public static Mock<IReference> ConstructMockDependency(string includeFilename = null, string includeVersion = null, bool hasHintpath = true)
        {
            includeFilename = includeFilename ?? AssemblyFilenameInPackageRepository;

            var dependency = new Mock<IReference>();
            dependency.SetupGet(d => d.AssemblyName).Returns(Path.GetFileNameWithoutExtension(includeFilename));
            dependency.SetupGet(d => d.AssemblyFilename).Returns(includeFilename);
            dependency.SetupGet(d => d.AssemblyVersion).Returns(includeVersion ?? "0.0.0.0");
            dependency.Setup(d => d.AssemblyFilenameEquals(includeFilename)).Returns(true);

            string anydependencyHintpath = includeFilename;
            dependency.Setup(d => d.TryGetHintPath(out anydependencyHintpath)).Returns(hasHintpath);

            return dependency;
        }

        public static MockPackageRepository CreateMockRepository()
        {
            var mockRepo = new MockPackageRepository();
            mockRepo.AddPackage(PackageUtility.CreatePackage(PackageInRepository, isLatest: true, assemblyReferences: new List<string> { AssemblyFilenameInPackageRepository, "Assembly12.dll" }, dependencies: new List<PackageDependency>()));
            mockRepo.AddPackage(PackageUtility.CreatePackage("Test2", isLatest: true, assemblyReferences: new List<string> { AnotherAssemblyInPackageRepository, "Assembly22.dll" }, dependencies: new List<PackageDependency> { new PackageDependency(PackageInRepository) }));
            return mockRepo;
        }
    }
}