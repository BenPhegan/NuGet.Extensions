using System;
using System.IO;
using Moq;
using NuGet.Common;
using NuGet.Extensions.Commands;
using NuGet.Extensions.Tests.MSBuild;
using NuGet.Extensions.Tests.TestData;
using NUnit.Framework;

namespace NuGet.Extensions.Tests.Commands
{
    public class NugetifyTests
    {
        private DirectoryInfo _solutionDir;
        private string _solutionFile;
        private DirectoryInfo _packageSource;

        [SetUp]
        public void SetupIsolatedSolutionAndUnrelatedPackages()
        {
            _solutionDir = Isolation.GetIsolatedTestSolutionDir();
            _solutionFile = Path.Combine(_solutionDir.FullName, Paths.AdapterTestsSolutionFile.Name);
            _packageSource = Isolation.GetIsolatedEmptyPackageSource();
        }

        [TearDown]
        public void DeleteIsolatedSolutionAndPackagesFolder()
        {
            _packageSource.Delete(true);
            _solutionDir.Delete(true);
        }

        [Test]
        public void NugetifyThrowsNoErrorsWhenNoPackagesFound()
        {
            var console = new ConsoleMock();

            var nugetify = GetNugetifyCommand(console, _solutionFile, _packageSource);
            nugetify.ExecuteCommand();

            console.AssertConsoleHasNoErrorsOrWarnings();
        }

        private static Nugetify GetNugetifyCommand(ConsoleMock console, string solutionFile, DirectoryInfo packageSource)
        {
            var repositoryFactory = new Mock<IPackageRepositoryFactory>();
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            var nugetify = new NugetifyCommandRunner(repositoryFactory.Object, packageSourceProvider.Object)
                           {
                               Console = console.Object,
                               MsBuildProperties = "Configuration=Releasable,SomeOtherParameter=false",
                               NuSpec = true,
                           };
            nugetify.Arguments.AddRange(new[] {solutionFile, packageSource.FullName});
            return nugetify;
        }
    }

    internal class NugetifyCommandRunner : Nugetify
    {
        public NugetifyCommandRunner(IPackageRepositoryFactory factory, IPackageSourceProvider provider)
        {
            RepositoryFactory = factory;
            SourceProvider = provider;
        }
    }
}
