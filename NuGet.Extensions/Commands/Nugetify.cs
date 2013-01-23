using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Extensions.ExtensionMethods;
using NuGet.Extensions.GetLatest.MSBuild;
using NuGet.Extras.Repositories;

namespace NuGet.Extensions.Commands
{
    [Command("nugetify", "Given a solution, attempts to replace all file references with package references, adding all required" +
                         " packages.config files as it goes.", MinArgs = 1, MaxArgs = 1)]
    public class Nugetify : Command
    {
        private readonly List<string> _sources = new List<string>();

        [ImportingConstructor]
        public Nugetify(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            Contract.Assert(packageRepositoryFactory != null);
            Contract.Assert(sourceProvider != null);

            RepositoryFactory = packageRepositoryFactory;
            SourceProvider = sourceProvider;
        }

        public IPackageRepositoryFactory RepositoryFactory { get; set; }
        public IPackageSourceProvider SourceProvider { get; set; }


        [Option("A list of sources to search")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option("NuSpec Project URL")]
        public string ProjectUrl { get; set; }

        [Option("NuSpec LicenseUrl")]
        public string LicenseUrl { get; set; }

        [Option("NuSpec Icon URL")]
        public string IconUrl { get; set; }

        [Option("NuSpec tags")]
        public string Tags { get; set; }

        [Option("NuSpec release notes")]
        public string ReleaseNotes { get; set; }

        [Option("NuSpec description")]
        public string Description { get; set; }

        [Option("NuSpec ID")]
        public string Id { get; set; }

        [Option("Create NuSpecs for solution")]
        public Boolean NuSpec { get; set; }

        [Option("NuSpec title")]
        public string Title { get; set; }

        [Option("NuSpec Author")]
        public string Author { get; set; }

        [Option("NuSpec RequireLicenseAcceptance (defaults to false)")]
        public bool RequireLicenseAcceptance { get; set; }

        [Option(("NuSpec Copyright"))]
        public string Copyright { get; set; }

        [Option("NuSpec Owners")]
        public string Owners { get; set; }

        public override void ExecuteCommand()
        {
            if (String.IsNullOrEmpty(Arguments[0])) return;

            var solutionFile = new FileInfo(Arguments[0]);
            if (!solutionFile.Exists || solutionFile.Extension != ".sln")
            {
                Console.WriteError("Could not find solution file : {0}", solutionFile);
                return;
            }

            var solutionRoot = solutionFile.Directory;
            if (solutionRoot == null)
            {
                Console.WriteError("Could not find solution file directory root : {0}", solutionFile);
                return;
            }

            var sharedPackagesRepository = new SharedPackageRepository(Path.Combine(solutionRoot.FullName, "packages"));
            var solution = new Solution(solutionFile.FullName);
            var simpleProjectObjects = solution.Projects;

            Console.WriteLine("Processing {0} projects in solution {1}...", simpleProjectObjects.Count, solutionFile.Name);
            foreach (var simpleProject in simpleProjectObjects)
            {
                var manifestDependencies = new List<ManifestDependency>();
                var projectPath = Path.Combine(solutionRoot.FullName, simpleProject.RelativePath);
                if (File.Exists(projectPath))
                {
                    Console.WriteLine();
                    Console.WriteLine("Processing Project: {0}", simpleProject.ProjectName);
                    var projectFileInfo = new FileInfo(projectPath);
                    var project = new Project(projectPath, new Dictionary<string, string>(), null, new ProjectCollection());
                    var assemblyOutput = project.GetPropertyValue("AssemblyName");

                    var references = project.GetItems("Reference");

                    IQueryable<IPackage> packageSource = GetRepository().GetPackages().OrderBy(p => p.Id);
                    var referenceList = references.GetReferencedAssembliesAsStrings();
                    if (referenceList.Any())
                    {
                        var resolvedAssemblies = ResolveAssembliesToPackagesConfigFile(projectFileInfo.Directory.ToString(), referenceList, packageSource, Console);
                        var resolvedMappings = ResolveReferenceMappings(resolvedAssemblies, Console);

                        if (resolvedMappings != null && resolvedMappings.Any())
                        {
                            UpdateProjectFileReferenceHintPaths(solutionRoot, project, projectPath, resolvedMappings, references);
                            Console.WriteLine("Checking for any project References for packages.config...");
                            var projectReferences = project.GetProjectReferences();
                            CreateNuGetScaffolding(sharedPackagesRepository, manifestDependencies, resolvedMappings, projectFileInfo, project, projectReferences);
                        }

                        //Create nuspec regardless of whether we have added dependencies
                        if (NuSpec)
                        {
                            CreateAndOutputNuSpecFile(assemblyOutput, manifestDependencies);
                        }
                    }
                    Console.WriteLine("Project completed!");
                }
                else
                {
                    Console.WriteWarning("Project: {0} was not found on disk", simpleProject.ProjectName);
                }
            }
            Console.WriteLine("Complete!");
        }

