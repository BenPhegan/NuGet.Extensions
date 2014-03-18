using System;
using System.IO;
using Moq;
using NuGet.Common;
using NuGet.Extensions.Commands;
using NuGet.Extensions.ExtensionMethods;
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
            _solutionDir = GetIsolatedTestSolutionDir();
            _solutionFile = Path.Combine(_solutionDir.FullName, Paths.AdapterTestsSolutionFile.Name);
            _packageSource = GetIsolatedPackageSourceFromThisSolution();
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
            var console = new Mock<IConsole>();

            var nugetify = GetNugetifyCommand(console, _solutionFile, _packageSource);
            nugetify.ExecuteCommand();

            ConsoleMock.AssertConsoleHasNoErrorsOrWarnings(console);
        }

        private static Nugetify GetNugetifyCommand(Mock<IConsole> console, string solutionFile, DirectoryInfo packageSource)
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

        private static DirectoryInfo GetIsolatedTestSolutionDir()
        {
            var solutionDir = new DirectoryInfo(Path.GetRandomFileName());
            CopyFilesRecursively(new DirectoryInfo(Paths.TestSolutionForAdapterFolder), solutionDir);
            return solutionDir;
        }

        private static DirectoryInfo GetIsolatedPackageSourceFromThisSolution()
        {
            var packageSource = new DirectoryInfo(Path.GetRandomFileName());
            CopyFilesRecursively(new DirectoryInfo("../packages"), packageSource);
            return packageSource;
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            if (!target.Exists) target.Create();
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
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
