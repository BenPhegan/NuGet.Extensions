using System;

namespace NuGet.Extensions.ExtensionMethods
{
    public static class IPackageExtensions
    {
        
        public static bool Equals(this IPackage package, IPackage other)
        {
            return string.Equals(package.Id, other.Id) && Equals(package.Version, other.Version);
        }

        public static int GetHashCode(this IPackage package)
        {
            unchecked
            {
                return ((package.Id != null ? package.Id.GetHashCode() : 0) * 397) ^ (package.Version != null ? package.Version.GetHashCode() : 0);
            }
        }

    }
}
