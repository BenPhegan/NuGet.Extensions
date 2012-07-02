using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using NuGet.Commands;
using NuGet.Extensions.GetLatest.MSBuild;
using NuGet.Extras.Repositories;
using NuGet.Common;

namespace NuGet.Extensions.Commands
{
        [Command("nugetify", "Given a solution, attempts to replace all file references with package references, adding all reqhired" +
                "packages.config files as it goes.", MinArgs = 1, MaxArgs = 1)]
    public class Nugetify : Command
    {
        private readonly List<string> _sources = new List<string>();
        public IPackageRepositoryFactory RepositoryFactory { get; set; }
        public IPackageSourceProvider SourceProvider { get; set; }
        private RepositoryAssemblyResolver _resolver;
        private IFileSystem _fileSystem;

        [Option("A list of sources to search")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [ImportingConstructor]
        public Nugetify(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            Contract.Assert(packageRepositoryFactory != null);
            Contract.Assert(sourceProvider != null);

            RepositoryFactory = packageRepositoryFactory;
            SourceProvider = sourceProvider;
        }

        public override void ExecuteCommand()
        {
            if (!String.IsNullOrEmpty(Arguments[0]))
            {
                var solutionFile = new FileInfo(Arguments[0]);
                if (solutionFile.Exists && solutionFile.Extension == ".sln")
                {
                    var solutionRoot = solutionFile.Directory;
                    var packagesRoot = Path.Combine(solutionRoot.FullName, "packages");
                    var sharedPackagesRepository = new SharedPackageRepository(packagesRoot);
                    var solution = new Solution(solutionFile.FullName);
                    var simpleProjectObjects = solution.Projects;

                    Console.WriteLine("Processing {0} Projects...", simpleProjectObjects.Count);
                    foreach (var simpleProject in simpleProjectObjects)
                    {
                        var projectPath = Path.Combine(solutionFile.Directory.FullName, simpleProject.RelativePath);
                        if (File.Exists(projectPath))
                        {
                            Console.WriteLine("Processing Project: {0}", simpleProject.ProjectName);
                            var projectFileInfo = new FileInfo(projectPath);
                            var project = new Project(projectPath);
                            var references = project.GetItems("Reference");

                            var referenceMappings = ResolveAssembliesToPackagesConfigFile(projectFileInfo, references);
                            var resolvedMappings = referenceMappings.Where(m => m.Value.Any());
                            var failedMappings = referenceMappings.Where(m => m.Value.Count == 0);
                            //next, lets rewrite the project file with the mappings to the new location...
                            //Going to have to use the mapping to assembly name that we get back from the resolve above
                            Console.WriteLine();
                            Console.WriteLine("Found {0} package to assembly mappings on feed...", resolvedMappings.Count());
                            failedMappings.ToList().ForEach(f => Console.WriteWarning("Could not match: {0}", f.Key));

                            if (resolvedMappings.Any())
                            {
                                foreach (var mapping in resolvedMappings)
                                {
                                    var referenceMatch = references.FirstOrDefault(r => ResolveProjectReferenceItemByAssemblyName(r, mapping.Key));
                                    if (referenceMatch != null)
                                    {
                                        var includeName = referenceMatch.EvaluatedInclude.Contains(',') ? referenceMatch.EvaluatedInclude.Split(',')[0] : referenceMatch.EvaluatedInclude;
                                        Console.WriteLine("Attempting to update hintpaths for \"{0}\" using package \"{1}\"", includeName, mapping.Value.First().Id);
                                        //Remove the old one....
                                        //project.RemoveItem(referenceMatch);
                                        var package = mapping.Value.OrderBy(p => p.GetFiles().Count()).First();
                                        var fileLocation = GetFileLocationFromPackage(package, mapping.Key);
                                        var newHintPathFull = Path.Combine(solutionRoot.FullName,"packages", package.Id, fileLocation);
                                        var newHintPathRelative = String.Format(GetRelativePath(projectPath, newHintPathFull));
                                        //TODO make version available, currently only works for non versioned package directories...
                                        referenceMatch.SetMetadataValue("HintPath", newHintPathRelative);
                                    }
                                }
                                project.Save();

                                //Now, create the packages.config for the resolved packages, and update the repositories.config
                                Console.WriteLine("Creating packages.config");
                                var packagesConfigFilePath = Path.Combine(projectFileInfo.Directory.FullName + "\\", "packages.config");
                                var packagesConfig = new PackageReferenceFile(packagesConfigFilePath);
                                foreach (var referenceMapping in resolvedMappings)
                                {
                                    //TODO We shouldnt need to resolve this twice....
                                    var package = referenceMapping.Value.OrderBy(p => p.GetFiles().Count()).First();
                                    packagesConfig.AddEntry(package.Id, package.Version);
                                }
                                sharedPackagesRepository.RegisterRepository(packagesConfigFilePath);
                            }

                        }
                        else
                        {
                            Console.WriteWarning("Project: {0} was not found on disk", simpleProject.ProjectName);
                        }
                    }
                }
            }
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

        private Dictionary<string, List<IPackage>> ResolveAssembliesToPackagesConfigFile(FileInfo projectFileInfo, IEnumerable<ProjectItem> references)
        {
            var referenceFiles = new List<string>();

            foreach (var reference in references)
            {
                //TODO deal with GAC assemblies that we want to replace as well....
                if (reference.HasMetadata("HintPath"))
                {
                    var hintPath = reference.GetMetadataValue("HintPath");
                    referenceFiles.Add(Path.GetFileName(hintPath));
                }
            }

            Dictionary<string, List<IPackage>> results = new Dictionary<string, List<IPackage>>();
            if (referenceFiles.Any())
            {
                Console.WriteLine("Checking feed for {0} references...", referenceFiles.Count);

                IQueryable<IPackage> packageSource = GetRepository().GetPackages().OrderBy(p => p.Id);

                var assemblyResolver = new RepositoryAssemblyResolver(referenceFiles,
                                                                      packageSource,
                                                                      new PhysicalFileSystem(projectFileInfo.Directory.ToString()),
                                                                      Console);
                results = assemblyResolver.ResolveAssemblies(false);
                assemblyResolver.OutputPackageConfigFile();
            }
            else
            {
                Console.WriteLine("No references found to resolve (all GAC?)");
            }
            return results;
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
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;
            return repository;
        }

    }
}
