using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using NuGet.Common;
using NuGet.Extensions.Repositories;

namespace NuGet.Extensions.Commands
{
    public class ReferenceNugetifier {
        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPackageSourceProvider _sourceProvider;
        private readonly IConsole _console;
        private readonly bool _nuspec;
        private readonly IEnumerable<string> _source;

        public ReferenceNugetifier(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider packageSourceProvider, IConsole console, bool nuspec, IEnumerable<string> source)
        {
            _repositoryFactory = packageRepositoryFactory;
            _sourceProvider = packageSourceProvider;
            _console = console;
            _nuspec = nuspec;
            _source = source;
        }

        public string NugetifyReferences(Project project, DirectoryInfo solutionRoot, SharedPackageRepository sharedPackagesRepository, string projectPath, List<ManifestDependency> manifestDependencies)
        {
            var projectFileInfo = new FileInfo(projectPath);
            var assemblyOutput = project.GetPropertyValue("AssemblyName");

            var references = project.GetItems("Reference");

            var resolvedMappings = ResolveReferenceMappings(references, projectFileInfo);

            if (resolvedMappings != null && resolvedMappings.Any())
            {
                UpdateProjectFileReferenceHintPaths(solutionRoot, project, projectPath, resolvedMappings, references);
                var projectReferences = ParseProjectReferences(project, _console);
                CreateNuGetScaffolding(sharedPackagesRepository, manifestDependencies, resolvedMappings, projectFileInfo, project, projectReferences);
            }
            return assemblyOutput;
        }

        public static List<string> ParseProjectReferences(Project project, IConsole console)
        {
            console.WriteLine("Checking for any project References for packages.config...");
            var refs = new List<string>();
            var references = project.GetItems("ProjectReference");
            foreach (var reference in references)
            {
                var refProject = new Project(Path.Combine(project.DirectoryPath, reference.UnevaluatedInclude),new Dictionary<string, string>(),null,new ProjectCollection());
                refs.Add(refProject.GetPropertyValue("AssemblyName"));
            }
            return refs;
        }

        private void UpdateProjectFileReferenceHintPaths(DirectoryInfo solutionRoot, Project project, string projectPath, IEnumerable<KeyValuePair<string, List<IPackage>>> resolvedMappings, ICollection<ProjectItem> references)
        {
            foreach (var mapping in resolvedMappings)
            {
                var referenceMatch = references.FirstOrDefault(r => ResolveProjectReferenceItemByAssemblyName(r, mapping.Key));
                if (referenceMatch != null)
                {
                    var includeName = referenceMatch.EvaluatedInclude.Contains(',') ? referenceMatch.EvaluatedInclude.Split(',')[0] : referenceMatch.EvaluatedInclude;
                    var includeVersion = referenceMatch.EvaluatedInclude.Contains(',') ? referenceMatch.EvaluatedInclude.Split(',')[1].Split('=')[1] : null;
                    var package = mapping.Value.OrderBy(p => p.GetFiles().Count()).First();

                    LogHintPathRewriteMessage(package, includeName, includeVersion);

                    var fileLocation = GetFileLocationFromPackage(package, mapping.Key);
                    var newHintPathFull  = Path.Combine(solutionRoot.FullName, "packages", package.Id, fileLocation);
                    var newHintPathRelative = String.Format(GetRelativePath(projectPath, newHintPathFull));
                    //TODO make version available, currently only works for non versioned package directories...
                    referenceMatch.SetMetadataValue("HintPath", newHintPathRelative);
                }
            }
            project.Save();
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

        private void CreateNuGetScaffolding(SharedPackageRepository sharedPackagesRepository, List<ManifestDependency> manifestDependencies, IEnumerable<KeyValuePair<string, List<IPackage>>> resolvedMappings, FileInfo projectFileInfo, Project project, List<string> projectDependencies)
        {
            //Now, create the packages.config for the resolved packages, and update the repositories.config
            _console.WriteLine("Creating packages.config");
            var packagesConfigFilePath = Path.Combine(projectFileInfo.Directory.FullName + "\\", "packages.config");
            var packagesConfig = new PackageReferenceFile(packagesConfigFilePath);
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
            sharedPackagesRepository.RegisterRepository(packagesConfigFilePath);

            //Add the packages.config to the project content, otherwise later versions of the VSIX fail...
            if (!project.GetItems("None").Any(i => i.UnevaluatedInclude.Equals("packages.config")))
            {
                project.Xml.AddItemGroup().AddItem("None", "packages.config");
                project.Save();
            }
        }

        private IEnumerable<KeyValuePair<string, List<IPackage>>> ResolveReferenceMappings(ICollection<ProjectItem> references, FileInfo projectFileInfo)
        {
            var referenceList = GetReferencedAssemblies(references);
            if (referenceList.Any())
            {
                var referenceMappings = ResolveAssembliesToPackagesConfigFile(projectFileInfo, referenceList);
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

        private Dictionary<string, List<IPackage>> ResolveAssembliesToPackagesConfigFile(FileInfo projectFileInfo, List<string> referenceFiles)
        {
            var results = new Dictionary<string, List<IPackage>>();
            if (referenceFiles.Any())
            {
                _console.WriteLine("Checking feed for {0} references...", referenceFiles.Count);

                IQueryable<IPackage> packageSource = GetRepository().GetPackages().OrderBy(p => p.Id);

                var assemblyResolver = new RepositoryAssemblyResolver(referenceFiles,
                    packageSource,
                    new PhysicalFileSystem(projectFileInfo.Directory.ToString()), _console);
                results = assemblyResolver.ResolveAssemblies(false);
                assemblyResolver.OutputPackageConfigFile();
            }
            else
            {
                _console.WriteWarning("No references found to resolve (all GAC?)");
            }
            return results;
        }

        private static List<string> GetReferencedAssemblies(IEnumerable<ProjectItem> references)
        {
            var referenceFiles = new List<string>();

            foreach (ProjectItem reference in references)
            {
                //TODO deal with GAC assemblies that we want to replace as well....
                if (reference.HasMetadata("HintPath"))
                {
                    var hintPath = reference.GetMetadataValue("HintPath");
                    referenceFiles.Add(Path.GetFileName(hintPath));
                }
            }
            return referenceFiles;
        }

        private bool ResolveProjectReferenceItemByAssemblyName(ProjectItem reference, string mapping)
        {
            if (reference.HasMetadata("HintPath"))
            {
                var hintpath = reference.GetMetadataValue("HintPath");
                var fileInfo = new FileInfo(hintpath);
                return fileInfo.Name.Equals(mapping, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private IPackageRepository GetRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(_repositoryFactory, _sourceProvider, _source);
            repository.Logger = _console;
            return repository;
        }
    }
}