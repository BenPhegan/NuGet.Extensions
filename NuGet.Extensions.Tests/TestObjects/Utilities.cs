using System.Collections.Generic;
using Moq;
using NuGet.Extensions.Tests.Mocks;

namespace NuGet.Extensions.Tests.TestObjects
{
    static internal class Utilities
    {
        public static IPackageRepositoryFactory GetFactory()
        {
            var repositoryA = new MockPackageRepository
                                  {
                                      PackageUtility.CreatePackage("Assembly.Common"),
                                      PackageUtility.CreatePackage("Assembly.Common", "1.1"),
                                      PackageUtility.CreatePackage("Assembly.Common", "1.2"),
                                      PackageUtility.CreatePackage("Assembly.Common", "1.3"),
                                      PackageUtility.CreatePackage("Assembly.Common", "2.0", isLatest: true),
                                      PackageUtility.CreatePackage("Assembly.Data"),
                                      PackageUtility.CreatePackage("Assembly.Data", "1.1"),
                                      PackageUtility.CreatePackage("Assembly.Data", "2.0"),
                                      PackageUtility.CreatePackage("Assembly.Data", "2.1", isLatest: true),
                                  };
            var repositoryB = new MockPackageRepository
                                  {
                                      PackageUtility.CreatePackage("Assembly.Common"),
                                      PackageUtility.CreatePackage("Assembly.Common", "1.1"),
                                      PackageUtility.CreatePackage("Assembly.Common", "1.2"),
                                      PackageUtility.CreatePackage("Assembly.Common", "2.0"),
                                      PackageUtility.CreatePackage("Assembly.Common", "2.1", isLatest: true),
                                      PackageUtility.CreatePackage("Assembly.Data"),
                                      PackageUtility.CreatePackage("Assembly.Data", "1.1"),
                                      PackageUtility.CreatePackage("Assembly.Data", "1.2"),
                                      PackageUtility.CreatePackage("Assembly.Data", "2.0"),
                                      PackageUtility.CreatePackage("Assembly.Data", "2.1", isLatest: true),
                                  };

            var factory = new Mock<IPackageRepositoryFactory>();

            factory.Setup(c => c.CreateRepository(It.Is<string>(f => f.Equals("Dev")))).Returns(repositoryA);
            factory.Setup(c => c.CreateRepository(It.Is<string>(f => f.Equals("Release")))).Returns(repositoryB);
            factory.Setup(c => c.CreateRepository(It.Is<string>(f => f.Equals("SingleAggregate")))).Returns(
                new AggregateRepository(new List<IPackageRepository> {repositoryA}));
            factory.Setup(c => c.CreateRepository(It.Is<string>(f => f.Equals("MultiAggregate")))).Returns(
                new AggregateRepository(new List<IPackageRepository>() { repositoryA, repositoryB }));

            return factory.Object;
        }

        public static IPackageSourceProvider GetSourceProvider(IEnumerable<PackageSource> sources = null)
        {
            var sourceProvider = new Mock<IPackageSourceProvider>();
            sources = sources ?? new[] {new PackageSource("Dev", "Development Feed"), new PackageSource("Release", "Release Feed")};
            sourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);

            return sourceProvider.Object;
        }

        public static Mock<MockFileSystem> CreatePopulatedMockFileSystem()
        {
            var fileSystem = new Mock<MockFileSystem> {CallBase = true};

            fileSystem.Object.CreateDirectory(@"c:\TestSolution");
            fileSystem.Object.CreateDirectory(@"c:\TestSolution\Project1");
            fileSystem.Object.CreateDirectory(@"c:\TestSolution\Project2");
            fileSystem.Object.CreateDirectory(@"c:\TestSolution\packages");

            fileSystem.Object.AddFile(@"c:\TestSolution\Project1\packages.config",
                                      @"<?xml version='1.0' encoding='utf-8'?>
                                <packages>
                                  <package id='Assembly.Common' version='2.0' allowedVersions='[1.0, 3.0)' />
                                  <package id='Assembly.Data' version='1.0' />
                                </packages>");

            fileSystem.Object.AddFile(@"c:\TestSolution\Project2\packages.config",
                                      @"<?xml version='1.0' encoding='utf-8'?>
                                <packages>
                                  <package id='Assembly.Common' version='1.0' allowedVersions='[1.0, 2.0)' />
                                  <package id='Assembly.Data' version='2.0' />
                                </packages>");

            fileSystem.Object.AddFile(@"c:\TestSolution\packages\repositories.config",
                                      @"<?xml version='1.0' encoding='utf-8'?>
                                    <repositories>
                                      <repository path='..\Project1\packages.config' />
                                      <repository path='..\Project2\packages.config' />
                                      <repository path='..\Project3\packages.config' /> <!--This file doesn't exist, it is here to test default behaviour-->
                                    </repositories>");

            return fileSystem;
        }
    }
}