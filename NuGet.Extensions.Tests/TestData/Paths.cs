using System;
using System.IO;

namespace NuGet.Extensions.Tests.TestData
{
    public static class Paths
    {
        public static string RootFolder {get { return Path.Combine(".", "TestData"); }}
        public static string PackageReferenceSetResolverXml { get { return Path.Combine(RootFolder, "PackageReferenceSetResolverTestData.xml"); } }
        public static string AdapterTestsSolutionFile { get { return Path.Combine(RootFolder, "TestSolutionForAdapter", "TestSolutionForAdapter.sln"); } }
    }
}
