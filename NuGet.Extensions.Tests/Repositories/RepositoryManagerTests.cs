using System;
using System.Linq;
using NUnit.Framework;
using NuGet.Extensions.Tests.Mocks;
using NuGet.Extras.Repositories;
using Moq;
using System.Collections.Generic;
using NuGet.Extras.Tests.TestObjects;
using System.IO;
using NuGet.Extras.Packages;

namespace NuGet.Extras.Tests.Repositories
{
    [TestFixture]
    public class RepositoryManagerTests
    {
        #region Global Constructor Tests

        public void ConstructParser()
        {
            var mfs = new MockFileSystem();
            var correctConfig = @"<?xml version='1.0' encoding='utf-8'?>
                                    <repositories>
                                        <repository path='..\Project1\packages.config' />
                                        <repository path='..\Project2\packages.config' />
                                        <repository path='..\Project3\packages.config' />
                                    </repositories>";

            mfs.AddFile(@"c:\packages\repositories.config", correctConfig);
            mfs.AddFile(@"c:\project1\packages.config", correctConfig);
            mfs.AddFile(@"c:\project2\packages.config", correctConfig);
            mfs.AddFile(@"c:\project3\packages.config", correctConfig);
            mfs.AddFile(@"c:\packages\repositories.config", correctConfig);

            var repositoryManager = new RepositoryManager(@"c:\packages\repositories.config", new RepositoryEnumerator(mfs), mfs);
            Assert.IsNotNull(repositoryManager);
            Assert.AreEqual(3, repositoryManager.PackageReferenceFiles.Count());
        }



        [TestCase(@"..\..\files\TestSolution\Project1\packages.config", false)]
        [TestCase(@"..\..\files\TestSolution\Project2\packages.config", false)]
        [TestCase(@"..\..\files\TestSolution\packages", false)]
        [TestCase(@"..\..\files\TestSolution", false)]
        [TestCase(@"..\..\files\repositories.config", false)]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ConstructorException(string repositoryConfigPath, Boolean fileExists)
        {
            var mfs = new Mock<MockFileSystem>();
            mfs.Setup(m => m.FileExists(It.IsAny<string>())).Returns(fileExists);
            new RepositoryManager(repositoryConfigPath, new RepositoryEnumerator(mfs.Object), mfs.Object);
        }

        #endregion
        [TestCase(Ignore = true, IgnoreReason = "Broken, need to move tests to IPackageMangerExtensions")]
        public void CanCleanPackageFolders()
        {
            var mfs = new MockFileSystem();

            mfs.CreateDirectory("c:\\packages\\Component");
            mfs.AddFile("c:\\packages\\Component\\test.txt", "blah");
            mfs.AddFile("c:\\packages\\Component\\test.dll","blah");
            mfs.AddFile("c:\\packages\\repositories.config","blah");

            var re = new Mock<IRepositoryEnumerator>();
            re.Setup(r => r.GetPackageReferenceFiles(It.IsAny<FileInfo>())).Returns(new List<PackageReferenceFile>());
            var repositoryManager = new RepositoryManager(@"c:\packages\repositories.config", re.Object, mfs);
            
            Assert.AreEqual(1, mfs.GetDirectories(repositoryManager.RepositoryConfig.Directory.FullName).Count());
            
            //_repositoryManager.CleanPackageFolders();

            Assert.AreEqual(0, mfs.GetDirectories(repositoryManager.RepositoryConfig.Directory.FullName).Count());
        }

    }
}
