using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NuGet.Extensions.Comparers
{
    /// <summary>
    /// Provides a set of Comparers to enable Packages comparisons to be evaluated in specific ways.
    /// </summary>
    public class PackageReferenceEqualityComparer : IEqualityComparer<PackageReference>
    {
        /// <summary>
        /// Check Package equality using the PackageID, Version and VersionsConstraints
        /// </summary>
        public static readonly PackageReferenceEqualityComparer IdVersionAndAllowedVersions = new PackageReferenceEqualityComparer((x, y) =>
        {
            var versionSpecEqualityComparer = new VersionSpecEqualityComparer(x.VersionConstraint);
            if (x.VersionConstraint == null ^ y.VersionConstraint == null)
            {
                return false;
            }

            if (x.VersionConstraint == null && y.VersionConstraint == null)
            {
                return x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase) && x.Version.Equals(y.Version);
            }

            //return x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase) && x.Version.Equals(y.Version) && x.VersionConstraint.Equals(y.VersionConstraint);
            return x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase) && x.Version.Equals(y.Version) && versionSpecEqualityComparer.Equals(y.VersionConstraint);

        },
            x =>
            {
                var versionSpecEqualityComparer = new VersionSpecEqualityComparer(x.VersionConstraint);
                var first = x.Id.GetHashCode() ^ x.Version.GetHashCode();
                first = x.VersionConstraint != null ? first ^ versionSpecEqualityComparer.GetHashCode() : first;
                return first;
            });

        /// <summary>
        /// Check Package equality using the PackageID and Version only.
        /// </summary>
        public static readonly PackageReferenceEqualityComparer IdAndVersion = new PackageReferenceEqualityComparer((x, y) =>
        {
            if (x.Version == null ^ y.Version == null)
            {
                return false;
            }

            if (x.Version == null && y.Version == null)
            {
                return x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase);
            }

            return x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase) && x.Version.Equals(y.Version);
        },
                x =>
                {
                    return x.Version == null ? x.Id.GetHashCode() : x.Id.GetHashCode() ^ x.Version.GetHashCode();
                });

        /// <summary>
        /// Check Package equality using the PackageID only.
        /// </summary>
        public static readonly PackageReferenceEqualityComparer IdAndAllowedVersions = new PackageReferenceEqualityComparer(
            (x, y) =>
                {
                    var versionSpecComparer = new VersionSpecEqualityComparer(x.VersionConstraint);
                    return x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase) && versionSpecComparer.Equals(y.VersionConstraint);
                }, 
                x =>
                    {
                        var versionSpecEqualityComparer = new VersionSpecEqualityComparer(x.VersionConstraint);
                        return x.VersionConstraint == null ? x.Id.GetHashCode() : x.Id.GetHashCode() ^ versionSpecEqualityComparer.GetHashCode();
                    });
   


        /// <summary>
        /// Check Package equality using the PackageID only.
        /// </summary>
        public static readonly PackageReferenceEqualityComparer Id = new PackageReferenceEqualityComparer((x, y) => x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase),
                                                                                        x => x.Id.GetHashCode());

        private readonly Func<PackageReference, PackageReference, bool> _equals;
        private readonly Func<PackageReference, int> _getHashCode;

        private PackageReferenceEqualityComparer(Func<PackageReference, PackageReference, bool> equals, Func<PackageReference, int> getHashCode)
        {
            _equals = equals;
            _getHashCode = getHashCode;
        }

        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <returns>
        /// true if the specified objects are equal; otherwise, false.
        /// </returns>
        /// <param name="x">The first object of type <paramref>
        ///                                            <name>T</name>
        ///                                          </paramref> to compare.</param><param name="y">The second object of type <paramref>
        ///                                                                                                                     <name>T</name>
        ///                                                                                                                   </paramref> to compare.</param>
        public bool Equals(PackageReference x, PackageReference y)
        {
            return _equals(x, y);
        }

        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <returns>
        /// A hash code for the specified object.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> for which a hash code is to be returned.</param><exception cref="T:System.ArgumentNullException">The type of <paramref name="obj"/> is a reference type and <paramref name="obj"/> is null.</exception>
        public int GetHashCode(PackageReference obj)
        {
            return _getHashCode(obj);
        }
    }
}