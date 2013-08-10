using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using NuGet.Extras.Tests.TestObjects;
using NuGet.Extras.ExtensionMethods;

namespace NuGet.Extras.Tests.Extensions
{
    [TestFixture]
    public class AggregateRepositoryExtensionsTests
    {
        AggregateRepository ar;

        [SetUp]
        public void Setup()
        {
            var mfs = new Mock<MockFileSystem>() { CallBase = true };
            var pr = new DefaultPackagePathResolver(mfs.Object);
            var mc = MachineCache.Default;
            var l = new LocalPackageRepository(pr, mfs.Object);

            var r1 = new DataServicePackageRepository(new Uri(@"http://nuget.org"));
            var r2 = new DataServicePackageRepository(new Uri(@"http://beta.nuget.org"));

            ar = new AggregateRepository(new List<IPackageRepository>() { mc, l, r1, r2 });
        }

        [Test]
        public void GetRemoteOnlyReturnsOnlyRemotes()
        {
            Assert.AreEqual(2, ar.GetRemoteOnlyAggregateRepository().Repositories.Count());
            Assert.AreEqual(typeof(DataServicePackageRepository), ar.GetRemoteOnlyAggregateRepository().Repositories.ToArray()[0].GetType());
            Assert.AreEqual(typeof(DataServicePackageRepository), ar.GetRemoteOnlyAggregateRepository().Repositories.ToArray()[1].GetType());
        }

        [Test]
        public void GetLocalOnlyReturnsOnlyLocalAndNoMachineCache()
        {
            Assert.AreEqual(1, ar.GetLocalOnlyAggregateRepository().Repositories.Count());
            Assert.AreEqual(typeof(LocalPackageRepository), ar.GetLocalOnlyAggregateRepository().Repositories.ToArray()[0].GetType());         
        }

    }
}
