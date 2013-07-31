using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using Moq;

namespace NuGet.Extensions.Tests.Mocks
{
    public class PackageUtility
    {
        public static IPackage CreateProjectLevelPackage(string id, string version = "1.0", IEnumerable<PackageDependency> dependencies = null)
        {
            return CreatePackage(id, version, assemblyReferences: new[] { id + ".dll" }, dependencies: dependencies);
        }

        public static IPackage CreatePackage(string id,
                                              string version = "1.0",
                                              IEnumerable<string> content = null,
                                              IEnumerable<string> assemblyReferences = null,
                                              IEnumerable<string> tools = null,
                                              IEnumerable<PackageDependency> dependencies = null,
                                              double? rating = null,
                                              string description = null,
                                              Boolean isLatest = false)
        {
            assemblyReferences = assemblyReferences ?? Enumerable.Empty<string>();
            return CreatePackage(id,
                                 version,
                                 content,
                                 CreateAssemblyReferences(assemblyReferences),
                                 tools,
                                 dependencies,
                                 rating,
                                 description,
                                 isLatest);
        }

        public static IPackage CreatePackage(string id,
                                              string version,
                                              IEnumerable<string> content,
                                              IEnumerable<IPackageAssemblyReference> assemblyReferences,
                                              IEnumerable<string> tools,
                                              IEnumerable<PackageDependency> dependencies,
                                              double? rating,
                                              string description,
                                              bool isLatest)
        {
            content = content ?? Enumerable.Empty<string>();
            assemblyReferences = assemblyReferences ?? Enumerable.Empty<IPackageAssemblyReference>();
            dependencies = dependencies ?? Enumerable.Empty<PackageDependency>();
            tools = tools ?? Enumerable.Empty<string>();
            description = description ?? "Mock package " + id;

            var allFiles = new List<IPackageFile>();
            allFiles.AddRange(CreateFiles(content, "content"));
            allFiles.AddRange(CreateFiles(tools, "tools"));
            allFiles.AddRange(assemblyReferences);

            var dependencySet = new PackageDependencySet(new FrameworkName(".NET Framework, Version=4.0"), dependencies);

            var mockPackage = new Mock<IPackage>() { CallBase = true };
            mockPackage.Setup(m => m.IsLatestVersion).Returns(isLatest);
            mockPackage.Setup(m => m.Id).Returns(id);
            //mockPackage.Setup(m => m.Published).Returns(DateTimeOffset.Now);
            mockPackage.Setup(m => m.Version).Returns(new SemanticVersion(version));
            mockPackage.Setup(m => m.GetFiles()).Returns(allFiles);
            mockPackage.Setup(m => m.AssemblyReferences).Returns(assemblyReferences);
            mockPackage.Setup(m => m.DependencySets).Returns(new[] { dependencySet });
            mockPackage.Setup(m => m.Description).Returns(description);
            mockPackage.Setup(m => m.Language).Returns("en-US");
            mockPackage.Setup(m => m.Authors).Returns(new[] { "Tester" });
            mockPackage.Setup(m => m.GetStream()).Returns(() => new MemoryStream());
            mockPackage.Setup(m => m.LicenseUrl).Returns(new Uri("ftp://test/somelicense.txts"));
            return mockPackage.Object;
        }

        private static List<IPackageAssemblyReference> CreateAssemblyReferences(IEnumerable<string> fileNames)
        {
            var assemblyReferences = new List<IPackageAssemblyReference>();
            foreach (var fileName in fileNames)
            {
                var mockAssemblyReference = new Mock<IPackageAssemblyReference>();
                mockAssemblyReference.Setup(m => m.GetStream()).Returns(() => new MemoryStream());
                mockAssemblyReference.Setup(m => m.Path).Returns(fileName);
                mockAssemblyReference.Setup(m => m.Name).Returns(Path.GetFileName(fileName));
                assemblyReferences.Add(mockAssemblyReference.Object);
            }
            return assemblyReferences;
        }

        public static IPackageAssemblyReference CreateAssemblyReference(string path, FrameworkName targetFramework)
        {
            var mockAssemblyReference = new Mock<IPackageAssemblyReference>();
            mockAssemblyReference.Setup(m => m.GetStream()).Returns(() => new MemoryStream());
            mockAssemblyReference.Setup(m => m.Path).Returns(path);
            mockAssemblyReference.Setup(m => m.Name).Returns(path);
            mockAssemblyReference.Setup(m => m.TargetFramework).Returns(targetFramework);
            mockAssemblyReference.Setup(m => m.SupportedFrameworks).Returns(new[] { targetFramework });
            return mockAssemblyReference.Object;
        }

        public static List<IPackageFile> CreateFiles(IEnumerable<string> fileNames, string directory = "")
        {
            var files = new List<IPackageFile>();
            foreach (var fileName in fileNames)
            {
                string path = Path.Combine(directory, fileName);
                var mockFile = new Mock<IPackageFile>();
                mockFile.Setup(m => m.Path).Returns(path);
                mockFile.Setup(m => m.GetStream()).Returns(() => new MemoryStream(Encoding.Default.GetBytes(path)));
                files.Add(mockFile.Object);
            }
            return files;
        }

        public static ZipPackage GetZipPackage(string id, string version)
        {
            PackageBuilder builder = new PackageBuilder();
            builder.Id = id;
            builder.Version = SemanticVersion.Parse(version);
            builder.Authors.Add("David");
            builder.Description = "This is a test package";
            builder.ReleaseNotes = "This is a release note.";
            builder.Copyright = "Copyright";
            builder.Files.AddRange(PackageUtility.CreateFiles(new[] { @"lib\foo" }));
            var ms = new MemoryStream();
            builder.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var p = new ZipPackage(ms);
            return p;
        }
    }
}
