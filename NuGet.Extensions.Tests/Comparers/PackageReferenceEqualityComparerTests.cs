using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Extensions.Comparers;
using NUnit.Framework;

namespace NuGet.Extensions.Tests.Comparers
{
    [TestFixture]
    public class PackageReferenceEqualityComparerTests
    {

        [Test, TestCaseSource(typeof(TestDataFactory), "TestCases")]
        public void CheckId(string id1, string v1, string vs1, string id2, string v2, string vs2, int idExpectation, int idVersionExpectation, int idVersionAndAllowedVersionExpectation, int idAndVersionSpecExpectation)
        {
            var packages = CreatePackageReferenceList(id1, v1, vs1, id2, v2, vs2);
            var result = packages.Distinct(PackageReferenceEqualityComparer.Id).ToList();
            Assert.IsTrue(result.Count == idExpectation);
        }

        [Test, TestCaseSource(typeof(TestDataFactory), "TestCases")]
        public void CheckIdAndAllowedVersion(string id1, string v1, string vs1, string id2, string v2, string vs2, int idExpectation, int idVersionExpectation, int idVersionAndAllowedVersionExpectation, int idAndVersionSpecExpectation)
        {
            var packages = CreatePackageReferenceList(id1, v1, vs1, id2, v2, vs2);
            var result = packages.Distinct(PackageReferenceEqualityComparer.IdAndAllowedVersions).ToList();
            Assert.IsTrue(result.Count == idAndVersionSpecExpectation);
        }

        [Test, TestCaseSource(typeof(TestDataFactory), "TestCases")]
        public void CheckIdAndVersion(string id1, string v1, string vs1, string id2, string v2, string vs2, int idExpectation, int idVersionExpectation, int idVersionAndAllowedVersionExpectation, int idAndVersionSpecExpectation)
        {
            var packages = CreatePackageReferenceList(id1, v1, vs1, id2, v2, vs2);
            var result = packages.Distinct(PackageReferenceEqualityComparer.IdAndVersion).ToList();
            Assert.IsTrue(result.Count == idVersionExpectation);
        }

        [Test, TestCaseSource(typeof(TestDataFactory), "TestCases")]
        public void CheckIdVersionAndAllowedVersion(string id1, string v1, string vs1, string id2, string v2, string vs2, int idExpectation, int idVersionExpectation, int idVersionAndAllowedVersionExpectation, int idAndVersionSpecExpectation)
        {
            var packages = CreatePackageReferenceList(id1, v1, vs1, id2, v2, vs2);
            var result = packages.Distinct(PackageReferenceEqualityComparer.IdVersionAndAllowedVersions).ToList();
            Assert.IsTrue(result.Count == idVersionAndAllowedVersionExpectation);
        }

        private static IEnumerable<PackageReference> CreatePackageReferenceList(string id1, string v1, string vs1, string id2, string v2, string vs2)
        {
            var packages = new List<PackageReference>
                               {
                                   new PackageReference(id1, v1 == null ? null : SemanticVersion.Parse(v1), vs1 == null ? null : VersionUtility.ParseVersionSpec(vs1), new FrameworkName(".NET Framework, Version=4.0"), false),
                                   new PackageReference(id2, v2 == null ? null : SemanticVersion.Parse(v2), vs2 == null ? null : VersionUtility.ParseVersionSpec(vs2), new FrameworkName(".NET Framework, Version=4.0"), false)
                               };
            return packages;
        }

    }

    public class TestDataFactory
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData("Common", "1.1.1.1",  null,        "Common",  "1.1.1.1",  null,           1, 1, 1, 1);
                yield return new TestCaseData("Common", "1.1.1.2",  null,        "Common",  "1.1.1.1",  null,           1, 2, 2, 1);
                yield return new TestCaseData("Common", "1.1.1.1",  null,        "Data",    "1.1.1.1",  null,           2, 2, 2, 2);
                yield return new TestCaseData("Common", "1.1.1.1",  "(1.2,2.3)", "Common",  "1.1.1.1",  "(1.2,2.3)",    1, 1, 1, 1);
                yield return new TestCaseData("Common", "1.1.1.1",  "(1.2,2.3)", "Common",  "1.1.1.1",  "(1.2,2.3]",    1, 1, 2, 2);
                yield return new TestCaseData("Common", "1.1.1.2",  "(1.2,2.3)", "Common",  "1.1.1.1",  "(1.2,2.3)",    1, 2, 2, 1);
                yield return new TestCaseData("Common", "1.1.1.2",  "(1.2,2.3)", "Common",  "1.1.1.3",  "(1.2,2.5)",    1, 2, 2, 2);
                //TODO Fix Null Reference Checks.
                //yield return new TestCaseData("Common", null,       null,       "Common",   null,       null,           1, 1, 1, 1).Throws(typeof (NullReferenceException));
                //yield return new TestCaseData("Common", "1.1.1.2",  null,       "Common",   null,       null,           1, 1, 1, 1).Throws(typeof(NullReferenceException));

            }
        }
    }
}
