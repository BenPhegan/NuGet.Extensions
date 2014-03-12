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
        [Test]
        public void NugetifyThrowsNoErrorsWhenNoPackagesFound()
        {
            var console = new Mock<IConsole>();
            var solutionDir = GetIsolatedTestSolutionDir();
            var solutionFile = Path.Combine(solutionDir.FullName, Paths.AdapterTestsSolutionFile.Name);
            var packageSource = GetIsolatedPackageSourceFromThisSolution();

            var nugetify = GetNugetifyCommand(console, solutionFile, packageSource);
            nugetify.ExecuteCommand();

            packageSource.Delete(true);
            solutionDir.Delete(true);
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
