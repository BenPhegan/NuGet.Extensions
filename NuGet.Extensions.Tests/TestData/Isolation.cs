using System.IO;

namespace NuGet.Extensions.Tests.TestData
{
    public class Isolation 
    {
        public static DirectoryInfo GetIsolatedTestSolutionDir()
        {
            var solutionDir = new DirectoryInfo(GetRandomTempDirectoryPath());
            CopyFilesRecursively(new DirectoryInfo(Paths.TestSolutionForAdapterFolder), solutionDir);
            return solutionDir;
        }

        private static string GetRandomTempDirectoryPath()
        {
            return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        public static DirectoryInfo GetIsolatedEmptyPackageSource()
        {
            var isolatedPackageSourceFromThisSolution = new DirectoryInfo(GetRandomTempDirectoryPath());
            isolatedPackageSourceFromThisSolution.Create();
            return isolatedPackageSourceFromThisSolution;
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
}