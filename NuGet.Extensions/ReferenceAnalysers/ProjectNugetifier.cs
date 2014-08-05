using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Common;
using NuGet.Extensions.Comparers;
using NuGet.Extensions.ExtensionMethods;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.Repositories;

namespace NuGet.Extensions.ReferenceAnalysers
{
    public class ProjectNugetifier
    {
        private readonly IConsole _console;
        private readonly IFileSystem _projectFileSystem;
        private readonly IVsProject _vsProject;
        private readonly IPackageRepository _packageRepository;
        private static readonly string PackageReferenceFilename = Constants.PackageReferenceFile;
        private readonly IHintPathGenerator _hintPathGenerator;

        public ProjectNugetifier(IVsProject vsProject, IPackageRepository packageRepository, IFileSystem projectFileSystem, IConsole console, IHintPathGenerator hintPathGenerator)
        {
            _console = console;
            _projectFileSystem = projectFileSystem;
            _vsProject = vsProject;
            _packageRepository = packageRepository;
            _hintPathGenerator = hintPathGenerator;
        }

        public ICollection<IPackage> NugetifyReferences(DirectoryInfo solutionDir)
        {
            var references = GetReferences().ToList();
            var resolvedMappings = ResolveReferenceMappings(references).ToDictionary(p => p.Key, p => p.Value);
            var packageReferencesAdded = new HashSet<IPackage>(new LambdaComparer<IPackage>(IPackageExtensions.Equals, IPackageExtensions.GetHashCode));

            foreach (var reference in references)
            {
                string assemblyFilename = reference.AssemblyFilename;
                var includeName = reference.AssemblyName;
                var includeVersion = reference.AssemblyVersion;

                List<IPackage> matchingPackages;
                if (resolvedMappings.TryGetValue(assemblyFilename, out matchingPackages))
                {

                    var package = matchingPackages.OrderBy(p => p.GetFiles().Count()).First();
                    packageReferencesAdded.Add(package);
                    LogHintPathRewriteMessage(package, includeName, includeVersion);

                    var newHintPath = _hintPathGenerator.ForAssembly(solutionDir, _vsProject.ProjectDirectory,
                        package, assemblyFilename);
                    reference.ConvertToNugetReferenceWithHintPath(newHintPath);
                }
            }

            return packageReferencesAdded;
        }

        private IEnumerable<IReference> GetReferences()
        {
            var binaryReferences = _vsProject.GetBinaryReferences();
            var projectReferences = _vsProject.GetProjectReferences();
            return binaryReferences.Concat(projectReferences);
        }

        private void LogHintPathRewriteMessage(IPackage package, string includeName, string includeVersion)
        {
            var message = string.Format("Attempting to update hintpaths for \"{0}\" {1}using package \"{2}\" version \"{3}\"",
                includeName,
                string.IsNullOrEmpty(includeVersion) ? "" : "version \"" + includeVersion + "\" ",
                package.Id,
                package.Version);
            if (package.Id.Equals(includeName, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(includeVersion) && package.Version.Version != SemanticVersion.Parse(includeVersion).Version) _console.WriteWarning(message);
                else _console.WriteLine(message);
            }
            else _console.WriteWarning(message);
        }

        public void AddNugetReferenceMetadata(ISharedPackageRepository sharedPackagesRepository, ICollection<IPackage> packagesToAdd, FrameworkName targetFramework)
        {
            _console.WriteLine("Checking for any project references for {0}...", PackageReferenceFilename);
            if (!packagesToAdd.Any()) return;
            CreatePackagesConfig(packagesToAdd, targetFramework);
            RegisterPackagesConfig(sharedPackagesRepository);
        }

        private void CreatePackagesConfig(ICollection<IPackage> packagesToAdd, FrameworkName targetFramework)
        {
            _console.WriteLine("Creating {0}", PackageReferenceFilename);
            var packagesConfig = new PackageReferenceFile(_projectFileSystem, PackageReferenceFilename);
            foreach (var package in packagesToAdd)
            {
                if (!packagesConfig.EntryExists(package.Id, package.Version)) //Note we don't re-add entries that have the wrong targetFramework set
                {
                    packagesConfig.AddEntry(package.Id, package.Version, false, targetFramework);
                }
            }
        }

        private void RegisterPackagesConfig(ISharedPackageRepository sharedPackagesRepository)
        {
            var packagesConfigFilePath = Path.Combine(_vsProject.ProjectDirectory.FullName + "\\", PackageReferenceFilename);
            sharedPackagesRepository.RegisterRepository(packagesConfigFilePath);
            _vsProject.AddFile(PackageReferenceFilename);
        }

        private IEnumerable<KeyValuePair<string, List<IPackage>>> ResolveReferenceMappings(IEnumerable<IReference> references)
        {
            var referenceList = GetReferencedAssemblies(references);
            if (referenceList.Any())
            {
                _console.WriteLine("Checking feed for {0} references...", referenceList.Count);

                IQueryable<IPackage> packageSource = _packageRepository.GetPackages();
                var assemblyResolver = new RepositoryAssemblyResolver(referenceList, packageSource, _projectFileSystem, _console);
                var referenceMappings = assemblyResolver.GetAssemblyToPackageMapping(false);
                referenceMappings.OutputPackageConfigFile();
                //next, lets rewrite the project file with the mappings to the new location...
                //Going to have to use the mapping to assembly name that we get back from the resolve above
                _console.WriteLine();
                _console.WriteLine("Found {0} package to assembly mappings on feed...", referenceMappings.ResolvedMappings.Count());
                referenceMappings.FailedMappings.ToList().ForEach(f => _console.WriteLine("Could not match: {0}", f));
                return referenceMappings.ResolvedMappings;
            }

            _console.WriteLine("No references found to resolve (all GAC?)");
            return Enumerable.Empty<KeyValuePair<string, List<IPackage>>>();
        }

        private List<string> GetReferencedAssemblies(IEnumerable<IReference> references)
        {
            var referenceFiles = new List<string>();

            foreach (var reference in references)
            {
                string gacPath;
                if (GacResolver.AssemblyExist(reference.AssemblyName, out gacPath))
                {
                    var publicKeyToken = GetPublicKeyTokenFromGacPath(gacPath);
                    WarnAboutIgnoredReference(reference, publicKeyToken);
                }
                else
                {
                    referenceFiles.Add(reference.AssemblyFilename);
                }
            }

            return referenceFiles;
        }

        private void WarnAboutIgnoredReference(IReference reference, string publicKeyToken)
        {
            if (!PublicKeyTokens.UsedInNetFramework.Contains(publicKeyToken))
            {
                _console.WriteWarning("Ignoring {0} because it was found in the GAC (with public key token {1})", reference.AssemblyName, publicKeyToken);
            }
        }

        private static string GetPublicKeyTokenFromGacPath(string hintPath)
        {
            var versionUnderscoreToken = Path.GetDirectoryName(hintPath);
            return versionUnderscoreToken.Split('_').Last();
        }

        public ICollection<ManifestDependency> GetManifestDependencies(ICollection<IPackage> packagesAdded)
        {
            var referencedProjectAssemblyNames = _vsProject.GetProjectReferences().Select(prf => prf.AssemblyName);
            var assemblyNames = new HashSet<string>(referencedProjectAssemblyNames);
            assemblyNames.AddRange(packagesAdded.Select(brf => brf.Id));
            return assemblyNames.Select(name => new ManifestDependency{Id = name}).ToList();
        }
    }
}