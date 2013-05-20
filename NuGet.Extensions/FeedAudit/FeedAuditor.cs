using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NuGet.Extensions.FeedAudit
{
    /// <summary>
    /// Allows assembly and package dependency auditing for NuGet feeds
    /// </summary>
    public class FeedAuditor
    {
        public delegate void PackageAuditEventHandler(object sender, PackageAuditEventArgs args);
        public delegate void PackageIgnoreEventHandler(object sender, PackageIgnoreEventArgs args);
        private readonly IPackageRepository _packageRepository;
        private readonly List<string> _exceptions; 
        private List<PackageAuditResult> _results = new List<PackageAuditResult>();
        private readonly bool _unlisted;
        private List<IPackage> _auditPackages;
        private List<IPackage> _feedPackages;
        private readonly bool _checkForFeedResolvableAssemblies;
        private readonly bool _checkGac;
        private readonly IEnumerable<Regex> _wildcards;
        private List<AssemblyName> _unresolvableAssemblyReferences = new List<AssemblyName>();
        private readonly List<string> _assemblyExceptions;
        private readonly IEnumerable<Regex> _assemblyWildcards;

        public event PackageAuditEventHandler StartPackageAudit = delegate { };
        public event PackageAuditEventHandler FinishedPackageAudit = delegate { };
        public event EventHandler StartPackageListDownload = delegate { };
        public event EventHandler FinishedPackageListDownload = delegate { };
        public event PackageIgnoreEventHandler PackageIgnored = delegate { }; 

        public FeedAuditor(IPackageRepository packageRepository, IEnumerable<String> exceptions, IEnumerable<Regex> wildcards, Boolean unlisted, bool checkForFeedResolvableAssemblies, bool checkGac, IEnumerable<String> assemblyExceptions, IEnumerable<Regex> assemblyWildcards)
        {
            _packageRepository = packageRepository;
            _exceptions = exceptions.ToList();
            _unlisted = unlisted;
            _checkForFeedResolvableAssemblies = checkForFeedResolvableAssemblies;
            _checkGac = checkGac;
            _wildcards = wildcards;
            _assemblyExceptions = assemblyExceptions.ToList();
            _assemblyWildcards = assemblyWildcards;
        }

        /// <summary>
        /// Audits a feed and provides back a set of results
        /// </summary>
        /// <returns></returns>
        public List<PackageAuditResult> Audit(IPackage packageToAudit = null)
        {
            StartPackageListDownload(this, new EventArgs());
            _feedPackages = _packageRepository.GetPackages().Where(p => p.IsLatestVersion).OrderBy(p => p.Id).ToList();
            FinishedPackageListDownload(this, new EventArgs());

            //If we are auditing the whole feed, use the whole feed as the audit package list.....
            _auditPackages = packageToAudit == null ? _feedPackages : new List<IPackage>(new[] {packageToAudit});

            foreach (var package in _auditPackages)
            {
                //OData wont let us query this remotely (again, fuck OData).
                if (_unlisted == false && package.Listed == false) continue;
                
                StartPackageAudit(this, new PackageAuditEventArgs {Package = package});

                //Try the next one if we are using this one as an exception
                if (_wildcards.Any(w => w.IsMatch(package.Id.ToLowerInvariant())))
                {
                    PackageIgnored(this, new PackageIgnoreEventArgs {IgnoredPackage = package, Wildcard = true});
                    continue;
                }
                if (_exceptions.Any(e => e.Equals(package.Id,StringComparison.OrdinalIgnoreCase)))
                {
                    PackageIgnored(this, new PackageIgnoreEventArgs { IgnoredPackage = package, StringMatch = true });
                    continue;
                }

                var currentResult = new PackageAuditResult {Package = package};
                var actualAssemblyReferences = GetPackageAssemblyReferenceList(package, currentResult);

                //Prune dependency list based on additional assemblies included in the package...
                actualAssemblyReferences = RemoveInternallySatisfiedDependencies(actualAssemblyReferences, package);

                var packageDependencies = GetDependencyAssemblyList(package, currentResult).ToList();
                actualAssemblyReferences = RemoveAssemblyExclusions(actualAssemblyReferences, currentResult);

                var usedDependencies = new List<IPackage>();
                foreach (var actualDependency in actualAssemblyReferences)
                {
                    var possibles = GetPossiblePackagesForAssembly(actualDependency, packageDependencies).ToList();
                    usedDependencies.AddRange(possibles.Select(p => p));
                    if (!possibles.Any())
                    {
                        currentResult.UnresolvedAssemblyReferences.Add(actualDependency);
                        if (_checkForFeedResolvableAssemblies)
                        {
                            //May be expensive....
                            if (GetPossiblePackagesForAssembly(actualDependency, _feedPackages).Any())
                                currentResult.FeedResolvableReferences.Add(actualDependency);
                        }

                        if (_checkGac)
                        {
                            if (CanResolveToGac(actualDependency.FullName) || CanResolveToGac(actualDependency.Name))
                                currentResult.GacResolvableReferences.Add(actualDependency);
                        }
                    }
                    else
                        currentResult.ResolvedAssemblyReferences.Add(actualDependency);
                }

                currentResult.UsedPackageDependencies.AddRange(packageDependencies.Where(usedDependencies.Contains).Select(l => l));
                currentResult.UnusedPackageDependencies.AddRange(packageDependencies.Where(p => !usedDependencies.Contains(p)).Select(l => l));
                _results.Add(currentResult);
                FinishedPackageAudit(this, new PackageAuditEventArgs{Package = package});
            }
            _unresolvableAssemblyReferences = GetUnresolvedAssemblies(_results);
            UpdateUnresolvablePackageAuditResults(_results, _unresolvableAssemblyReferences);
            return _results;
        }

        private IEnumerable<AssemblyName> RemoveAssemblyExclusions(IEnumerable<AssemblyName> actualAssemblyReferences, PackageAuditResult currentResult)
        {
	        var actualAssemblyReferencesList = actualAssemblyReferences.ToList();
			var assemblyReferences = new List<AssemblyName>(actualAssemblyReferencesList);
			foreach (var assembly in actualAssemblyReferencesList)
            {
                if (_assemblyWildcards.Any(w => w.IsMatch(assembly.Name.ToLowerInvariant())))
                {
                    assemblyReferences.Remove(assembly);
                    currentResult.AuditExclusionReferences.Add(assembly);
                }

                if (_assemblyExceptions.Any(e => e.Equals(assembly.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    assemblyReferences.Remove(assembly);
                    currentResult.AuditExclusionReferences.Add(assembly);
                }
            }

            return assemblyReferences;
        }


        private void UpdateUnresolvablePackageAuditResults(IEnumerable<PackageAuditResult> results, IEnumerable<AssemblyName> unresolvableAssemblyReferences)
        {
            var unresolvable = unresolvableAssemblyReferences as List<AssemblyName> ?? unresolvableAssemblyReferences.ToList();
            foreach (var packageAuditResult in results)
            {
                packageAuditResult.UnresolvableReferences = packageAuditResult.UnresolvedAssemblyReferences.Where(unresolvable.Contains).ToList();
            }
        }

        private static List<AssemblyName> GetUnresolvedAssemblies(List<PackageAuditResult> results)
        {
            var unresolvable = new List<AssemblyName>();
            foreach (var unresolved in results.SelectMany(r => r.UnresolvedAssemblyReferences))
            {
                if (!results.Any(r => r.FeedResolvableReferences.Any(fr => fr.Name.Equals(unresolved.Name, StringComparison.OrdinalIgnoreCase))) &&
                    !results.Any(r => r.GacResolvableReferences.Any(gr => gr.Name.Equals(unresolved.Name, StringComparison.OrdinalIgnoreCase))))
                {
                    unresolvable.Add(unresolved);
                }
            }
            return unresolvable;
        }
        private static bool CanResolveToGac(string actualDependency)
        {
            string result;
            return GacResolver.AssemblyExist(actualDependency, out result);
        }

        private static IEnumerable<IPackage> GetPossiblePackagesForAssembly(AssemblyName actualDependency, IEnumerable<IPackage> packageDependencies)
        {
            return packageDependencies.Where(d => d.GetFiles().Any(a => new FileInfo(a.Path).Name.Equals(actualDependency.Name + ".dll", StringComparison.OrdinalIgnoreCase)));
        }

        private static IEnumerable<AssemblyName> RemoveInternallySatisfiedDependencies(IEnumerable<AssemblyName> actualAssemblyReferences, IPackage package)
        {
            var fileNames = GetFileInfoListFromPackageFiles(package);
            return actualAssemblyReferences.Where(a => !fileNames.Contains(a.Name.ToLowerInvariant() + ".dll") && !fileNames.Contains(a.Name.ToLowerInvariant() + ".exe"));
        }

        private static List<string> GetFileInfoListFromPackageFiles(IPackage package)
        {
            return package.GetFiles().Select(f => new FileInfo(f.Path).Name.ToLowerInvariant()).ToList();
        }

        /// <summary>
        /// Returns a dictionary mapping of IPackages to their included files.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private IEnumerable<IPackage> GetDependencyAssemblyList(IPackage package, PackageAuditResult result)
        {
            var packageDependencies = new List<IPackage>();
            foreach (var dependency in package.DependencySets.SelectMany(s => s.Dependencies))
            {
                //HACK Slow and wrong and evil and I HATE ODATA.
                var dependencyPackage = _feedPackages.FirstOrDefault(p => p.Id.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase));
                if (dependencyPackage == null)
                {
                    result.UnresolvedDependencies.Add(dependency);
                    continue;
                }
                packageDependencies.Add(dependencyPackage);
            }
            return packageDependencies;
        }

        /// <summary>
        /// Gets a list of AssemblyNames referenced by the files in a package
        /// </summary>
        /// <param name="package">The package to check.</param>
        /// <param name="result">A result to append errors to.</param>
        /// <returns></returns>
        private static IEnumerable<AssemblyName> GetPackageAssemblyReferenceList(IPackage package, PackageAuditResult result)
        {
            var actualDependencies = new List<AssemblyName>();
            foreach (var file in package.GetFiles().Where(f => f.Path.EndsWith(".dll") || f.Path.EndsWith("*.exe")))
            {
                using (var stream = file.GetStream())
                {
                    try
                    {
                        var assembly = Assembly.Load(stream.ReadAllBytes());
                        actualDependencies.AddRange(assembly.GetReferencedAssemblies());
                    }
                    catch (Exception)
                    {
                        result.UnloadablePackageFiles.Add(file.Path);
                    }
                }
            }
            return actualDependencies.Where(d => !IsProbablySystemAssembly(d)).Distinct(new AssemblyNameEqualityComparer());
        }

        /// <summary>
        /// Checks whether an assembly name is probably a system (ie GAC) assembly.  Not infallible.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private static bool IsProbablySystemAssembly(AssemblyName d)
        {
            return d.Name.StartsWith("System.") || d.Name.Equals("System") || d.Name.Equals("mscorlib");
        }
    }
}