        private void UpdateProjectFileReferenceHintPaths(DirectoryInfo solutionRoot, Project project, string projectPath, IEnumerable<KeyValuePair<string, List<IPackage>>> resolvedMappings, ICollection<ProjectItem> references)
        {
            foreach (var mapping in resolvedMappings)
            {
                var referenceMatch = references.FirstOrDefault(r => r.HintPathFileNameMatches(mapping.Key));
                if (referenceMatch != null)
                {
                    var includeName = referenceMatch.EvaluatedInclude.Contains(',') ? referenceMatch.EvaluatedInclude.Split(',')[0] : referenceMatch.EvaluatedInclude;
                    var includeVersion = referenceMatch.EvaluatedInclude.Contains(',') ? referenceMatch.EvaluatedInclude.Split(',')[1].Split('=')[1] : null;
                    var package = mapping.Value.OrderBy(p => p.GetFiles().Count()).First();

                    LogHintPathRewriteMessage(package, includeName, includeVersion);

                    var fileLocation = package.GetFileLocationFromPackage(mapping.Key);
                    var newHintPathFull  = Path.Combine(solutionRoot.FullName, "packages", package.Id, fileLocation);
                    var newHintPathRelative = String.Format(projectPath.GetRelativePath(newHintPathFull));
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
                if (!string.IsNullOrEmpty(includeVersion) && package.Version.Version != SemanticVersion.Parse(includeVersion).Version)
                    Console.WriteWarning(message);
                else
                    Console.WriteLine(message);
            }
            else
                Console.WriteWarning(message);
        }

        private void CreateNuGetScaffolding(SharedPackageRepository sharedPackagesRepository, ICollection<ManifestDependency> manifestDependencies, IEnumerable<KeyValuePair<string, List<IPackage>>> resolvedMappings, FileInfo projectFileInfo, Project project, List<string> projectDependencies)
        {
            //Now, create the packages.config for the resolved packages, and update the repositories.config
            Console.WriteLine("Creating packages.config");
            var packagesConfigFilePath = Path.Combine(projectFileInfo.Directory.FullName + "\\", "packages.config");
            var packagesConfig = new PackageReferenceFile(packagesConfigFilePath);
            foreach (var referenceMapping in resolvedMappings)
            {
                //TODO We shouldnt need to resolve this twice....
                var package = referenceMapping.Value.OrderBy(p => p.GetFiles().Count()).First();
                if (!packagesConfig.EntryExists(package.Id, package.Version))
                    packagesConfig.AddEntry(package.Id, package.Version);
                if (NuSpec && manifestDependencies.All(m => m.Id != package.Id))
                {
                    manifestDependencies.Add(new ManifestDependency {Id = package.Id});
                }
            }

            //This is messy...refactor
            //For any resolved project dependencies, add a manifest dependency if we are doing nuspecs
            if (NuSpec)
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

        private static IEnumerable<KeyValuePair<string, List<IPackage>>> ResolveReferenceMappings(Dictionary<string, List<IPackage>> referenceMappings, IConsole console)
        {
            var resolvedMappings = referenceMappings.Where(m => m.Value.Any());
            var failedMappings = referenceMappings.Where(m => m.Value.Count == 0);
            console.WriteLine();
            console.WriteLine("Found {0} package to assembly mappings on feed...", resolvedMappings.Count());
            failedMappings.ToList().ForEach(f => console.WriteWarning("Could not match: {0}", f.Key));
            return resolvedMappings;
        }

        private void CreateAndOutputNuSpecFile(string assemblyOutput, List<ManifestDependency> manifestDependencies)
        {
            var manifest = new Manifest
                               {
                                   Metadata =
                                       {
                                           Dependencies = manifestDependencies,
                                           Id = Id ?? assemblyOutput,
                                           Title = Title ?? assemblyOutput,
                                           Version = "$version$",
                                           Description = Description ?? assemblyOutput,
                                           Authors = Author ?? "$author$",
                                           Tags = Tags ?? "$tags$",
                                           LicenseUrl = LicenseUrl ?? "$licenseurl$",
                                           RequireLicenseAcceptance = RequireLicenseAcceptance,
                                           Copyright = Copyright ?? "$copyright$",
                                           IconUrl = IconUrl ?? "$iconurl$",
                                           ProjectUrl = ProjectUrl ?? "$projrcturl$",
                                           Owners = Owners ?? Author ?? "$author$"                                          
                                       },
                                   Files = new List<ManifestFile>
                                               {
                                                   new ManifestFile
                                                       {
                                                           Source = assemblyOutput + ".dll",
                                                           Target = "lib"
                                                       }
                                               }
                               };

            string nuspecFile = assemblyOutput + Constants.ManifestExtension;
            
            //Dont add a releasenotes node if we dont have any to add...
            if (!string.IsNullOrEmpty(ReleaseNotes))
                manifest.Metadata.ReleaseNotes = ReleaseNotes;

            try
            {
                Console.WriteLine("Saving new NuSpec: {0}", nuspecFile);
                using (var stream = new MemoryStream())
                {
                    manifest.Save(stream, validate: false);
                    stream.Seek(0, SeekOrigin.Begin);
                    var content = stream.ReadToEnd();
                    File.WriteAllText(nuspecFile, content.RemoveSchemaNamespace());
                }
            }
            catch (Exception)
            {
                Console.WriteError("Could not save file: {0}", nuspecFile);
                throw;
            }
        }

        private static Dictionary<string, List<IPackage>> ResolveAssembliesToPackagesConfigFile(string fileSystemRoot, List<string> referenceFiles, IQueryable<IPackage> packageSource, IConsole console)
        {
            var results = new Dictionary<string, List<IPackage>>();
            if (referenceFiles.Any())
            {
                console.WriteLine("Checking feed for {0} references...", referenceFiles.Count);

                var assemblyResolver = new RepositoryAssemblyResolver(referenceFiles,
                                                                      packageSource,
                                                                      new PhysicalFileSystem(fileSystemRoot),
                                                                      console);
                results = assemblyResolver.ResolveAssemblies(false);
                assemblyResolver.OutputPackageConfigFile();
            }
            else
            {
                console.WriteWarning("No references found to resolve (all GAC?)");
            }
            return results;
        }

        private IPackageRepository GetRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;
            return repository;
        }
    }
}