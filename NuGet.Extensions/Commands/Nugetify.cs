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
using NuGet.Extensions.GetLatest.MSBuild;
using NuGet.Extras.Repositories;

namespace NuGet.Extensions.Commands
{
    [Command("nugetify", "Given a solution, attempts to replace all file references with package references, adding all reqhired" +
                         "packages.config files as it goes.", MinArgs = 1, MaxArgs = 1)]
    public class Nugetify : Command
    {
        private readonly List<ManifestDependency> ManifestDependencies = new List<ManifestDependency>();
        private readonly List<string> _sources = new List<string>();
        private string _author;
        private string _copyright;
        private string _description;
        private IFileSystem _fileSystem;
        private string _id;
        private string _licenseUrl;
        private string _releaseNotes;
        private Boolean _requireLicenseAcceptance;
        private RepositoryAssemblyResolver _resolver;
        private string _tags;
        private string _title;

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
        public string LicenseUrl
        {
            get { return _licenseUrl; }
            set { _licenseUrl = value; }
        }

        [Option("NuSpec Icon URL")]
        public string IconUrl { get; set; }

        [Option("NuSpec tags")]
        public string Tags
        {
            get { return _tags; }
            set { _tags = value; }
        }

        [Option("NuSpec release notes")]
        public string ReleaseNotes
        {
            get { return _releaseNotes; }
            set { _releaseNotes = value; }
        }

        [Option("NuSpec description")]
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        [Option("NuSpec ID")]
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        [Option("Create NuSpecs for solution")]
        public Boolean NuSpec { get; set; }

        [Option("NuSpec title")]
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        [Option("NuSpec Author")]
        public string Author
        {
            get { return _author; }
            set { _author = value; }
        }

        [Option("NuSpec RequireLicenseAcceptance (defaults to false)")]
        public bool RequireLicenseAcceptance
        {
            get { return _requireLicenseAcceptance; }
            set { _requireLicenseAcceptance = value; }
        }

        [Option(("NuSpec Copyright"))]
        public string Copyright
        {
            get { return _copyright; }
            set { _copyright = value; }
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
                            var assemblyOutput = project.GetPropertyValue("AssemblyName");

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
                                        var newHintPathFull = Path.Combine(solutionRoot.FullName, "packages", package.Id, fileLocation);
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
                                    if (NuSpec)
                                        ManifestDependencies.Add(new ManifestDependency {Id = package.Id});
                                }
                                sharedPackagesRepository.RegisterRepository(packagesConfigFilePath);
                            }

                            //Create nuspec regardless of whether we have added dependencies
                            if (NuSpec)
                            {
                                CreateAndOutputNuSpecFile(assemblyOutput);
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

        private void CreateAndOutputNuSpecFile(string assemblyOutput)
        {
            var manifest = new Manifest
                               {
                                   Metadata =
                                       {
                                           Dependencies = ManifestDependencies,
                                           Id = _id ?? assemblyOutput,
                                           Title = _title ?? assemblyOutput,
                                           Version = "$version$",
                                           Description = _description ?? assemblyOutput,
                                           Authors = _author ?? "$author$",
                                           Tags = _tags ?? "$tags$",
                                           LicenseUrl = _licenseUrl ?? "$licenseurl$",
                                           RequireLicenseAcceptance = _requireLicenseAcceptance,
                                           ReleaseNotes = _releaseNotes ?? "$releasenotes$",
                                           Copyright = _copyright ?? "$copyright$"
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

            try
            {
                using (var stream = new MemoryStream())
                {
                    manifest.Save(stream, validate: false);
                    stream.Seek(0, SeekOrigin.Begin);
                    var content = stream.ReadToEnd();
                    File.WriteAllText(nuspecFile, RemoveSchemaNamespace(content));
                }
            }
            catch (Exception)
            {
                Console.WriteError("Could not save file: {0}", nuspecFile);
                throw;
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

        private static string RemoveSchemaNamespace(string content)
        {
            // This seems to be the only way to clear out xml namespaces.
            return Regex.Replace(content, @"(xmlns:?[^=]*=[""][^""]*[""])", String.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }

        private Dictionary<string, List<IPackage>> ResolveAssembliesToPackagesConfigFile(FileInfo projectFileInfo, IEnumerable<ProjectItem> references)
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

            var results = new Dictionary<string, List<IPackage>>();
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