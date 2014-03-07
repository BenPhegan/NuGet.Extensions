using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.Tests.TestData;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace NuGet.Extensions.Tests.MSBuild
{
    [TestFixture]
    public class AdapterTests
    {
        private ProjectAdapter _projectWithDependenciesAdapter;
        private IEnumerable<IBinaryReference> _projectBinaryReferenceAdapters;
        
        private const string _expectedBinaryDependencyAssemblyName = "Newtonsoft.Json";
        private const string _expectedBinaryDependencyVersion = "6.0.0.0";

        [SetUp]
        public void SetUpProjectAdapterAndBinaryDependencies()
        {
            _projectWithDependenciesAdapter = CreateProjectAdapter(Paths.ProjectWithDependencies);
            _projectBinaryReferenceAdapters = _projectWithDependenciesAdapter.GetBinaryReferences();
        }

        [Test]
        public void ProjectWithDependenciesAssemblyNameIsProjectWithDependencies()
        {
            Assert.That(_projectWithDependenciesAdapter.AssemblyName, Is.EqualTo("ProjectWithDependencies"));
        }

        [Test]
        public void ProjectWithDependenciesDependsOnNewtonsoftJson()
        {
            var binaryReferenceIncludeNames = _projectBinaryReferenceAdapters.Select(r => r.IncludeName).ToList();
            Assert.That(binaryReferenceIncludeNames, Contains.Item(_expectedBinaryDependencyAssemblyName));
        }

        [Test]
        public void ProjectWithDependenciesDependsOnNewtonsoftJson6000()
        {
            var binaryDependency = _projectBinaryReferenceAdapters.Single(IsExpectedBinaryDependency);
            Assert.That(binaryDependency.IncludeVersion, Is.EqualTo(_expectedBinaryDependencyVersion));
        }

        [Test]
        public void BinaryReferenceHasHintPathContainingAssemblyNameAndVersion()
        {
            var binaryDependency = _projectBinaryReferenceAdapters.Single(IsExpectedBinaryDependency);
            string hintpath;
            var hasHintPath = binaryDependency.TryGetHintPath(out hintpath);

            Assert.That(hasHintPath, Is.True);
            const string expectedHintPathStart = "..\\packages\\" + _expectedBinaryDependencyAssemblyName;
            const string expectedHintPathEnd = _expectedBinaryDependencyAssemblyName + ".dll";
            Assert.That(hintpath, new StartsWithConstraint(expectedHintPathStart));
            Assert.That(hintpath, new EndsWithConstraint(expectedHintPathEnd));
        }

        [Test]
        public void BinaryReferenceSetHintPathCanBeRetrieved()
        {
            var binaryDependency = _projectBinaryReferenceAdapters.Single(IsExpectedBinaryDependency);

            const string nonPersistedHintPath = "a different string that won't be persisted";
            binaryDependency.SetHintPath(nonPersistedHintPath);
            string hintpath;
            var hasHintPath = binaryDependency.TryGetHintPath(out hintpath);

            Assert.That(hasHintPath, Is.True);
            Assert.That(hintpath, Is.EqualTo(nonPersistedHintPath));
        }

        private static bool IsExpectedBinaryDependency(IBinaryReference r)
        {
            return r.IncludeName == _expectedBinaryDependencyAssemblyName;
        }

        private static ProjectAdapter CreateProjectAdapter(string projectWithDependencies)
        {
            var msBuildProject = new Project(projectWithDependencies, null, null, new ProjectCollection());
            var projectAdapter = new ProjectAdapter(msBuildProject, "packages.config");
            return projectAdapter;
        }
    }
}
