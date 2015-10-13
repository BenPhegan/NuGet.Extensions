using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Moq;
using NuGet.Extensions.ReferenceAnalysers;
using NuGet.Extensions.Tests.Mocks;
using NUnit.Framework;

namespace NuGet.Extensions.Tests.ReferenceAnalysers
{
    [TestFixture]
    public class HintPathGeneratorTests
    {
        [Test]
        public void HintPathPointsToSolutionLevelPackagesDirectory()
        {
            var hpg = new HintPathGenerator();
            var solutionDirectory = "c:\\solutionDirectoryWithoutTrailingSlash";
            var projectDirName = "projectDirectoryWithoutTrailingSlash";
            var solutionDirInfo = new DirectoryInfo(solutionDirectory);
            var projectDirInfo = new DirectoryInfo(solutionDirectory + string.Format("\\{0}", projectDirName));
            var package = PackageUtility.CreatePackage("any", "0.0.0", assemblyReferences: new []{"any.dll"});

            var hintpath = hpg.ForAssembly(solutionDirInfo, projectDirInfo, package, "any.dll");

            Assert.That(hintpath, Is.EqualTo("..\\packages\\any.0.0.0\\any.dll"));
        }

        [Test]
        public void HintPathGeneratedWithExcludeVersionDoesNotContainVersion()
        {
            var hpg = new HintPathGenerator(false);
            var solutionDirectory = "c:\\solutionDirectoryWithoutTrailingSlash";
            var projectDirName = "projectDirectoryWithoutTrailingSlash";
            var solutionDirInfo = new DirectoryInfo(solutionDirectory);
            var projectDirInfo = new DirectoryInfo(solutionDirectory + string.Format("\\{0}", projectDirName));
            var package = PackageUtility.CreatePackage("any", "0.0.0", assemblyReferences: new[] { "any.dll" });

            var hintpath = hpg.ForAssembly(solutionDirInfo, projectDirInfo, package, "any.dll");

            Assert.That(hintpath, Is.EqualTo("..\\packages\\any\\any.dll"));
        }
    }
}
