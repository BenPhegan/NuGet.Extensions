using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Extensions.ExtensionMethods
{
    public static class IPackageExtensions
    {
        public static string GetFileLocationFromPackage(this IPackage package, string key)
        {
            return (from fileLocation in package.GetFiles()
                    where fileLocation.Path.ToLowerInvariant().EndsWith(key, StringComparison.OrdinalIgnoreCase)
                    select fileLocation.Path).FirstOrDefault();
        }
    }
}
