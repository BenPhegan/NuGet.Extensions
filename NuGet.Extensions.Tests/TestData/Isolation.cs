using System.IO;

namespace NuGet.Extensions.Tests.TestData
{
    public class Isolation {
        public static DirectoryInfo GetIsolatedTestSolutionDir()
        {
            var solutionDir = new DirectoryInfo(Path.GetRandomFileName());
            CopyFilesRecursively(new DirectoryInfo(Paths.TestSolutionForAdapterFolder), solutionDir);
            return solutionDir;
        }

        public static DirectoryInfo GetIsolatedPackageSourceFromThisSolution()
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
}