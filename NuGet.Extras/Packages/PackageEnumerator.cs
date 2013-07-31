using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Extras.Comparers;

namespace NuGet.Extras.Packages
{
    /// <summary>
    /// Manages the enumeration of a list of PackageReferenceFile objects.
    /// </summary>
    public class PackageEnumerator : IPackageEnumerator
    {
        /// <summary>
        /// Gets the package references.
        /// </summary>
        /// <param name="packageReferenceFiles">The package reference files.</param>
        /// <param name="logCount">The log count.</param>
        /// <param name="comparer"> </param>
        /// <returns></returns>
        public IEnumerable<PackageReference> GetPackageReferences(IEnumerable<PackageReferenceFile> packageReferenceFiles, Action<string, string> logCount, PackageReferenceEqualityComparer comparer = null)
        {
            //Get the full list of all packages, minus version numbers
            var packages = new List<PackageReference>();

            foreach (PackageReferenceFile packageReferenceFile in packageReferenceFiles)
                packages.AddRange(packageReferenceFile.GetPackageReferences());

            int total = packages.Count;

            //Use the distinct list based on the comparer
			if (comparer == null)
				comparer = PackageReferenceEqualityComparer.IdVersionAndAllowedVersions;
            packages = packages.Distinct(comparer).ToList();
            logCount(packages.Count.ToString(), total.ToString());

            //Now, get a list of the reduced set...

            return packages;
        }
    }
}
