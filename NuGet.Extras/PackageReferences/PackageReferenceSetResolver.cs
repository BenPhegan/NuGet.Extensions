using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Extras.ExtensionMethods;

namespace NuGet.Extras.PackageReferences
{
    /// <summary>
    /// Resolves a set of packages to the smallest distinct set that provides all required constrained versions.
    /// </summary>
    public class PackageReferenceSetResolver : IPackageReferenceSetResolver
    {
        /// <summary>
        /// Resolves the set of correct PackageReferences based on the smallest set of VersionSpec per Package Id that satisfies all
        /// the specific Package Ids in the same list.
        /// </summary>
        /// <param name="references">The list of PackageReferences.</param>
        /// <returns>A Tuple where List1 are the resolved PackageReferences and List2 are the PackageReferences we failed to resolve</returns>
        public Tuple<IEnumerable<PackageReference>, IEnumerable<PackageReference>> Resolve(IEnumerable<PackageReference> references)
        {
            var cleanAllowedVersions = new List<PackageReference>();
            var cleanVersions = new List<PackageReference>();
            var failed = new List<PackageReference>();
            var fullyResolved = new List<PackageReference>();

            //Need to split into two lists
            //1. PackageReference for Id with only a version
            //2. PackageReference for Id with an additional allowedVersion

            var versionGroups = references.Where((x) => x.VersionConstraint == null || x.VersionConstraint.MaxVersion == null && x.VersionConstraint.MinVersion == null).GroupBy((p) => p.Id);
            var allowedVersionGroups = references.Where((x) => x.VersionConstraint != null && x.VersionConstraint.MaxVersion != null && x.VersionConstraint.MinVersion != null).GroupBy((p) => p.Id);

            //TODO Duplicate of the code below, extract out.
            //Make sure that we do not have conflicting specific versions for the same ID
            foreach (var group in versionGroups)
            {
                var package = ResolveValidVersion(group);
                if (package != null)
                {
                    cleanVersions.Add(package);
                }
                else
                {
                    failed.AddRange(group);
                }
            }

            //Check specific allowedVersions and get the reduced set per id
            foreach (var group in allowedVersionGroups)
            {
                var package = ResolveValidVersionSpec(group);
                if (package != null)
                {
                    cleanAllowedVersions.Add(package);
                }
                else
                {
                    failed.AddRange(group);
                }
            }

            //Now ensure that we can satisfy any non-allowedVersions Version request for the same id
            foreach (var packageVersion in cleanVersions)
            {
                var vs = cleanAllowedVersions.Where((x) => x.Id == packageVersion.Id).FirstOrDefault();
                if (vs != null)
                {
                    //remove it from the list...
                    cleanAllowedVersions.Remove(vs);
                    if (vs.VersionConstraint.Satisfies(packageVersion.Version))
                    {
                        //TODO should we be just getting the explicit version in this case and define it without a allowedVersions range?  Or leave both in?
                        //Thinking about this, if we have two and one is specific, then we need to only take the specific one....
                        //fullyResolved.Add(vs);
                        fullyResolved.Add(packageVersion);
                    }
                    else
                    {
                        failed.Add(vs);
                        failed.Add(packageVersion);
                    }
                }
                else
                {
                    fullyResolved.Add(packageVersion);
                }
            }

            //Last but not least, add any remaining clean allowedVersion PackageReferences
            fullyResolved.AddRange(cleanAllowedVersions);

            //Finally, return the list of Id PackageVersions

            return Tuple.Create(fullyResolved.AsEnumerable(), failed.AsEnumerable());
        }

        /// <summary>
        /// Resolves the largest valid VersionSpec across a set of PackageReference objects.
        /// </summary>
        /// <param name="packageReferences">The package references.</param>
        /// <returns></returns>
        internal PackageReference ResolveValidVersionSpec(IEnumerable<PackageReference> packageReferences)
        {
            if (packageReferences.Count() != 0)
            {
                var failures = new List<PackageReference>();
                SemanticVersion winner = null;
                VersionSpec smallest = ReturnLargestVersionSpec();
                string id = packageReferences.First().Id;

                foreach (var pr in packageReferences)
                {
                    //First, does this VersionSpec sit completely outside the range of the existing smallest set?
                    bool minGreaterThanCurrentMax = smallest.IsMaxInclusive && pr.VersionConstraint.IsMinInclusive ? pr.VersionConstraint.MinVersion > smallest.MaxVersion : pr.VersionConstraint.MinVersion >= smallest.MaxVersion;
                    bool maxLessThanCurrentMin = smallest.IsMinInclusive && pr.VersionConstraint.IsMaxInclusive ? pr.VersionConstraint.MaxVersion < smallest.MinVersion : pr.VersionConstraint.MaxVersion <= smallest.MinVersion;

                    if (minGreaterThanCurrentMax || maxLessThanCurrentMin)
                    {
                        failures.Add(pr);
                    }
                    else
                    {
                        //Now, is it more restrictive than the smallest?
                        bool minMoreConstrictive = (!pr.VersionConstraint.IsMinInclusive && smallest.IsMinInclusive && (pr.VersionConstraint.MinVersion == smallest.MinVersion)) || pr.VersionConstraint.MinVersion > smallest.MinVersion;
                        bool maxMoreConstrictive = (!pr.VersionConstraint.IsMaxInclusive && smallest.IsMaxInclusive && (pr.VersionConstraint.MaxVersion == smallest.MaxVersion)) || pr.VersionConstraint.MaxVersion < smallest.MaxVersion;
                        if (minMoreConstrictive)
                        {
                            smallest.MinVersion = pr.VersionConstraint.MinVersion;
                            smallest.IsMinInclusive = pr.VersionConstraint.IsMinInclusive;
                            winner = pr.Version;
                        }
                        if (maxMoreConstrictive)
                        {
                            smallest.MaxVersion = pr.VersionConstraint.MaxVersion;
                            smallest.IsMaxInclusive = pr.VersionConstraint.IsMaxInclusive;
                            winner = pr.Version;
                        }
                    }
                }

                //If we get no failures, set the ResolvedVersionSpec, otherwise it stays at null
                if (failures.Count == 0)
                {
                    var pr = new PackageReference(id, winner, smallest, new FrameworkName(".NET Framework, Version=4.0"));
                    return pr;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the largest version spec we can think of to start narrowing down from.
        /// </summary>
        /// <returns></returns>
        private static VersionSpec ReturnLargestVersionSpec()
        {
            VersionSpec smallest = new VersionSpec()
            {
                MinVersion = SemanticVersion.Parse("0.0.0.0"),
                MaxVersion = SemanticVersion.Parse("999.999.999.999"),
                IsMinInclusive = true,
                IsMaxInclusive = true
            };
            return smallest;
        }

        /// <summary>
        /// Resolves a valid Version that is common across a list of PackageReference objects.
        /// </summary>
        /// <param name="packageReferences">The package references.</param>
        /// <returns></returns>
        internal PackageReference ResolveValidVersion(IEnumerable<PackageReference> packageReferences)
        {
            var resolvedVersions = packageReferences.Distinct();
            return resolvedVersions.Count() == 1 ? packageReferences.First() : null;
        }
    }
}
