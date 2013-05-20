using System;
using NUnit.Framework;
using NuGet.Extensions.Comparers;

namespace NuGet.Extensions.Tests.Comparers
{
    [TestFixture]
    public class VersionSpecEqualityComparerTests
    {
        [TestCase("(1.2,2.3)", "(1.2,2.3)", true)]
        [TestCase("(1.2,2.3)", "(1.2,2.4)", false)]
        [TestCase("(1.2,2.3)", "", false)]
        [TestCase("(1.2,2.3)", null, false)]
        [TestCase("", "(1.2,2.4)", false)]
        [TestCase(null, "(1.2,2.4)", false)]
        public void Test(string vs1, string vs2, bool result)
        {
            IVersionSpec ivs1 = GetVersionSpec(vs1);
            IVersionSpec ivs2 = GetVersionSpec(vs2);

            var vsec = new VersionSpecEqualityComparer(ivs1);

            Assert.IsTrue(vsec.Equals(ivs2) == result);
        }

        private IVersionSpec GetVersionSpec(string versionSpec)
        {
            switch (versionSpec)
            {
                case null :
                    return null;

                case "" :
                    return new VersionSpec();

                default :
                    return VersionUtility.ParseVersionSpec(versionSpec);
            }
        }
    }
}
