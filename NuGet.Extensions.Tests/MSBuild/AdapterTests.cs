using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Moq;
using NuGet.Common;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.Tests.TestData;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace NuGet.Extensions.Tests.MSBuild
{
    [TestFixture]
    public class AdapterTests
    {
        private IVsProject _projectWithDependenciesAdapter;
        private IEnumerable<IReference> _projectBinaryReferenceAdapters;
        private SolutionProjectLoader _solutionProjectLoader;
        private Mock<IConsole> _console;
        private const string _projectWithDependenciesName = "ProjectWithDependencies";
        private const string _expectedBinaryDependencyAssemblyName = "Newtonsoft.Json";
        private const string _expectedBinaryDependencyVersion = "6.0.0.0";
        private const string _expectedProjectDependencyName = "ProjectDependency";

        [SetUp]
        public void SetUpProjectAdapterAndBinaryDependencies()
        {
            _console = new Mock<IConsole>();
            _solutionProjectLoader = new SolutionProjectLoader(new FileInfo(Paths.AdapterTestsSolutionFile), _console.Object);
            var projectAdapters = _solutionProjectLoader.GetProjects();
            _projectWithDependenciesAdapter = projectAdapters.Single(p => p.ProjectName == _projectWithDependenciesName);
            _projectBinaryReferenceAdapters = _projectWithDependenciesAdapter.GetBinaryReferences();
        }

        [TearDown]
        public void CheckForConsoleErrors()
        {
            _console.Verify(c => c.WriteError(It.IsAny<object>()), Times.Never());
            _console.Verify(c => c.WriteError(It.IsAny<string>()), Times.Never());
            _console.Verify(c => c.WriteError(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never());
            _console.Verify(c => c.WriteWarning(It.IsAny<string>()), Times.Never());
            _console.Verify(c => c.WriteWarning(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never());
            _console.Verify(c => c.WriteWarning(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never());
        }

        [Test]
        public void ProjectWithDependenciesAssemblyNameIsProjectWithDependencies()
        {
            Assert.That(_projectWithDependenciesAdapter.AssemblyName, Is.EqualTo(_projectWithDependenciesName));
        }

        [Test]
        public void ProjectWithDependenciesDependsOnNewtonsoftJson()
        {
            var binaryReferenceIncludeNames = _projectBinaryReferenceAdapters.Select(r => r.AssemblyName).ToList();
            Assert.That(binaryReferenceIncludeNames, Contains.Item(_expectedBinaryDependencyAssemblyName));
        }

        [Test]
        public void ProjectWithDependenciesDependsOnNewtonsoftJson6000()
        {
            var binaryDependency = _projectBinaryReferenceAdapters.Single(IsExpectedBinaryDependency);
            Assert.That(binaryDependency.AssemblyVersion, Is.EqualTo(_expectedBinaryDependencyVersion));
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
            Assert.That(projReferences.Single().AssemblyName, Contains.Substring(_expectedProjectDependencyName));
        }

        private static bool IsExpectedBinaryDependency(IReference r)
        {
            return r.AssemblyName == _expectedBinaryDependencyAssemblyName;
        }
    }
}
