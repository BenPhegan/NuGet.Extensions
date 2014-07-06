using System;
using System.Collections.Generic;
using System.IO;

namespace NuGet.Extensions.Tests.TestData
{
    public static class Paths
    {
        public static string RootFolder {get { return Path.Combine(".", "TestData"); }}
        public static string PackageReferenceSetResolverXml { get { return Path.Combine(RootFolder, "PackageReferenceSetResolverTestData.xml"); } }

        public static string TestSolutionForAdapterFolder { get { return Path.Combine(RootFolder, "TestSolutionForAdapter"); } }
        public static FileInfo AdapterTestsSolutionFile { get { return new FileInfo(Path.Combine(TestSolutionForAdapterFolder, "TestSolutionForAdapter.sln")); } }
        public static string ProjectWithDependencies { get { return Path.Combine(TestSolutionForAdapterFolder, ProjectWithDependenciesRelativeToSolutionDir); } }
        public static string ProjectWithDependenciesRelativeToSolutionDir{ get { return Path.Combine("ProjectWithDependencies", "ProjectWithDependencies.csproj"); } }
    }
}
