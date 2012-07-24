using System.Linq;
using NUnit.Framework;
using NuGet.Extensions.Tests.TestObjects;

namespace NuGet.Extensions.Tests.Commands
{
    public class RepositoryConfigExecuteTests : GetCommandTestsBase
    {
        protected override void SetUpTest()
        {
        }

        [TestCase(@"c:\TestSolution", 2)]
        public void InstallSampleRepository(string repository, int expectedCount)
        {
            GetCommand.Arguments.Add(repository);
            GetCommand.Latest = true;
            GetCommand.ExecuteCommand();
            var packageCount = FileSystem.Object.GetDirectories(@"c:\TestSolution\packages").Count();
            Assert.AreEqual(expectedCount, packageCount);
        }

        [TestCase(@"c:\TestSolution\Project1\packages.config", 2)]
        [TestCase(@"c:\TestSolution\Project2\packages.config", 2)]
        public void ExpectedInstallCounts(string packageConfig, int expectedCount)
        {
            GetCommand.Arguments.Add(packageConfig);
            GetCommand.ExecuteCommand();
            var packageCount =  FileSystem.Object.GetDirectories(@"c:\TestSolution\packages").Count();
            Assert.AreEqual(expectedCount, packageCount);
        }

        [TestCase(@"c:\TestSolution\Project1\packages.config", 2)]
        public void ExcludeVersion(string packageConfig, int expectedCount)
        {
            GetCommand.Arguments.Add(packageConfig);
            GetCommand.ExcludeVersion = true;
            GetCommand.ExecuteCommand();
            var packageCount =  FileSystem.Object.GetDirectories(@"c:\TestSolution\packages").Count();
            Assert.AreEqual(expectedCount, packageCount);
        }

        [TestCase(@"c:\", @".\TestSolution\Project1\packages.config", 2)]
        [TestCase(@"c:\TestSolution", @".", 2)]
        [TestCase(@"c:\", @".\TestSolution", 2)]
        public void CanUseRelativePaths(string basePath, string packageConfig, int expectedCount)
        {
            GetCommand.Arguments.Add(packageConfig);
            GetCommand.ExcludeVersion = true;
            GetCommand.Latest = true;
            GetCommand.BaseDirectory = basePath;
            GetCommand.ExecuteCommand();
            var packageCount = FileSystem.Object.GetDirectories(@"c:\TestSolution\packages").Count();
            Assert.AreEqual(expectedCount, packageCount);
        }
    }
}
