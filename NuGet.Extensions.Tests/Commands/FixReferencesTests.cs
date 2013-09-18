using System.Linq;
using NUnit.Framework;
using NuGet.Extensions.Commands;
using NuGet.Extensions.Tests.Mocks;
using Console = NuGet.Common.Console;

namespace NuGet.Extensions.Tests.Commands
{
    public class FixReferencesTests
    {
        [Test]
        public void CanDetectMissingVersionAndUpdateToValid()
        {
            var fileSystem = new MockFileSystem(@"d:\");
            fileSystem.AddFile(@"d:\test\packages.config",
                "<packages>" +
                    "<package id=\"Test\" version=\"1.0.0.0\" />" +
                    "<package id=\"Other\" version=\"1.0.0.0\" />" +
                "</packages>");

            var repository = new MockPackageRepository("http://test.com");
            repository.AddPackage(PackageUtility.CreatePackage("Test", "2.0.0.0"));
            repository.AddPackage(PackageUtility.CreatePackage("Other", "1.0.0.0"));

            var command = new FixReferences(fileSystem, repository, new Console()) {Directory = @"d:\test"};
            command.Execute();

            var packageReferences = new PackageReferenceFile(fileSystem, @"d:\test\packages.config").GetPackageReferences().ToList();
            Assert.AreEqual("2.0.0.0", packageReferences.First(p => p.Id == "Test").Version.Version.ToString());
            Assert.AreEqual("1.0.0.0", packageReferences.First(p => p.Id == "Other").Version.Version.ToString());
        }

        [Test]
        public void ReferenceToPackageIdNotOnFeedStays()
        {
            var fileSystem = new MockFileSystem(@"d:\");
            fileSystem.AddFile(@"d:\test\packages.config",
                "<packages>" +
                    "<package id=\"Missing\" version=\"1.0.0.0\" />" +
                "</packages>");

            var repository = new MockPackageRepository("http://test.com");

            var command = new FixReferences(fileSystem, repository, new Console()) { Directory = @"d:\test" };
            command.Execute();

            var packageReferences = new PackageReferenceFile(fileSystem, @"d:\test\packages.config").GetPackageReferences().ToList();
            Assert.AreEqual("1.0.0.0", packageReferences.First(p => p.Id == "Missing").Version.Version.ToString());
        }
    }
} 
