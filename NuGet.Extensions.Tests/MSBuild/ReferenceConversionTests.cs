using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Moq;
using NuGet.Common;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.Tests.TestData;
using NUnit.Framework;

namespace NuGet.Extensions.Tests.MSBuild
{
    [TestFixture]
    public class ReferenceConversionTests
    {
        private DirectoryInfo _solutionDir;
        private FileInfo _solutionFile;
        private ConsoleMock _console;
        private const string ProjectWithDependenciesName = "ProjectWithDependencies";
        private FileInfo _projectWithDependenciesFile;

        [SetUp]
        public void SetupIsolatedSolutionAndUnrelatedPackages()
        {
            _solutionDir = Isolation.GetIsolatedTestSolutionDir();
            _solutionFile = new FileInfo(Path.Combine(_solutionDir.FullName, Paths.AdapterTestsSolutionFile.Name));
            _projectWithDependenciesFile = new FileInfo(Path.Combine(_solutionDir.FullName, Paths.ProjectWithDependenciesRelativeToSolutionDir));
            _console = new ConsoleMock();
        }

        [TearDown]
        public void DeleteIsolatedSolutionAndPackagesFolder()
        {
            _solutionDir.Delete(true);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void ConvertingProjectReferenceMutlipleTimesAddsOneBinaryReference(int timesToConvert)
        {
            var projectWithDependencies = LoadProjectWithDependenciesFromDisk();
            var projectReference = projectWithDependencies.GetProjectReferences().First();
            var anEasilyRecognisableString = "an easily recognisable string";

            for (int i = 0; i < timesToConvert; i++) projectReference.ConvertToNugetReferenceWithHintPath(anEasilyRecognisableString);
            projectWithDependencies.Save();

            var reloadedProject = LoadProjectWithDependenciesFromDisk();
            var addedReferences = reloadedProject.GetBinaryReferences().Where(s => HintPathMatches(s, anEasilyRecognisableString));
            Assert.That(addedReferences.Count(), Is.EqualTo(1));
        }

        [Test]
        public void ConvertingProjectReferenceRemovesProjectReference()
        {
            var projectWithDependencies = LoadProjectWithDependenciesFromDisk();
            var projectReference = projectWithDependencies.GetProjectReferences().First();
            var assemblyNameForProject = projectReference.AssemblyName;
            var anEasilyRecognisableString = "an easily recognisable string";

            projectReference.ConvertToNugetReferenceWithHintPath(anEasilyRecognisableString);
            projectWithDependencies.Save();

            var reloadedProject = LoadProjectWithDependenciesFromDisk();
            var referencesMatchingRemovedProject = reloadedProject.GetProjectReferences().Where(p => p.AssemblyName == assemblyNameForProject);
            Assert.That(referencesMatchingRemovedProject.Count(), Is.EqualTo(0));
        }

        private static bool HintPathMatches(IReference s, string anEasilyRecognisableString)
        {
            string hintPath;
            return s.TryGetHintPath(out hintPath) && hintPath == anEasilyRecognisableString;
        }

        private IVsProject LoadProjectWithDependenciesFromDisk()
        {
            var newProjectLoader = new CachingProjectLoader(new Dictionary<string, string>(), _console.Object);
            return newProjectLoader.GetProject(Guid.Empty, _projectWithDependenciesFile.FullName);
        }
    }
}
