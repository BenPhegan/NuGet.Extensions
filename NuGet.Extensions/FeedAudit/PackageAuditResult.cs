using System.Collections.Generic;
using System.Reflection;

namespace NuGet.Extensions.FeedAudit
{
    public class PackageAuditResult
    {
        public IPackage Package;

        /// <summary>
        /// Assembly references that are resolved internally via file includes or by assemblies present in direct package dependencies.
        /// </summary>
        public List<AssemblyName> ResolvedAssemblyReferences = new List<AssemblyName>();

        /// <summary>
        /// Represents the assembly references that can't be resolved internal to the package or the packages direct stated package dependencies.
        /// </summary>
        public List<AssemblyName> UnresolvedAssemblyReferences = new List<AssemblyName>();

        /// <summary>
        /// Package files that we could not load via Reflection, probably either not .Net assemblies or corrupt.
        /// </summary>
        public List<string> UnloadablePackageFiles = new List<string>();

        /// <summary>
        /// Package dependencies where we have resolved an assembly dependency based on the package contents.
        /// </summary>
        public List<IPackage> UsedPackageDependencies = new List<IPackage>();

        /// <summary>
        /// Package dependencies that do not appear to be required to provide runtime references for any files in the package.
        /// </summary>
        public List<IPackage> UnusedPackageDependencies = new List<IPackage>(); 

        /// <summary>
        /// Package dependencies that we could not find on the feed provided.
        /// </summary>
        public List<PackageDependency> UnresolvedDependencies = new List<PackageDependency>();

        /// <summary>
        /// References that we could not find in the package or the packages direct dependencies, but that can be found on the referenced feed.
        /// </summary>
        public List<AssemblyName> FeedResolvableReferences = new List<AssemblyName>();

        /// <summary>
        /// References that we could not find in the package or the packages direct dependencies, but that can be found in the GAC of the machine running the audit (this does NOT mean they will be present wherever they are run).
        /// </summary>
        public List<AssemblyName> GacResolvableReferences = new List<AssemblyName>();

        /// <summary>
        /// Assembly references that we could not find in the package or the packages direct dependencies, nor on the feed or the GAC.  These simply arent available.  This is probably bad.
        /// </summary>
        public List<AssemblyName> UnresolvableReferences = new List<AssemblyName>();

        /// <summary>
        /// Assemblies that were excluded from the audit via command line parameter
        /// </summary>
        public List<AssemblyName> AuditExclusionReferences = new List<AssemblyName>();
    }
}