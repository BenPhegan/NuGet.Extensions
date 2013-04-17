using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet;
using NuGet.Extras.Packages;
using System.IO;
using System.Xml.Linq;

namespace NuGet.Extensions
{
    public static class GetExtensions
    {
        public static void ResolvePackages(this List<PackageReference> packages, List<PackageReference> packageReferences, IPackageResolutionManager packageResolutionManager, IPackageRepository remoteRepository)
        {
            foreach (var packageReference in packageReferences)
            {
                var resolvedPackage = packageResolutionManager.ResolveLatestInstallablePackage(remoteRepository, packageReference);
                if (!packages.Contains(packageReference))
                {
                    packages.Add(new PackageReference(resolvedPackage.Id, resolvedPackage.Version, packageReference.VersionConstraint));
                    packages.ResolvePackages(resolvedPackage.Dependencies.ToList().ConvertAll(d => new PackageReference(d.Id, null, d.VersionSpec)), packageResolutionManager, remoteRepository);
                }
            }
        }

        public static FileInfo Save(this List<PackageReference> packages, string directory)
        {
            string fileName = Path.Combine(directory, "packages.config");
            XDocument xml = CreatePackagesConfigXml(packages);
            xml.Save(fileName);
            return new FileInfo(fileName);
        }

        private static XDocument CreatePackagesConfigXml(List<PackageReference> packages)
        {
            var doc = new XDocument();
            var packagesElement = new XElement("packages");

            foreach (PackageReference p in packages)
            {
                var packageXml = new XElement("package");
                packageXml.SetAttributeValue("id", p.Id);
                packageXml.SetAttributeValue("version", p.Version);
                if (p.VersionConstraint != null)
                    packageXml.SetAttributeValue("allowedVersions", p.VersionConstraint.ToString());
                packagesElement.Add(packageXml);
            }

            doc.Add(packagesElement);
            return doc;
        }
    }
}
