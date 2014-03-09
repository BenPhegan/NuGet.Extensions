using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Moq;
using NuGet.Extensions.Tests.Mocks;
using NuGet.Extensions.Repositories;
using NuGet.Common;
using PackageUtility = NuGet.Extensions.Tests.TestObjects.PackageUtility;

namespace NuGet.Extensions.Tests.Repositories
{
    [TestFixture]
    public class RepositoryAssemblyResolverTests
    {
        [Test]
        public void CanFindSingleAssemblyInSinglePackage()
        {
            var fileList = new List<string>() { "Assembly.Common.dll" };
            var packages = new List<IPackage> { PackageUtility.CreatePackage("Assembly.Common", "1.0",assemblyReferences: fileList) };

            var assemblies = new List<string>() { "Assembly.Common.dll" };

            var assemblyResolver = new RepositoryAssemblyResolver(assemblies, packages.AsQueryable(), new Mock<MockFileSystem>().Object, new Mock<IConsole>().Object);
            var resolved = assemblyResolver.GetAssemblyToPackageMapping(false);
            Assert.AreEqual(1, resolved["Assembly.Common.dll"].Count);
        }

        [Test]
        public void CanFindSingleAssemblyInExhaustively()
        {
            var fileList = new List<string>() { "Assembly.Common.dll" };
            var packages = new List<IPackage> 
            { 
                PackageUtility.CreatePackage("Assembly.Common", "1.2", assemblyReferences: fileList, isLatest: true), 
                PackageUtility.CreatePackage("Assembly.Common", "1.1", assemblyReferences: fileList, isLatest: false),
                PackageUtility.CreatePackage("Assembly.Common", "1.0", assemblyReferences: fileList, isLatest: false) 
            };

            var assemblies = new List<string>() { "Assembly.Common.dll" };

            var assemblyResolver = new RepositoryAssemblyResolver(assemblies, packages.AsQueryable(), new Mock<MockFileSystem>().Object, new Mock<IConsole>().Object);
            var resolved = assemblyResolver.GetAssemblyToPackageMapping(true);
            Assert.AreEqual(3, resolved["Assembly.Common.dll"].Count);
        }

        [Test]
        public void CanFindMultipleAssembliesInSinglePackage()
        {
            var fileList = new List<string>() { "Assembly.Common.dll", "Assembly.Data.dll" };
            var packages = new List<IPackage> 
            { 
                PackageUtility.CreatePackage("Assembly.Common", "1.2", assemblyReferences: fileList, isLatest: true), 
            };

            var assemblies = new List<string>() { "Assembly.Common.dll", "Assembly.Data.dll" };

            var assemblyResolver = new RepositoryAssemblyResolver(assemblies, packages.AsQueryable(), new Mock<MockFileSystem>().Object, new Mock<IConsole>().Object);
            var resolved = assemblyResolver.GetAssemblyToPackageMapping(true);
            Assert.AreEqual(1, resolved["Assembly.Common.dll"].Count);
            Assert.AreEqual(1, resolved["Assembly.Data.dll"].Count);
        }

        [Test]
        public void CanFindMultipleAssembliesInMultiplePackagesExhaustively()
        {
            var fileList = new List<string>() { "Assembly.Common.dll", "Assembly.Data.dll" };
            var packages = new List<IPackage> 
            { 
                PackageUtility.CreatePackage("Assembly.Common", "1.2", assemblyReferences: fileList, isLatest: true), 
                PackageUtility.CreatePackage("Assembly.Common", "1.1", assemblyReferences: fileList, isLatest: false), 
                PackageUtility.CreatePackage("Assembly.Common", "1.0", assemblyReferences: fileList, isLatest: false), 
                PackageUtility.CreatePackage("Assembly.Data", "1.0", assemblyReferences: new List<string>() { "Assembly.Data.dll" }, isLatest: true) 
            };

            var assemblies = new List<string>() { "Assembly.Common.dll", "Assembly.Data.dll" };

            var assemblyResolver = new RepositoryAssemblyResolver(assemblies, packages.AsQueryable(), new Mock<MockFileSystem>().Object, new Mock<IConsole>().Object);
            var resolved = assemblyResolver.GetAssemblyToPackageMapping(true);
            Assert.AreEqual(3, resolved["Assembly.Common.dll"].Count);
            Assert.AreEqual(4, resolved["Assembly.Data.dll"].Count);
        }

        [Test]
        public void CanOutputPackageConfigWithSingleEntry()
        {
            var fileList = new List<string>() { "Assembly.Common.dll" };
            var packages = new List<IPackage> { PackageUtility.CreatePackage("Assembly.Common", "1.0", assemblyReferences: fileList) };

            var assemblies = new List<string>() { "Assembly.Common.dll" };
            var filesystem = new MockFileSystem();
            //filesystem.Root = @"c:\test";

            var assemblyResolver = new RepositoryAssemblyResolver(assemblies, packages.AsQueryable(), filesystem, new Mock<IConsole>().Object);
            var resolved = assemblyResolver.GetAssemblyToPackageMapping(false);
            assemblyResolver.OutputPackageConfigFile();
            Assert.AreEqual(1,filesystem.Paths.Count);
            var file = new PackageReferenceFile(filesystem, string.Concat(filesystem.Root, ".\\packages.config"));
            Assert.AreEqual(1, file.GetPackageReferences().Count());
        }

        [Test]
        public void WillChoosePackageWithSmallestNumberOfAssembliesFromMultipleMatches()
        {
            var packages = new List<IPackage> 
            {
                PackageUtility.CreatePackage("Assembly.Common", "1.0", assemblyReferences: new List<string>() { "Assembly.Common.dll" }),
                PackageUtility.CreatePackage("Assembly.Other", "1.0", assemblyReferences: new List<string>() { "Assembly.Common.dll", "Assembly.Other.dll" }) 
            };

            var assemblies = new List<string>() { "Assembly.Common.dll" };
            var filesystem = new MockFileSystem();

            var assemblyResolver = new RepositoryAssemblyResolver(assemblies, packages.AsQueryable(), filesystem, new Mock<IConsole>().Object);
            var resolved = assemblyResolver.GetAssemblyToPackageMapping(false);
            assemblyResolver.OutputPackageConfigFile();
            Assert.AreEqual(1, filesystem.Paths.Count);
            var file = new PackageReferenceFile(filesystem, string.Concat(filesystem.Root, ".\\packages.config"));
            Assert.AreEqual(1, file.GetPackageReferences().Count());
            Assert.AreEqual(true, file.EntryExists("Assembly.Common",SemanticVersion.Parse("1.0")));
        }
    }
}
