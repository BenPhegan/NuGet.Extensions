using System.IO;
using System.Linq;
using NuGet.Extensions.PackageReferences;
using NuGet.Extensions.Packages;
using NuGet.Extensions.Tests.Mocks;
using NuGet.Extensions.Repositories;
using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using NuGet.Extensions.Comparers;

namespace NuGet.Extensions.Tests.Packages
{
    [TestFixture]
    public class PackageAggregatorTests
    {
        private IRepositoryManager _repositoryManager;
        private PackageAggregator _packageAggregator;

        [SetUp]
        public void SetUp()
        {
            var packageFiles = GetPackageReferenceFileList();

            var repoManagerMock = new Mock<IRepositoryManager>();
            repoManagerMock.Setup(r => r.PackageReferenceFiles).Returns(packageFiles);

            _repositoryManager = repoManagerMock.Object;
        }


        [TestCase]
        public void ConstructorTest()
        {
            var fileSystem = new Mock<MockFileSystem>();
            _packageAggregator = new PackageAggregator(fileSystem.Object, _repositoryManager, new PackageEnumerator());
            Assert.AreSame(_repositoryManager, _packageAggregator.RepositoryManager);
            Assert.IsNotNull(_packageAggregator.Packages);
        }

        //TODO Not much to test here really....
        [TestCase(2, 0, Description = "Using version stated")]
        public void AggregateCount(int expectedCanResolveCount, int expectedCannotResolveCount)
        {
            var fileSystem = new Mock<MockFileSystem>();
            _packageAggregator = new PackageAggregator(fileSystem.Object, _repositoryManager, new PackageEnumerator());
            _packageAggregator.Compute((s, s1) => { }, PackageReferenceEqualityComparer.Id, new PackageReferenceSetResolver());
            Assert.AreEqual(expectedCanResolveCount, _packageAggregator.Packages.Count());
            Assert.AreEqual(expectedCannotResolveCount, _packageAggregator.PackageResolveFailures.Count());
        }

        [Test]
        public void SaveTo()
        {
            var fileSystem = new Mock<MockFileSystem>(){CallBase = true};
            _packageAggregator = new PackageAggregator(fileSystem.Object, _repositoryManager, new PackageEnumerator());
            FileInfo file = _packageAggregator.Save(@".");
            Assert.IsTrue(fileSystem.Object.Paths.ContainsKey(file.ToString()));
        }

        [Test]
        public void SaveToTemp()
        {
            var fileSystem = new Mock<MockFileSystem>() { CallBase = true };
            _packageAggregator = new PackageAggregator(fileSystem.Object, _repositoryManager, new PackageEnumerator());

            FileInfo file = _packageAggregator.Save();
            Assert.IsTrue(fileSystem.Object.Paths.ContainsKey(file.ToString()));
        }

        private static List<PackageReferenceFile> GetPackageReferenceFileList()
        {
            var x1 = @"<?xml version='1.0' encoding='utf-8'?>
                    <packages>
                      <package id='Package1' version='1.0' allowedVersions='[1.0, 2.0)' />
                      <package id='Package2' version='2.0' />
                    </packages>";

            var x2 = @"<?xml version='1.0' encoding='utf-8'?>
                    <packages>
                      <package id='Package1' version='2.0' allowedVersions='[1.0, 3.0)' />
                      <package id='Package2' version='2.0' />
                    </packages>";

            var mfs = new Mock<MockFileSystem>();
            mfs.Setup(m => m.OpenFile("x1")).Returns(x1.AsStream());
            mfs.Setup(m => m.OpenFile("x2")).Returns(x2.AsStream());
            mfs.Setup(m => m.FileExists(It.IsAny<string>())).Returns(true);

            var mpr1 = new PackageReferenceFile(mfs.Object, "x1");
            var mpr2 = new PackageReferenceFile(mfs.Object, "x2");

            var packageFiles = new List<PackageReferenceFile>();
            packageFiles.Add(mpr1);
            packageFiles.Add(mpr2);
            return packageFiles;
        }
    }
}
