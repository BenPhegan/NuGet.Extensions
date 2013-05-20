using System.Collections.Generic;
using Moq;
using NuGet.Common;
using NuGet.Extensions.Caches;
using NuGet.Extensions.Tests.Mocks;
using NuGet.Extensions.Tests.TestObjects;
using NUnit.Framework;
using NuGet.Extensions.Packages;

namespace NuGet.Extensions.Tests.Configuration
{
    [TestFixture]
    public class FindPackageTests : GetCommandTestsBase
    {
        private MockFileSystem _mockFileSystem;
        private PackageManager _packageManager;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _mockFileSystem = new MockFileSystem();
            _packageManager = new PackageManager(
                AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(Utilities.GetFactory(),
                                                                               Utilities.GetSourceProvider(),
                                                                               new List<string>() { "Dev" }),
                new DefaultPackagePathResolver(_mockFileSystem), _mockFileSystem);
        }

        [TestCase("Should.Not.Exist.On.Feed", "0.0", null, true, Result = null)] // get latest for package id (0.0 version)
        [TestCase("Assembly.Common", "0.0", null, true, Result = "2.0")] // get latest for package id (0.0 version)
        [TestCase("Assembly.Data", "0.0", null, true, Result = "2.1")]  // get latest for package id (0.0 version)
        [TestCase("Assembly.Common", "1.0", null, true, Result = "2.0")] // get specific version for package id
        [TestCase("Assembly.Data", "1.1", null, true, Result = "2.1")] // get specific version for package id
        [TestCase("Assembly.Data", "1.0", "[1.0,2.0)", true, Result = "1.1")] // get latest constrained version
        [TestCase("Assembly.Data", "1.0", "[1.0,2.0]", true, Result = "2.0")] // get latest constrained version
        [TestCase("Assembly.Data", "1.0", "[1.0,2.1)", true, Result = "2.0")] // get latest constrained version
        [TestCase("Assembly.Data", "1.0", "[1.0,2.1]", true, Result = "2.1")] // get latest constrained version
        [TestCase("Assembly.Common", "1.0", "[1.0,1.2)", true, Result = "1.1")] // get latest constrained version
        [TestCase("Assembly.Common", "1.0", "[1.0,1.2]", true, Result = "1.2")] // get latest constrained version
        [TestCase("Assembly.Common", "1.0", "[1.0,2.0)", true, Result = "1.3")] // get latest constrained version
        [TestCase("Assembly.Common", "1.0", "[1.0,2.0]", true, Result = "2.0")] // get latest constrained version
        [TestCase("Assembly.Data", "0.0", "[1.0,2.0)", true, Result = "1.1")] // get latest constrained version (0.0 version)
        [TestCase("Assembly.Data", "0.0", "[1.0,2.0]", true, Result = "2.0")] // get latest constrained version (0.0 version)
        [TestCase("Assembly.Data", "0.0", "[1.0,2.1)", true, Result = "2.0")] // get latest constrained version (0.0 version)
        [TestCase("Assembly.Data", "0.0", "[1.0,2.1]", true, Result = "2.1")] // get latest constrained version (0.0 version)
        [TestCase("Assembly.Common", "0.0", "[1.0,1.2)", true, Result = "1.1")] // get latest constrained version (0.0 version)
        [TestCase("Assembly.Common", "0.0", "[1.0,1.2]", true, Result = "1.2")] // get latest constrained version (0.0 version)
        [TestCase("Assembly.Common", "0.0", "[1.0,2.0)", true, Result = "1.3")] // get latest constrained version (0.0 version)
        [TestCase("Assembly.Common", "0.0", "[1.0,2.0]", true, Result = "2.0")] // get latest constrained version (0.0 version)

        [TestCase("Assembly.Common", "0.0", null, false, Result = "0.0")] // get latest for package id (0.0 version)
        [TestCase("Assembly.Common", "1.0", null, false, Result = "1.0")] // get specific version for package id
        [TestCase("Assembly.Data", "1.1", null, false, Result = "1.1")] // get specific version for package id
        [TestCase("Assembly.Data", "1.0", "[1.0,2.0)", false, Result = "1.0")] // get latest constrained version
        [TestCase("Assembly.Common", "0.0", "[1.0,2.0]", false, Result = "0.0")] // get latest constrained version (0.0 version)
        public string FindPackageLatestTests(string id, string version, string versionSpec, bool latest)
        {
            var pr = new PackageReference(id, SemanticVersion.Parse(version), string.IsNullOrEmpty(versionSpec) ? new VersionSpec() : VersionUtility.ParseVersionSpec(versionSpec),null);
            var prc = new PackageResolutionManager(new Mock<ILogger>().Object, latest, new MemoryBasedPackageCache(new Mock<ILogger>().Object));
            var v = prc.ResolveInstallableVersion(Utilities.GetFactory().CreateRepository("SingleAggregate"), pr);
            return v != null ? v.ToString() : null;
        }

        protected override void SetUpTest()
        {
            
        }
    }
}
