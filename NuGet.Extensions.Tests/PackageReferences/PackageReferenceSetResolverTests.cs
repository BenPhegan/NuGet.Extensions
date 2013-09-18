using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Xml.Linq;
using NUnit.Framework;
using NuGet.Extensions.PackageReferences;
using NuGet.Extensions.Tests.TestData;
using System.IO;

namespace NuGet.Extensions.Tests.PackageReferences
{
    [TestFixture]
    public class PackageReferenceSetResolverTests
    {
        [TestCase("(1.1,1.2)", "(1.3,1.4]", null)]
        [TestCase("(1.2,2.3)", "(1.2,2.3]", "(1.2,2.3)")]
        [TestCase("(1.2,2.3)", "[1.2,2.3]", "(1.2,2.3)")]
        [TestCase("(1.7,2.1)", "[1.2,2.3]", "(1.7,2.1)")]
        [TestCase("(1.7,2.9)", "[1.2,2.3]", "(1.7,2.3]")]
        [TestCase("[1.1,2.1)", "(1.2,2.3]", "(1.2,2.1)")]
        [TestCase("[1.1,1.1]", "(1.1,2.3]", null)]
        [TestCase("[2.3,2.3]", "(1.1,2.3)", null)]
        [TestCase("[1.1,1.1]", "[1.1,2.3]", "[1.1,1.1]")]
        [TestCase("[2.3,2.3]", "(1.1,2.3]", "[2.3,2.3]")]
        public void CanResolveMinimalVersionSpec(string v1, string v2, string expected)
        {
            var t = new PackageReferenceSetResolver();
            var l = new List<PackageReference>();
            l.Add(new PackageReference("test", SemanticVersion.Parse("1.1.1.1"), VersionUtility.ParseVersionSpec(v1), new FrameworkName(".NET Framework, Version=4.0"), false));
            l.Add(new PackageReference("test", SemanticVersion.Parse("1.1.1.1"), VersionUtility.ParseVersionSpec(v2), new FrameworkName(".NET Framework, Version=4.0"), false));
            var resolvedVs = t.ResolveValidVersionSpec(l);
            if (expected == null)
            {
                Assert.IsNull(resolvedVs);
            }
            else
            {
                var expectedVs = VersionUtility.ParseVersionSpec(expected);
                var iSpec = resolvedVs.VersionConstraint as IVersionSpec;

                //TODO Equality comparer, anyone?  This is pretty crappy....
                Assert.AreEqual(expectedVs.IsMaxInclusive, iSpec.IsMaxInclusive);
                Assert.AreEqual(expectedVs.IsMinInclusive, iSpec.IsMinInclusive);
                Assert.AreEqual(expectedVs.MaxVersion, iSpec.MaxVersion);
                Assert.AreEqual(expectedVs.MinVersion, iSpec.MinVersion);
            }
        }

        [TestCase("(1.1, 1.3)", "(1.2,1.3)", "(1.3,1.4)", null)]
        [TestCase("(1.1, 1.3]", "(1.2,1.3)", "(1.2,1.4)", "(1.2,1.3)")]
        [TestCase("(1.1, 1.3]", "(1.2,1.3]", "[1.3,1.4)", "[1.3,1.3]")]
        [TestCase("(1.1, 1.3]", "[1.3,1.4)", "(1.2.9,1.3)", null)]
        [TestCase("(1.1, 1.5.1)", "(1.4,1.5]", "(1.3.9,1.4.1]", "(1.4,1.4.1]")]
        public void CanResolveMinimalVersionSpec(string v1, string v2, string v3, string expected)
        {
            var t = new PackageReferenceSetResolver();
            var l = new List<PackageReference>();
            l.Add(new PackageReference("test", SemanticVersion.Parse("1.1.1.1"), VersionUtility.ParseVersionSpec(v1), new FrameworkName(".NET Framework, Version=4.0"), false));
            l.Add(new PackageReference("test", SemanticVersion.Parse("1.1.1.1"), VersionUtility.ParseVersionSpec(v2), new FrameworkName(".NET Framework, Version=4.0"), false));
            l.Add(new PackageReference("test", SemanticVersion.Parse("1.1.1.1"), VersionUtility.ParseVersionSpec(v3), new FrameworkName(".NET Framework, Version=4.0"), false));
            var resolvedVs = t.ResolveValidVersionSpec(l);
            if (expected == null)
            {
                Assert.IsNull(resolvedVs);
            }
            else
            {
                var expectedVs = VersionUtility.ParseVersionSpec(expected);
                var iSpec = resolvedVs.VersionConstraint as IVersionSpec;

                //TODO Equality comparer, anyone?  This is pretty crappy....
                Assert.AreEqual(expectedVs.IsMaxInclusive, iSpec.IsMaxInclusive);
                Assert.AreEqual(expectedVs.IsMinInclusive, iSpec.IsMinInclusive);
                Assert.AreEqual(expectedVs.MaxVersion, iSpec.MaxVersion);
                Assert.AreEqual(expectedVs.MinVersion, iSpec.MinVersion);
            }
        }

        [TestCase("1.1", "1.1", "1.1")]
        [TestCase("1.1", "1.2", null)]
        public void CanResolveMinimalVersion(string v1, string v2, string expected)
        {
            var t = new PackageReferenceSetResolver();
            var l = new List<PackageReference>();
            l.Add(new PackageReference("test", SemanticVersion.Parse(v1), new VersionSpec(), new FrameworkName(".NET Framework, Version=4.0"), false));
            l.Add(new PackageReference("test", SemanticVersion.Parse(v2), new VersionSpec(), new FrameworkName(".NET Framework, Version=4.0"), false));

            if (expected == null)
                Assert.IsNull(t.ResolveValidVersion(l));
            else
                Assert.AreEqual(SemanticVersion.Parse(expected), t.ResolveValidVersion(l).Version);

        }


        //TODO Would be nice to pass the description through here....still need a lot more tests....
        [TestCase("1")]
        [TestCase("2")]
        [TestCase("3")]
        [TestCase("4")]
        [TestCase("5")]
        [TestCase("6")]
        [TestCase("7")]
        [TestCase("8")]
        public void CanResolvePackageList(string testname)
        {
            var testData = GetTestObjectFromDataFile(testname);

            var t = new PackageReferenceSetResolver();
            var r = t.Resolve(testData.Input);

            //Check output
            foreach (var element in testData.Output)
            {
                Assert.IsTrue(r.Item1.Contains(element));
            }

            //Check failures
            foreach (var element in testData.Error)
            {
                Assert.IsTrue(r.Item2.Contains(element));
            }

        }

        private PackageResolverTestObject GetTestObjectFromDataFile(string sample)
        {
            XDocument xDoc = XDocument.Load(Path.Combine(".","TestData","PackageReferenceSetResolverTestData.xml"));
            var testData = xDoc.Elements("Tests").Elements("Test").Where(x => x.Attribute("name").Value == sample).First();
            if (testData != null)
            {
                return new PackageResolverTestObject(testData);
            }

            return null;
        }
    }
}
