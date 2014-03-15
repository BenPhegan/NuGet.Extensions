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
    public class ProjectNugetifier
    {
        private readonly IConsole _console;
        private readonly IFileSystem _projectFileSystem;
        private readonly IVsProject _vsProject;
        private readonly IPackageRepository _packageRepository;
        private readonly Lazy<IList<IReference>> _references;
        private readonly Lazy<IList<KeyValuePair<string, List<IPackage>>>> _resolveReferenceMappings;
        private static readonly string PackageReferenceFilename = Constants.PackageReferenceFile;
        private readonly IHintPathGenerator _hintPathGenerator;

        public ProjectNugetifier(IVsProject vsProject, IPackageRepository packageRepository, IFileSystem projectFileSystem, IConsole console, IHintPathGenerator hintPathGenerator)
        {
            _console = console;
            _projectFileSystem = projectFileSystem;
            _vsProject = vsProject;
            _packageRepository = packageRepository;
            _references = new Lazy<IList<IReference>>(() => _vsProject.GetBinaryReferences().ToList());
            _resolveReferenceMappings = new Lazy<IList<KeyValuePair<string, List<IPackage>>>>(() => ResolveReferenceMappings(_references.Value).ToList());
            _hintPathGenerator = hintPathGenerator;
        }

        public void NugetifyReferences(DirectoryInfo solutionDir)
        {
            var resolvedMappings = _resolveReferenceMappings.Value;
            if (!resolvedMappings.Any()) return;
            foreach (var mapping in resolvedMappings)
            {
                var referenceMatch = _references.Value.FirstOrDefault(r => r.IsForAssembly(mapping.Key));
                if (referenceMatch != null)
                {
                    var includeName = referenceMatch.AssemblyName;
                    var includeVersion = referenceMatch.AssemblyVersion;
                    var package = mapping.Value.OrderBy(p => p.GetFiles().Count()).First();

                    LogHintPathRewriteMessage(package, includeName, includeVersion);

                    var newHintPath = _hintPathGenerator.ForAssembly(solutionDir, _vsProject.ProjectDirectory, package, mapping.Key);
                    //TODO make version available, currently only works for non versioned package directories...
                    referenceMatch.ConvertToNugetReferenceWithHintPath(newHintPath);
                }
            }
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

        public List<ManifestDependency> AddNugetReferenceMetadata(ISharedPackageRepository sharedPackagesRepository, bool nuspec)
        {
            _console.WriteLine("Checking for any project references for {0}...", PackageReferenceFilename);
            var resolvedMappings = _resolveReferenceMappings.Value;
            var manifestDependencies = new List<ManifestDependency>();
            if (!resolvedMappings.Any()) return manifestDependencies;
            var projectReferences = _vsProject.GetProjectReferences().Select(pRef => pRef.AssemblyName).ToList();
            CreatePackagesConfig(nuspec, resolvedMappings, manifestDependencies);
            RegisterPackagesConfig(sharedPackagesRepository);
            AddProjectReferenceAssemblies(nuspec, projectReferences, manifestDependencies);
            return manifestDependencies;
        }

        private static void AddProjectReferenceAssemblies(bool nuspec, List<string> projectReferences, List<ManifestDependency> manifestDependencies)
        { //This is messy...refactor
            //For any resolved project dependencies, add a manifest dependency if we are doing nuspecs
            if (nuspec)
            {
                foreach (var projectDependency in projectReferences)
                {
                    if (manifestDependencies.All(m => m.Id != projectDependency))
                    {
                        manifestDependencies.Add(new ManifestDependency {Id = projectDependency});
                    }
                }
            }
        }

        private void RegisterPackagesConfig(ISharedPackageRepository sharedPackagesRepository)
        {
            var packagesConfigFilePath = Path.Combine(_vsProject.ProjectDirectory.FullName + "\\", PackageReferenceFilename);
            sharedPackagesRepository.RegisterRepository(packagesConfigFilePath);
            _vsProject.AddFile(PackageReferenceFilename);
        }

        private void CreatePackagesConfig(bool nuspec, IList<KeyValuePair<string, List<IPackage>>> resolvedMappings, List<ManifestDependency> manifestDependencies)
        { 
            _console.WriteLine("Creating {0}", PackageReferenceFilename);
            var packagesConfig = new PackageReferenceFile(_projectFileSystem, PackageReferenceFilename);
            foreach (var referenceMapping in resolvedMappings)
            {
                //TODO We shouldnt need to resolve this twice....
                var package = referenceMapping.Value.OrderBy(p => p.GetFiles().Count()).First();
                if (!packagesConfig.EntryExists(package.Id, package.Version)) packagesConfig.AddEntry(package.Id, package.Version);
                if (nuspec && manifestDependencies.All(m => m.Id != package.Id))
                {
                    manifestDependencies.Add(new ManifestDependency {Id = package.Id});
                }
            }
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

        private static List<string> GetReferencedAssemblies(IEnumerable<IReference> references)
        {
            var referenceFiles = new List<string>();

            foreach (var reference in references)
            {
                //TODO deal with GAC assemblies that we want to replace as well....
                string hintPath;
                if (reference.TryGetHintPath(out hintPath))
                {
                    referenceFiles.Add(Path.GetFileName(hintPath));
                }
            }
            return referenceFiles;
        }
    }
}