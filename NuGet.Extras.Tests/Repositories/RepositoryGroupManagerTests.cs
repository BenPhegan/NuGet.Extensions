using System;
using System.Linq;
using NUnit.Framework;
using NuGet.Extras.Repositories;
using NuGet.Extras.Tests.TestObjects;
using System.IO;

namespace NuGet.Extras.Tests.Repositories
{
    [TestFixture]
    public class RepositoryGroupManagerTests
    {
        string baseRepositoriesConfig = @"<?xml version='1.0' encoding='utf-8'?>
                                            <repositories>
                                              <repository path='..\Project1\packages.config' />
                                              <repository path='..\Project2\packages.config' />
                                              <repository path='..\Project3\packages.config' />
                                            </repositories>";

        MockFileSystem mfs; 

        [TestFixtureSetUp]
        public void Setup()
        {
            mfs = new MockFileSystem();
            mfs.CreateDirectory(@"c:\files");
            mfs.CreateDirectory(@"c:\files\TestSolution");
            mfs.CreateDirectory(@"c:\files\TestSolution\packages");
            mfs.AddFile(@"c:\files\TestSolution\packages\repositories.config", baseRepositoriesConfig);
            mfs.AddFile(@"c:\files\TestSolution\repositories.config", baseRepositoriesConfig);
            mfs.CreateDirectory(@"c:\random\empty");
        }

        [TestCase(@"c:\files\TestSolution\packages\repositories.config", 1)]
        [TestCase(@"c:\files\TestSolution\packages", 1)]
        [TestCase(@"c:\files\TestSolution", 2)]
        [TestCase(@"c:\random", 0)]
        public void ConstructParser(string repositoryConfigPath,int repositoryCount)
        {
            var repositoryManager = new RepositoryGroupManager(repositoryConfigPath, mfs);
            Assert.IsNotNull(repositoryManager);
            Assert.AreEqual(repositoryCount, repositoryManager.RepositoryManagers.Count());
        }

        [TestCase(@"c:\files\TestSolution\Project1\packages.config")]
        [TestCase(@"c:\files\TestSolution\Project2\packages.config")]
        [ExpectedException(typeof(System.ArgumentOutOfRangeException))]
        public void ConstructorException(string repositoryConfigPath)
        {
            var mfs = new MockFileSystem();
            mfs.AddFile(repositoryConfigPath);
            new RepositoryGroupManager(repositoryConfigPath, mfs);
        }

        [TestCase(@"c:\files\TestSolution\packages\repositories.config",Ignore = true, IgnoreReason = "Broken, need to move tests to IPackageMangerExtensions")]
        public void CanCleanPackageFolders(string repositoryConfig)
        {
            var repositoryGroupManager = new RepositoryGroupManager(repositoryConfig, mfs);

            foreach (var repositoryManager in repositoryGroupManager.RepositoryManagers)
            {
                if (repositoryManager.RepositoryConfig.Directory != null)
                {
                    mfs.CreateDirectory(Path.Combine(repositoryManager.RepositoryConfig.Directory.Name, "Test"));
                }
            }

            //repositoryGroupManager.CleanPackageFolders();

            foreach (var repositoryManager in repositoryGroupManager.RepositoryManagers)
            {
                if (repositoryManager.RepositoryConfig.Directory != null)
                    Assert.AreEqual(0, mfs.GetDirectories(repositoryManager.RepositoryConfig.Directory.Name).Count());
            }
        }
    }
}
