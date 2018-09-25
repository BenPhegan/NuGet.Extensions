using System;

namespace NuGet.Extensions.Commands.GraphCommand
{
    public static class Comparers
    {
        internal static bool DependencyEquals(PackageDependency dep1, PackageDependency dep2)
        {
            return 0 == DependencyComparer(dep1, dep2);
        }

        internal static int DependencyComparer(PackageDependency dep1, PackageDependency dep2)
        {
            int cmp = StringComparer.OrdinalIgnoreCase.Compare(dep1.Id, dep2.Id);
            if (cmp != 0)
            {
                return cmp;
            }
            return VersionComparison((VersionSpec)dep1.VersionSpec, (VersionSpec)dep2.VersionSpec);
        }

        private static int VersionComparison(VersionSpec ver1, VersionSpec ver2)
        {
            if (ver1 == null && ver2 == null)
            {
                return 0;
            }
            if (ver1 == null)
            {
                return -1;
            }
            if (ver2 == null)
            {
                return 1;
            }

                
            var cmp = VersionComparison(ver1.MinVersion, ver2.MinVersion);
            if (cmp != 0)
            {
                return cmp;
            }

            return VersionComparison(ver1.MaxVersion, ver2.MaxVersion);
        }

        private static int VersionComparison(SemanticVersion ver1, SemanticVersion ver2)
        {
            if (ver1 == null && ver2 == null)
            {
                return 0;
            }

            return ver1 != null ? ver1.CompareTo(ver2) : 1;
        }
    }
}