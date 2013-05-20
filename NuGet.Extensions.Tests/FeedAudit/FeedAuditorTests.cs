using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using NuGet.Extensions.FeedAudit;
using NuGet.Extensions.Tests.Mocks;

namespace NuGet.Extensions.Tests.FeedAudit
{
    [TestFixture]
    public class FeedAuditorTests
    {
        [TestCase("Test1;Test2", null, Result = 2)]
        [TestCase("Test1", null, Result = 1)]
        [TestCase(null, "Test*", Result = 2)]
        [TestCase(null, "Test?", Result = 2)]
        [TestCase(null, "*1;*2", Result = 2)]
        [TestCase(null, "*1", Result = 1)]
        [TestCase("Test1", "*1", Result = 1)]
        [TestCase("Test1", "*2", Result = 2)]
        public int ExcludePackageTest(string exceptions, string wildcards)
        {
            var mockRepo = CreateMockRepository();
            var stringMatches = !string.IsNullOrEmpty(exceptions) ? exceptions.Split(';').ToList() : new List<string>();
            var regii = !string.IsNullOrEmpty(wildcards) ? wildcards.Split(';').Select(w => new Wildcard(w.ToLowerInvariant())).ToList() : new List<Wildcard>();

            var auditor = new FeedAuditor(mockRepo, stringMatches, regii, true, false, false, new List<String>(), new List<Regex>());
            var ignoredCount = 0;
            auditor.PackageIgnored += (o, e) => ignoredCount++;
            auditor.Audit();
            return ignoredCount;
        }

        [TestCase("Assembly11.dll", null, Result = 3)]
        [Ignore]
        public int ExcludeAssemblyTest(string exceptions, string wildcards)
        {
            var mockRepo = CreateMockRepository();
            var stringMatches = !string.IsNullOrEmpty(exceptions) ? exceptions.Split(';').ToList() : new List<string>();
            var regii = !string.IsNullOrEmpty(wildcards) ? wildcards.Split(';').Select(w => new Wildcard(w.ToLowerInvariant())).ToList() : new List<Wildcard>();

            var auditor = new FeedAuditor(mockRepo, new List<String>(), new List<Regex>(), true, false, false, stringMatches, regii);
            var results = auditor.Audit();
            return results.SelectMany(r => r.UnresolvableReferences).Count();
        }

        private static MockPackageRepository CreateMockRepository()
        {
            var mockRepo = new MockPackageRepository();
            mockRepo.AddPackage(PackageUtility.CreatePackage("Test1", isLatest: true, assemblyReferences: new List<string> { "Assembly11.dll", "Assembly12.dll" }, dependencies: new List<PackageDependency> { new PackageDependency("Test2") }));
            mockRepo.AddPackage(PackageUtility.CreatePackage("Test2", isLatest: true, assemblyReferences: new List<string> { "Assembly21.dll", "Assembly22.dll" }, dependencies: new List<PackageDependency> { new PackageDependency("Test1") }));
            return mockRepo;
        }
    }
}
