using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Moq;
using NuGet.Extensions.GetLatest.Commands;
using NuGet.Extensions.Tests.Mocks;

namespace NuGet.Extensions.Tests.Commands
{
    [TestFixture]
    public class FindAssemblyTests
    {
        [Test]
        public void Test()
        {
            var fileSystem = new Mock<MockFileSystem>();
            fileSystem.Object.AddFile(@"x:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
            <packages>
            <package id=""Foo"" version=""1.0"" />
            <package id=""Baz"" version=""0.7"" />
            </packages>");



            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            var packageSourceProvider =new Mock<IPackageSourceProvider>();
            var findAssemblyCommand = new FindAssemblyTest(packageRepositoryFactory.Object, packageSourceProvider.Object, fileSystem.Object);
        }

        private static IPackageRepositoryFactory GetFactory()
        {
            var repository = new MockPackageRepository 
            { 
                PackageUtility.CreatePackage("Assembly.Common","1.1",isLatest: true), 
                PackageUtility.CreatePackage("Assembly.Common", "1.0"), 
                PackageUtility.CreatePackage("Assembly.Data", "1.0", isLatest: true) 
            };

            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository(It.Is<string>(f => f.Equals("Default source")))).Returns(repository);

            return factory.Object;
        }

        private static IPackageSourceProvider GetSourceProvider(IEnumerable<PackageSource> sources = null)
        {
            var sourceProvider = new Mock<IPackageSourceProvider>();
            sources = sources ?? new[] { new PackageSource("Default source", "Default source name") };
            sourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);

            return sourceProvider.Object;
        }


    }


    public class FindAssemblyTest : FindAssembly
    {
        private IFileSystem filesystem;

        public FindAssemblyTest(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider packageSourceProvider, IFileSystem filesystem)
        : base(packageRepositoryFactory, packageSourceProvider)
        {
            this.filesystem = filesystem;
        }
        protected override IFileSystem CreateFileSystem(string root)
        {
            if (filesystem != null)
                return filesystem;

            var mfs = new Mock<MockFileSystem>();
            mfs.Setup(m => m.Root).Returns(@"c:\test");
            return mfs.Object;
        }
    }
}
