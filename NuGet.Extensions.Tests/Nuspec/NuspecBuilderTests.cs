using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Moq;
using NuGet.Common;
using NuGet.Extensions.Nuspec;
using NuGet.Extensions.Tests.MSBuild;
using NUnit.Framework;

namespace NuGet.Extensions.Tests.Nuspec
{
    [TestFixture]
    public class NuspecBuilderTests
    {
        [Test]
        public void AssemblyNameReplacesNullDescription()
        {
            var console = new ConsoleMock();
            const string anyAssemblyName = "any.assembly.name";
            var nullDataSource = new Mock<INuspecDataSource>().Object;

            var nuspecBuilder = new NuspecBuilder(anyAssemblyName);
            nuspecBuilder.SetMetadata(nullDataSource, new List<ManifestDependency>());
            nuspecBuilder.Save(console.Object);

            var nuspecContents = File.ReadAllText(nuspecBuilder.FilePath);
            Assert.That(nuspecContents, Contains.Substring("<description>" + anyAssemblyName + "</description>"));
            console.AssertConsoleHasNoErrorsOrWarnings();
        }

        [TestCase("net40")]
        [TestCase("net35")]
        [TestCase("notARealTargetFramework", Description = "Characterisation: There is no validation on the target framework")]
        public void TargetFrameworkAppearsVerbatimInOutput(string targetFramework)
        {
            var console = new ConsoleMock();

            var nuspecBuilder = new NuspecBuilder("anyAssemblyName");
            var anyDependencies = new List<ManifestDependency>{new ManifestDependency {Id="anyDependency", Version = "0.0.0.0"}};
            nuspecBuilder.SetDependencies(anyDependencies, targetFramework);
            nuspecBuilder.Save(console.Object);

            var nuspecContents = File.ReadAllText(nuspecBuilder.FilePath);
            var expectedAssemblyGroupStartTag = string.Format("<group targetFramework=\"{0}\">", targetFramework);
            Assert.That(nuspecContents, Contains.Substring(expectedAssemblyGroupStartTag));
            console.AssertConsoleHasNoErrorsOrWarnings();
        }
    }
}
