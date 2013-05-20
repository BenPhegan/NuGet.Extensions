using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NuGet.Extensions.Tests.Mocks;
using NuGet.Extras.Packages;
using NuGet.Extras.Tests.TestObjects;
using NuGet.Extras.Comparers;
using System.IO;

namespace NuGet.Extras.Tests.Packages
{
    [TestFixture]
    public class PackageEnumeratorTests
    {

        [Test]
        public void TestStream()
        {
            Stream stream1 = "test".AsStream();
            IFileSystem fs = new MockFileSystem();
            fs.AddFile(@"c:\test.config",stream1);
            //Stream stream2 = fs.OpenFile();

        }

        [Test]
        public void CanEnumerateOverSet()
        {
            IFileSystem fs = new MockFileSystem();
            var test1 = @"c:\test1\packages.config";
            var test2 = @"c:\test1\packages.config";

            var p1 = @"<packages>
                      <package id='Test' version='1.0.0.0' />
                      <package id='Test' version='1.1.0.0' />
                      <package id='Test' version='1.2.0.07' />
                    </packages>";

            var p2 = @"<packages>
                      <package id='Test' version='1.0.0.0' />
                      <package id='Test' version='1.1.0.0' />
                      <package id='Test' version='1.2.0.07' />
                    </packages>";

            fs.AddFile(test1, p1.AsStream());
            fs.AddFile(test2, p2.AsStream());

            var list = new List<PackageReferenceFile>();

            list.Add(new PackageReferenceFile(fs, test1));
            list.Add(new PackageReferenceFile(fs, test2));


            PackageEnumerator enumerator = new PackageEnumerator();
            var idOnly = enumerator.GetPackageReferences(list, (a, b) => { }, PackageReferenceEqualityComparer.Id);
            Assert.AreEqual(1, idOnly.Count());

            var idAndVersion = enumerator.GetPackageReferences(list, (a, b) => { }, PackageReferenceEqualityComparer.IdAndVersion);
            Assert.AreEqual(3, idAndVersion.Count());


        }

        private static void CreatePackageConfigs(IFileSystem fs, out PackageReferenceFile f1, out PackageReferenceFile f2)
        {
            f1 = (new PackageReferenceFile(fs, @"c:\test1\packages.config"));
            f1.AddEntry("Test", SemanticVersion.Parse("1.0.0.0"));
            f1.AddEntry("Test", SemanticVersion.Parse("1.1.0.0"));
            f1.AddEntry("Test", SemanticVersion.Parse("1.2.0.0"));

            f2 = (new PackageReferenceFile(fs, @"c:\test2\packages.config"));
            f2.AddEntry("Test", SemanticVersion.Parse("1.0.0.0"));
            f2.AddEntry("Test", SemanticVersion.Parse("1.1.0.0"));
            f2.AddEntry("Test", SemanticVersion.Parse("1.2.0.0"));
        }

    }
}
