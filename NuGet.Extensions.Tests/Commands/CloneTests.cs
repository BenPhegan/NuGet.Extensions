using System.Linq;
using NUnit.Framework;
using NuGet.Extensions.Tests.TestObjects;

namespace NuGet.Extensions.Tests.Commands
{
    public class CloneTests : CloneCommandTestsBase
    {
        protected override void SetUpTest()
        {
        }

        [TestCase(@"Dev", @"Release", @"", 10)]
        [TestCase(@"Release", @"Dev", @"", 10)]
        public void CloneAllLatest(string source, string destination, string packageId, int expectedCount)
        {
            CloneCommand.Sources.Add(source);
            CloneCommand.Destinations.Add(destination);
            CloneCommand.Arguments.Add(packageId);
            CloneCommand.AllVersions = false;
            var destinationProvider = Utilities.GetSourceProvider(destination);
            CloneCommand.ExecuteCommand();
            var packageCount = CloneCommand.GetPackageList(true, "", destinationProvider).Count();
            Assert.AreEqual(expectedCount, packageCount);
        }
    }
}
