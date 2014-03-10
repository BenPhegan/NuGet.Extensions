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
        private const string AssemblyCommonDll = AssemblyCommon + ".dll";
        private const string AssemblyDataDll = AssemblyData + ".dll";
        private const string AssemblyCommon = "Assembly.Common";
        private const string AssemblyData = "Assembly.Data";

        [TestCase(1, Description = "Normal single assembly reference")]
        [TestCase(2, Description = "Can resolve the multiple versions of the same named assembly a project could reference")]
        public void CanFindAssemblyInSinglePackage(int numberOfRepeatedAssemblies)
        {
            var fileList = new List<string>() { AssemblyCommonDll };
            var packages = new List<IPackage> { PackageUtility.CreatePackage(AssemblyCommon, "1.0",assemblyReferences: fileList) };

            var assemblies = Enumerable.Repeat(AssemblyCommonDll, numberOfRepeatedAssemblies).ToList();

            var assemblyResolver = new RepositoryAssemblyResolver(assemblies, packages.AsQueryable(), new Mock<MockFileSystem>().Object, new Mock<IConsole>().Object);
            var resolved = assemblyResolver.GetAssemblyToPackageMapping(false).ResolvedMappings;
            Assert.AreEqual(1, resolved[AssemblyCommonDll].Count);
        }

        [Test]
        public void CanFindSingleAssemblyInExhaustively()
        {
            var fileList = new List<string>() { AssemblyCommonDll };
            var packages = new List<IPackage> 
            { 
                PackageUtility.CreatePackage(AssemblyCommon, "1.2", assemblyReferences: fileList, isLatest: true), 
                PackageUtility.CreatePackage(AssemblyCommon, "1.1", assemblyReferences: fileList, isLatest: false),
                PackageUtility.CreatePackage(AssemblyCommon, "1.0", assemblyReferences: fileList, isLatest: false) 
            };

            var assemblies = new List<string>() { AssemblyCommonDll };

            var assemblyResolver = new RepositoryAssemblyResolver(assemblies, packages.AsQueryable(), new Mock<MockFileSystem>().Object, new Mock<IConsole>().Object);
            var resolved = assemblyResolver.GetAssemblyToPackageMapping(true).ResolvedMappings;
            Assert.AreEqual(3, resolved[AssemblyCommonDll].Count);
        }

        [Test]
        public void CanFindMultipleAssembliesInSinglePackage()
        {
            var fileList = new List<string>() { AssemblyCommonDll, AssemblyDataDll };
            var packages = new List<IPackage> 
            { 
                PackageUtility.CreatePackage(AssemblyCommon, "1.2", assemblyReferences: fileList, isLatest: true), 
            };

            var assemblies = new List<string>() { AssemblyCommonDll, AssemblyDataDll };

            var assemblyResolver = new RepositoryAssemblyResolver(assemblies, packages.AsQueryable(), new Mock<MockFileSystem>().Object, new Mock<IConsole>().Object);
            var resolved = assemblyResolver.GetAssemblyToPackageMapping(true).ResolvedMappings;
            Assert.AreEqual(1, resolved[AssemblyCommonDll].Count);
            Assert.AreEqual(1, resolved[AssemblyDataDll].Count);
        }

        [Test]
        public void CanFindMultipleAssembliesInMultiplePackagesExhaustively()
        {
            var fileList = new List<string>() { AssemblyCommonDll, AssemblyDataDll };
            var packages = new List<IPackage> 
            { 
                PackageUtility.CreatePackage(AssemblyCommon, "1.2", assemblyReferences: fileList, isLatest: true), 
                PackageUtility.CreatePackage(AssemblyCommon, "1.1", assemblyReferences: fileList, isLatest: false), 
                PackageUtility.CreatePackage(AssemblyCommon, "1.0", assemblyReferences: fileList, isLatest: false), 
                PackageUtility.CreatePackage(AssemblyData, "1.0", assemblyReferences: new List<string>() { AssemblyDataDll }, isLatest: true) 
            };

            var assemblies = new List<string>() { AssemblyCommonDll, AssemblyDataDll };

            var assemblyResolver = new RepositoryAssemblyResolver(assemblies, packages.AsQueryable(), new Mock<MockFileSystem>().Object, new Mock<IConsole>().Object);
            var resolved = assemblyResolver.GetAssemblyToPackageMapping(true).ResolvedMappings;
            Assert.AreEqual(3, resolved[AssemblyCommonDll].Count);
            Assert.AreEqual(4, resolved[AssemblyDataDll].Count);
        }

        [Test]
        public void CanOutputPackageConfigWithSingleEntry()
        {
            var fileList = new List<string>() { AssemblyCommonDll };
            var packages = new List<IPackage> { PackageUtility.CreatePackage(AssemblyCommon, "1.0", assemblyReferences: fileList) };

            var assemblies = new List<string>() { AssemblyCommonDll };
            var filesystem = new MockFileSystem();
            //filesystem.Root = @"c:\test";

            var assemblyResolver = new RepositoryAssemblyResolver(assemblies, packages.AsQueryable(), filesystem, new Mock<IConsole>().Object);
            var resolved = assemblyResolver.GetAssemblyToPackageMapping(false);
            resolved.OutputPackageConfigFile();
            Assert.AreEqual(1,filesystem.Paths.Count);
            var file = new PackageReferenceFile(filesystem, string.Concat(filesystem.Root, ".\\packages.config"));
            Assert.AreEqual(1, file.GetPackageReferences().Count());
        }

        [Test]
        public void WillChoosePackageWithSmallestNumberOfAssembliesFromMultipleMatches()
        {
            var packages = new List<IPackage> 
            {
                PackageUtility.CreatePackage(AssemblyCommon, "1.0", assemblyReferences: new List<string>() { AssemblyCommonDll }),
                PackageUtility.CreatePackage("Assembly.Other", "1.0", assemblyReferences: new List<string>() { AssemblyCommonDll, "Assembly.Other.dll" }) 
            };

            var assemblies = new List<string>() { AssemblyCommonDll };
            var filesystem = new MockFileSystem();

            var assemblyResolver = new RepositoryAssemblyResolver(assemblies, packages.AsQueryable(), filesystem, new Mock<IConsole>().Object);
            var resolved = assemblyResolver.GetAssemblyToPackageMapping(false);
            resolved.OutputPackageConfigFile();
            Assert.AreEqual(1, filesystem.Paths.Count);
            var file = new PackageReferenceFile(filesystem, string.Concat(filesystem.Root, ".\\packages.config"));
            Assert.AreEqual(1, file.GetPackageReferences().Count());
            Assert.AreEqual(true, file.EntryExists(AssemblyCommon,SemanticVersion.Parse("1.0")));
        }
    }
}
