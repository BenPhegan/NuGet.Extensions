using System;
using System.Collections.Generic;
namespace NuGet.Extras.PackageReferences
{
    /// <summary>
    /// Provides the ability to resolve a set of packages that will meet a set of common constraints.
    /// </summary>
    public interface IPackageReferenceSetResolver
    {
        /// <summary>
        /// Resolves the specified PackageReferences into a single set that allows all required version constraints to be satisfied, or returns a list of which cant.
        /// Uses a Tuple as the return type, Item1 are the succesfuly resolved PackageReferences, and Item2 are the failed ones.
        /// </summary>
        /// <param name="references">The references.</param>
        /// <returns></returns>
        Tuple<IEnumerable<PackageReference>, IEnumerable<PackageReference>> Resolve(IEnumerable<PackageReference> references);
    }
}
