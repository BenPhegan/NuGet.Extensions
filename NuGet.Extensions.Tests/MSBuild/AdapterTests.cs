using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Moq;
using NuGet.Common;
using NuGet.Extensions.ExtensionMethods;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.Tests.ReferenceAnalysers;
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
        private CachingSolutionLoader _solutionProjectLoader;
        private Mock<IConsole> _console;
        private const string ProjectWithDependenciesName = "ProjectWithDependencies";
        private const string ExpectedBinaryDependencyAssemblyName = "Newtonsoft.Json";
        private const string ExpectedBinaryDependencyVersion = "6.0.0.0";
        private const string ExpectedProjectDependencyName = "ProjectDependency";

        [SetUp]
        public void SetUpProjectAdapterAndBinaryDependencies()
        {
            _console = new Mock<IConsole>();
            _solutionProjectLoader = new CachingSolutionLoader(Paths.AdapterTestsSolutionFile, new Dictionary<string, string>(), _console.Object);
            var projectAdapters = _solutionProjectLoader.GetProjects();
            _projectWithDependenciesAdapter = projectAdapters.Single(p => p.ProjectName.Equals(ProjectWithDependenciesName, StringComparison.OrdinalIgnoreCase));
            _projectBinaryReferenceAdapters = _projectWithDependenciesAdapter.GetBinaryReferences();
        }

        [TearDown]
        public void CheckForConsoleErrors()
        {
            ConsoleMock.AssertConsoleHasNoErrorsOrWarnings(_console);
        }

        [Test]
        public void ProjectWithDependenciesAssemblyNameIsProjectWithDependencies()
        {
            Assert.That(_projectWithDependenciesAdapter.AssemblyName, Is.EqualTo(ProjectWithDependenciesName));
        }

        [Test]
        public void ProjectWithDependenciesDependsOnNewtonsoftJson()
        {
            var binaryReferenceIncludeNames = _projectBinaryReferenceAdapters.Select(r => r.AssemblyName).ToList();
            Assert.That(binaryReferenceIncludeNames, Contains.Item(ExpectedBinaryDependencyAssemblyName));
        }

        [Test]
        public void ProjectWithDependenciesDependsOnNewtonsoftJson6000()
        {
            var binaryDependency = _projectBinaryReferenceAdapters.Single(IsExpectedBinaryDependency);
            Assert.That(binaryDependency.AssemblyVersion, Is.EqualTo(ExpectedBinaryDependencyVersion));
        }

        [Test]
        public void ProjectWithDependenciesDependsOnCorrectReferencesForEmptyCondition()
        {
            var emptyConfigurationDependencies = GetReferencesForProjectWithDependencies(new Dictionary<string, string>()).ToList();
            var conditionTrueReferences = emptyConfigurationDependencies.Where(r => r.Condition).Select(r => r.AssemblyName).ToArray();
            var conditionFalseReferences = emptyConfigurationDependencies.Where(r => !r.Condition).Select(r => r.AssemblyName).ToArray();
            Assert.That(conditionTrueReferences, Contains.Item("AssemblyReferencedWhenConfigurationNotEqualsRelease"));
        }

        [Test]
        public void ProjectWithDependenciesDependsOnCorrectReferencesForSetCondition()
        {
            var emptyConfigurationDependencies = GetReferencesForProjectWithDependencies(new Dictionary<string, string> {{"Configuration", "Release"}}).ToList();
            var conditionTrueReferences = emptyConfigurationDependencies.Where(r => r.Condition).Select(r => r.AssemblyName).ToArray();
            var conditionFalseReferences = emptyConfigurationDependencies.Where(r => !r.Condition).Select(r => r.AssemblyName).ToArray();
            Assert.That(conditionTrueReferences, Contains.Item("AssemblyReferencedWhenConfigurationEqualsRelease"));
        }
        
        public IEnumerable<IReference> GetReferencesForProjectWithDependencies(IDictionary<string, string> globalMsBuildProperties)
        {
            var loader = new CachingSolutionLoader(Paths.AdapterTestsSolutionFile, globalMsBuildProperties, _console.Object);
            var projectAdapters = loader.GetProjects();
            var projectWithDependenciesAdapter = projectAdapters.Single(p => p.ProjectName.Equals(ProjectWithDependenciesName, StringComparison.OrdinalIgnoreCase));
            return projectWithDependenciesAdapter.GetBinaryReferences();
        }

        [Test]
        public void BinaryReferenceHasHintPathContainingAssemblyNameAndVersion()
        {
            var binaryDependency = _projectBinaryReferenceAdapters.Single(IsExpectedBinaryDependency);
            string hintpath;
            var hasHintPath = binaryDependency.TryGetHintPath(out hintpath);

            Assert.That(hasHintPath, Is.True);
            const string expectedHintPathStart = "..\\packages\\" + ExpectedBinaryDependencyAssemblyName;
            const string expectedHintPathEnd = ExpectedBinaryDependencyAssemblyName + ".dll";
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

            var isForCorrespondingAssembly = binaryDependency.IsForAssembly(ExpectedBinaryDependencyAssemblyName + ".dll");

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
            Assert.That(projReferences.Single().AssemblyName, Contains.Substring(ExpectedProjectDependencyName));
        }

        [Test]
        public void PassingWrongGuidGetsProjectByPath()
        {
            var anyProject = _solutionProjectLoader.GetProjects().First();
            var projectLoader = new CachingProjectLoader(new Dictionary<string, string>(), _console.Object);
            var loadedByPathOnly = projectLoader.GetProject(Guid.Empty, Path.Combine(anyProject.ProjectDirectory.FullName, anyProject.ProjectName + ".csproj"));
            Assert.That(anyProject.ProjectName, Is.EqualTo(loadedByPathOnly.ProjectName));
        }

        [Test]
        public void ProjectPropertiesAreCorrect()
        {
            var projectDependency = _solutionProjectLoader.GetProjects().Single(p => p.ProjectName.Equals(ExpectedProjectDependencyName, StringComparison.OrdinalIgnoreCase));

            Assert.That(string.Equals(projectDependency.AssemblyName, projectDependency.ProjectName, StringComparison.OrdinalIgnoreCase), Is.True);
            var projectsInProjectDir = projectDependency.ProjectDirectory.GetFiles(ExpectedProjectDependencyName + ".csproj");
            Assert.That(projectsInProjectDir.Count(), Is.EqualTo(1));
            Assert.That(projectDependency.GetProjectReferences().Count(), Is.EqualTo(0));
            Assert.That(projectDependency.GetBinaryReferences().Count(), Is.EqualTo(7));
        }

        [Test(Description = "The same IVsProject must be returned so we don't end up with two out-of-sync views of the project")]
        public void PassingWrongGuidAndNonCanonicalPathGetsSameReference()
        {
            var projectLoader = new CachingProjectLoader(new Dictionary<string, string>(), _console.Object);
            var loadedWithCorrectGuid = projectLoader.GetProject(ProjectReferenceTestData.ProjectWithDependenciesGuid, Paths.ProjectWithDependencies);
            var nonCanonicalPath = Path.Combine(loadedWithCorrectGuid.ProjectDirectory.FullName.ToLower(), "randomFolder", "..", loadedWithCorrectGuid.ProjectName.ToUpper() + ".csproj");

            var loadedByNonCanonPath = projectLoader.GetProject(Guid.Empty, nonCanonicalPath);

            Assert.That(ReferenceEquals(loadedWithCorrectGuid, loadedByNonCanonPath), Is.True);
        }

        private static bool IsExpectedBinaryDependency(IReference r)
        {
            return r.AssemblyName.Equals(ExpectedBinaryDependencyAssemblyName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
