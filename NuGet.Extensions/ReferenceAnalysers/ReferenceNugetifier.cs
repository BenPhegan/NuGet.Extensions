using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using NuGet.Common;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.Repositories;

namespace NuGet.Extensions.ReferenceAnalysers
{
    public class ReferenceNugetifier
    {
        private readonly IConsole _console;
        private readonly FileInfo _projectFileInfo;
        private readonly IFileSystem _projectFileSystem;
        private readonly IVsProject _vsProject;
        private readonly IPackageRepository _packageRepository;
        private readonly Lazy<IList<IReference>> _references;
        private readonly Lazy<IList<KeyValuePair<string, List<IPackage>>>> _resolveReferenceMappings;

        public ReferenceNugetifier(IConsole console, FileInfo projectFileInfo, IFileSystem projectFileSystem, IVsProject vsProject, IPackageRepository packageRepository)
        {
            _console = console;
            _projectFileInfo = projectFileInfo;
            _projectFileSystem = projectFileSystem;
            _vsProject = vsProject;
            _packageRepository = packageRepository;
            _references = new Lazy<IList<IReference>>(() => _vsProject.GetBinaryReferences().ToList());
            _resolveReferenceMappings = new Lazy<IList<KeyValuePair<string, List<IPackage>>>>(() => ResolveReferenceMappings(_references.Value).ToList());
        }

        public void NugetifyReferencesInProject(DirectoryInfo solutionDir)
        {
            var resolvedMappings = _resolveReferenceMappings.Value;
            if (!resolvedMappings.Any()) return;
            foreach (var mapping in resolvedMappings)
            {
                var referenceMatch = _references.Value.FirstOrDefault(r => r.IsForAssembly(mapping.Key));
                if (referenceMatch != null)
                {
                    var includeName = referenceMatch.IncludeName;
                    var includeVersion = referenceMatch.IncludeVersion;
                    var package = mapping.Value.OrderBy(p => p.GetFiles().Count()).First();

                    LogHintPathRewriteMessage(package, includeName, includeVersion);

                    var fileLocation = GetFileLocationFromPackage(package, mapping.Key);
                    var newHintPathFull = Path.Combine(solutionDir.FullName, "packages", package.Id, fileLocation);
                    var newHintPathRelative = String.Format(GetRelativePath(_projectFileInfo.FullName, newHintPathFull));
                    //TODO make version available, currently only works for non versioned package directories...
                    referenceMatch.ConvertToNugetReferenceWithHintPath(newHintPathRelative);
                }
            }
            _vsProject.Save();
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

        public List<ManifestDependency> AddNugetMetadataForReferences(ISharedPackageRepository sharedPackagesRepository, List<string> projectDependencies, PackageReferenceFile packageReferenceFile, string packagesConfigFilename, bool nuspec)
        {
            var resolvedMappings = _resolveReferenceMappings.Value;
            var manifestDependencies = new List<ManifestDependency>();
            if (!resolvedMappings.Any()) return manifestDependencies;
            //Now, create the packages.config for the resolved packages, and update the repositories.config
            _console.WriteLine("Creating {0}", packagesConfigFilename);
            var packagesConfig = packageReferenceFile;
            foreach (var referenceMapping in resolvedMappings)
            {
                //TODO We shouldnt need to resolve this twice....
                var package = referenceMapping.Value.OrderBy(p => p.GetFiles().Count()).First();
                if (!packagesConfig.EntryExists(package.Id, package.Version))
                    packagesConfig.AddEntry(package.Id, package.Version);
                if (nuspec && manifestDependencies.All(m => m.Id != package.Id))
                {
                    manifestDependencies.Add(new ManifestDependency {Id = package.Id});
                }
            }

            //This is messy...refactor
            //For any resolved project dependencies, add a manifest dependency if we are doing nuspecs
            if (nuspec)
            {
                foreach (var projectDependency in projectDependencies)
                {
                    if (manifestDependencies.All(m => m.Id != projectDependency))
                    {
                        manifestDependencies.Add(new ManifestDependency {Id = projectDependency});
                    }
                }
            }
            //Register the packages.config
            var packagesConfigFilePath = Path.Combine(_projectFileInfo.Directory.FullName + "\\", packagesConfigFilename);
            sharedPackagesRepository.RegisterRepository(packagesConfigFilePath);

            _vsProject.AddPackagesConfig();

            return manifestDependencies;
        }

        private IEnumerable<KeyValuePair<string, List<IPackage>>> ResolveReferenceMappings(IEnumerable<IReference> references)
        {
            var referenceList = ProjectAdapter.GetReferencedAssemblies(references);
            if (referenceList.Any())
            {
                var referenceMappings = ResolveAssembliesToPackagesConfigFile(referenceList);
                var resolvedMappings = referenceMappings.Where(m => m.Value.Any());
                var failedMappings = referenceMappings.Where(m => m.Value.Count == 0);
                //next, lets rewrite the project file with the mappings to the new location...
                //Going to have to use the mapping to assembly name that we get back from the resolve above
                _console.WriteLine();
                _console.WriteLine("Found {0} package to assembly mappings on feed...", resolvedMappings.Count());
                failedMappings.ToList().ForEach(f => _console.WriteWarning("Could not match: {0}", f.Key));
                return resolvedMappings;
            }

            _console.WriteLine("No references found to resolve (all GAC?)");
            return Enumerable.Empty<KeyValuePair<string, List<IPackage>>>();
        }

        private string GetFileLocationFromPackage(IPackage package, string key)
        {
            return (from fileLocation in package.GetFiles()
                where fileLocation.Path.ToLowerInvariant().EndsWith(key, StringComparison.OrdinalIgnoreCase)
                select fileLocation.Path).FirstOrDefault();
        }

        private static String GetRelativePath(string root, string child)
        {
            // Validate paths.
            Contract.Assert(!String.IsNullOrEmpty(root));
            Contract.Assert(!String.IsNullOrEmpty(child));

            // Create Uris
            var rootUri = new Uri(root);
            var childUri = new Uri(child);

            // Get relative path.
            var relativeUri = rootUri.MakeRelativeUri(childUri);

            // Clean path and return.
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private Dictionary<string, List<IPackage>> ResolveAssembliesToPackagesConfigFile(List<string> referenceFiles)
        {
            var results = new Dictionary<string, List<IPackage>>();
            if (referenceFiles.Any())
            {
                _console.WriteLine("Checking feed for {0} references...", referenceFiles.Count);

                IQueryable<IPackage> packageSource = _packageRepository.GetPackages().OrderBy(p => p.Id);

                var assemblyResolver = new RepositoryAssemblyResolver(referenceFiles,
                    packageSource,
                    _projectFileSystem, _console);
                results = assemblyResolver.ResolveAssemblies(false);
                assemblyResolver.OutputPackageConfigFile();
            }
            else _console.WriteWarning("No references found to resolve (all GAC?)");
            return results;
        }
    }
}