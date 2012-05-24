using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Extensions.GetLatest.Configuration
{
    public static class PackageRepositoryExtensions
    {
        public static IPackage FindLatestPackage(this IPackageRepository repository, string packageId)
        {
            if (packageId == null)
            {
                throw new ArgumentNullException("packageId");
            }

            return repository.GetPackages().Where(l => l.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase)).Where(l => l.IsLatestVersion).FirstOrDefault();

        }
    }
}
