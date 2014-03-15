using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Moq;
using NuGet.Common;
using NuGet.Extensions.Nuspec;
using NUnit.Framework;

namespace NuGet.Extensions.Tests.Nuspec
{
    [TestFixture]
    public class NuspecBuilderTests
    {
        [Test]
        public void AssemblyNameReplacesNullDescription()
        {
            var console = new Mock<IConsole>();
            const string anyAssemblyName = "any.assembly.name";
            var nullDataSource = new Mock<INuspecDataSource>().Object;

            var nuspecBuilder = new NuspecBuilder(anyAssemblyName);
            nuspecBuilder.SetMetadata(nullDataSource, new List<ManifestDependency>());
            nuspecBuilder.Save(console.Object);

            var nuspecContents = File.ReadAllText(nuspecBuilder.FilePath);
            Assert.That(nuspecContents, Contains.Substring("<description>" + anyAssemblyName + "</description>"));
        }
    }
}
