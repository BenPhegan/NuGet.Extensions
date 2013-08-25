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

        [TestCase(@"Dev", @"Release", @"", true, 11)]
        [TestCase(@"Release", @"Dev", @"", true, 11)]
        [Ignore]
        public void CloneAllLatest(string source, string destination, string packageId, bool allVersions, int expectedCount)
        {
            CloneCommand.Sources.Add(source);
            CloneCommand.Destinations.Add(destination);
            CloneCommand.Arguments.Add(packageId);
            CloneCommand.AllVersions = allVersions;
            var destinationProvider = Utilities.GetSourceProvider(destination);
            CloneCommand.ExecuteCommand();
            var packageCount = CloneCommand.GetPackageList(true, "", string.Empty, destinationProvider).Count();
            Assert.AreEqual(expectedCount, packageCount);
        }
    }
}
