using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using NUnit.Framework;
using Moq;
using NuGet.Extensions.Caches;
using NuGet.Extensions.Packages;
using NuGet.Extensions.Tests.TestObjects;

namespace NuGet.Extensions.Tests.Packages
{
    [TestFixture]
    public class PackageResolutionManagerTests
    {
        [Test]
        public void CanResolveFromLocalRepository()
        {
            var console = new Mock<ILogger>().Object;
            var resolver = new PackageResolutionManager(console, true, new MemoryBasedPackageCache(console));
            var remoteRepository = Utilities.GetFactory().CreateRepository("SingleAggregate");
            var localRepository = new MockPackageRepository();

            var package = new DataServicePackage()
                              {
                                  Id = "Assembly.Common",
                                  Version = "1.0"
                              };

            var test = resolver.FindPackageInAllLocalSources(localRepository, remoteRepository, package, false, false);
            Assert.AreEqual("Assembly.Common", test.Id);
        }

        [TestCase("Assembly.Common", "3.0", null, "3.0", true, Result = "3.0")]
        [TestCase("Assembly.Common", "3.0", null, "3.0", false, Result = "")]
        [TestCase("Assembly.Common", "3.0", "[2.0,3.1]", "2.9", true, Result = "2.9")]
        public string ResolvesFromCacheFirst(string id, string version, string spec, string cacheVersion, bool isLatest)
        {
            //Setup the package and versionspec for the cache
            var cachePackage = new DataServicePackage() { Id = id, Version = cacheVersion };
            var versionSpec = spec == null ? null : VersionUtility.ParseVersionSpec(spec);

            //Add to appropriate cache we want to test based on whether we have a versionspec or not.
            var console = new Mock<ILogger>().Object;
            var cache = new MemoryBasedPackageCache(console);
            if (versionSpec == null)
                cache.AddCacheEntryByIsLatest(cachePackage);
            else
                cache.AddCacheEntryByConstraint(cachePackage, versionSpec);

            //Repository does not have these versions, so any call to it will fail...
            var resolver = new PackageResolutionManager(console, isLatest, cache);
            var remoteRepository = Utilities.GetFactory().CreateRepository("SingleAggregate");
            var result = resolver.ResolveLatestInstallablePackage(remoteRepository, new PackageReference(id, SemanticVersion.Parse(version), versionSpec, null, false));
            
            //Null result when we call ResolveLastestInstallablePackage when PackageResolutionManager not using Latest
            return result == null ? "" : result.Version.ToString();
        }

        [TestCase("Assembly.Instrumentation", "1.4", null, true, Result = "2.1")]
        [TestCase("Assembly.Common", "1.4", null, true, Result = "2.1")]
        [TestCase("Assembly.Common", "2.9", "[1.0,1.3)", true, Result = "1.2")]
        [TestCase("Assembly.Common", "2.9", "[1.0,1.3]", true, Result = "1.3")]
        [TestCase("Assembly.Common", "1.3", null, false, Result = "")]
        public string ResolvesUsingMultipleRepos(string id, string version, string spec, bool isLatest)
        {
            var versionSpec = spec == null ? null : VersionUtility.ParseVersionSpec(spec);

            //Add to appropriate cache we want to test based on whether we have a versionspec or not.
            var console = new Mock<ILogger>().Object;
            var cache = new MemoryBasedPackageCache(console);

            //Repository does not have these versions, so any call to it will fail...
            var resolver = new PackageResolutionManager(console, isLatest, cache);
            var remoteRepository = Utilities.GetFactory().CreateRepository("MultiAggregate");
            var result = resolver.ResolveLatestInstallablePackage(remoteRepository, new PackageReference(id, SemanticVersion.Parse(version), versionSpec, null, false));

            //Null result when we call ResolveLastestInstallablePackage when PackageResolutionManager not using Latest
            return result == null ? "" : result.Version.ToString();
        }

        [Test]
        [Ignore("Need to sort out a set of tests here....")]
        public void WillNotUseODataIfPackageLocal()
        {
            var console = new Mock<ILogger>().Object;
            var resolver = new PackageResolutionManager(console, true, new MemoryBasedPackageCache(console));
            var odata = new Mock<DataServicePackageRepository>(new Uri(@"http://nuget.org"));
            var list = new List<IPackage>()
                           {
                               new DataServicePackage()
                                   {
                                       Id = "Assembly.Common",
                                       Version = "3.0"
                                   }
                           };
            odata.Setup(o => o.GetPackages()).Returns(list.AsQueryable());
            var remoteRepository = new AggregateRepository(new List<IPackageRepository>()
                                                               {
                                                                   Utilities.GetFactory().CreateRepository("SingleAggregate"),
                                                                   odata.Object
                                                               });
        

            var localRepository = new MockPackageRepository();

            var package = new DataServicePackage()
            {
                Id = "Assembly.Common",
                Version = "3.0"
            };

            Assert.AreEqual(true,remoteRepository.GetPackages().Contains(package));
            var local = resolver.FindPackageInAllLocalSources(localRepository, remoteRepository, package, false, false);
            Assert.AreEqual(null, local);

        }

        [Test]
        public void CanResolveInstallablePackages()
        {
            var console = new Mock<ILogger>().Object;
            var resolver = new PackageResolutionManager(console, true, new MemoryBasedPackageCache(console));
            var remoteRepository = Utilities.GetFactory().CreateRepository("SingleAggregate");

            var packageReference = new PackageReference("Assembly.Common", SemanticVersion.Parse("1.0"), new VersionSpec(), new FrameworkName(".NET Framework, Version=4.0"), false);

            var test = resolver.ResolveLatestInstallablePackage(remoteRepository, packageReference);
            Assert.AreEqual("Assembly.Common", test.Id);
        }
    }
}
