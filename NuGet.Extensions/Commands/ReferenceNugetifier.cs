using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using NuGet.Common;
using NuGet.Extensions.Repositories;

namespace NuGet.Extensions.Commands
{
    public class ReferenceNugetifier {
        public const string _packagesConfigFilename = "packages.config";
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPackageSourceProvider _sourceProvider;
        private readonly IConsole _console;
        private readonly bool _nuspec;
        private readonly IEnumerable<string> _source;
        private readonly FileInfo _projectFileInfo;
        private readonly DirectoryInfo _solutionRoot;
        private readonly IFileSystem _projectFileSystem;
        private readonly IProjectAdapter _projectAdapter;
        private readonly PackageReferenceFile _packageReferenceFile;

        public ReferenceNugetifier(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider packageSourceProvider, IConsole console, bool nuspec, IEnumerable<string> source, FileInfo projectFileInfo, DirectoryInfo solutionRoot, IFileSystem projectFileSystem, ProjectAdapter projectAdapter, PackageReferenceFile packageReferenceFile)
        {
            _repositoryFactory = packageRepositoryFactory;
            _sourceProvider = packageSourceProvider;
            _console = console;
            _nuspec = nuspec;
            _source = source;
            _projectFileInfo = projectFileInfo;
            _solutionRoot = solutionRoot;
            _projectFileSystem = projectFileSystem;
            _projectAdapter = projectAdapter;
            _packageReferenceFile = packageReferenceFile;
        }

        public string NugetifyReferences(ISharedPackageRepository sharedPackagesRepository, string projectPath, List<ManifestDependency> manifestDependencies, List<string> projectReferences)
        {
            var assemblyOutput = _projectAdapter.GetAssemblyName();

            var references = _projectAdapter.GetBinaryReferences();

            var resolvedMappings = ResolveReferenceMappings(references);

            if (resolvedMappings != null && resolvedMappings.Any())
            {
                UpdateProjectFileReferenceHintPaths(_solutionRoot, resolvedMappings, references);
                CreateNuGetScaffolding(sharedPackagesRepository, manifestDependencies, resolvedMappings, _projectFileInfo, projectReferences);
            }
            return assemblyOutput;
        }

        private void UpdateProjectFileReferenceHintPaths(DirectoryInfo solutionRoot, IEnumerable<KeyValuePair<string, List<IPackage>>> resolvedMappings, IEnumerable<BinaryReferenceAdapter> references)
        {
            foreach (var mapping in resolvedMappings)
            {
                var referenceMatch = references.FirstOrDefault(r => r.ResolveProjectReferenceItemByAssemblyName(mapping.Key));
                if (referenceMatch != null)
                {
                    var includeName = referenceMatch.GetIncludeName();
                    var includeVersion = referenceMatch.GetIncludeVersion();
                    var package = mapping.Value.OrderBy(p => p.GetFiles().Count()).First();

                    LogHintPathRewriteMessage(package, includeName, includeVersion);

                    var fileLocation = GetFileLocationFromPackage(package, mapping.Key);
                    var newHintPathFull  = Path.Combine(solutionRoot.FullName, "packages", package.Id, fileLocation);
                    var newHintPathRelative = String.Format(GetRelativePath(_projectFileInfo.FullName, newHintPathFull));
                    //TODO make version available, currently only works for non versioned package directories...
                    referenceMatch.SetHintPath(newHintPathRelative);
                }
            }
            _projectAdapter.Save();
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

        private void CreateNuGetScaffolding(ISharedPackageRepository sharedPackagesRepository, List<ManifestDependency> manifestDependencies, IEnumerable<KeyValuePair<string, List<IPackage>>> resolvedMappings, FileInfo projectFileInfo, List<string> projectDependencies)
        {
            //Now, create the packages.config for the resolved packages, and update the repositories.config
            _console.WriteLine("Creating packages.config");
            var packagesConfig = _packageReferenceFile;
            foreach (var referenceMapping in resolvedMappings)
            {
                //TODO We shouldnt need to resolve this twice....
                var package = referenceMapping.Value.OrderBy(p => p.GetFiles().Count()).First();
                if (!packagesConfig.EntryExists(package.Id, package.Version))
                    packagesConfig.AddEntry(package.Id, package.Version);
                if (_nuspec && manifestDependencies.All(m => m.Id != package.Id))
                {
                    manifestDependencies.Add(new ManifestDependency {Id = package.Id});
                }
            }

            //This is messy...refactor
            //For any resolved project dependencies, add a manifest dependency if we are doing nuspecs
            if (_nuspec)
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
            var packagesConfigFilePath = Path.Combine(projectFileInfo.Directory.FullName + "\\", _packagesConfigFilename);
            sharedPackagesRepository.RegisterRepository(packagesConfigFilePath);

            _projectAdapter.AddPackagesConfig();
        }

        private IEnumerable<KeyValuePair<string, List<IPackage>>> ResolveReferenceMappings(IEnumerable<BinaryReferenceAdapter> references)
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
            return null;
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

                IQueryable<IPackage> packageSource = GetRepository().GetPackages().OrderBy(p => p.Id);

                var assemblyResolver = new RepositoryAssemblyResolver(referenceFiles,
                    packageSource,
                    _projectFileSystem, _console);
                results = assemblyResolver.ResolveAssemblies(false);
                assemblyResolver.OutputPackageConfigFile();
            }
            else
            {
                _console.WriteWarning("No references found to resolve (all GAC?)");
            }
            return results;
        }

        private IPackageRepository GetRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(_repositoryFactory, _sourceProvider, _source);
            repository.Logger = _console;
            return repository;
        }
    }
}