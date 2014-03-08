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
        private IEnumerable<IReference> _projectBinaryReferenceAdapters;
        
        private const string _expectedBinaryDependencyAssemblyName = "Newtonsoft.Json";
        private const string _expectedBinaryDependencyVersion = "6.0.0.0";
        private const string _expectedProjectDependencyName = "ProjectDependency";

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
        public void ProjectWithDependenciesProjectyNameIsProjectWithDependencies()
        {
            Assert.That(_projectWithDependenciesAdapter.ProjectName, Is.EqualTo("ProjectWithDependencies"));
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
            binaryDependency.ConvertToNugetReferenceWithHintPath(nonPersistedHintPath);
            string hintpath;
            //Don't constrain the implementation to modifying the existing reference
            var newBinaryDepedency = _projectWithDependenciesAdapter.GetBinaryReferences().Single(IsExpectedBinaryDependency);
            var hasHintPath = newBinaryDepedency.TryGetHintPath(out hintpath);

            Assert.That(hasHintPath, Is.True);
            Assert.That(hintpath, Is.EqualTo(nonPersistedHintPath));
        }

        [Test]
        public void BinaryReferenceForExpectedAssembly()
        {
            var binaryDependency = _projectBinaryReferenceAdapters.Single(IsExpectedBinaryDependency);

            var isForCorrespondingAssembly = binaryDependency.IsForAssembly(_expectedBinaryDependencyAssemblyName + ".dll");

            Assert.That(isForCorrespondingAssembly, Is.True);
        }

        [Test]
        public void BinaryReferenceNotForEmptyAssemblyName()
        {
            var binaryDependency = _projectBinaryReferenceAdapters.Single(IsExpectedBinaryDependency);

            var isForBlankAssemblyName = binaryDependency.IsForAssembly("") || binaryDependency.IsForAssembly(".dll");

            Assert.That(isForBlankAssemblyName, Is.False);
        }

        [Test]
        public void GetProjectReferencesHasAssemblyNameInIncludeName()
        {
            var projReferences = _projectWithDependenciesAdapter.GetProjectReferences().ToList();

            Assert.That(projReferences.Count(), Is.EqualTo(1));
            Assert.That(projReferences.Single().IncludeName, Contains.Substring(_expectedProjectDependencyName));
        }


        private static bool IsExpectedBinaryDependency(IReference r)
        {
            return r.IncludeName == _expectedBinaryDependencyAssemblyName;
        }

        private static ProjectAdapter CreateProjectAdapter(string projectWithDependencies)
        {
            return new ProjectAdapter(projectWithDependencies);
        }
    }
}
