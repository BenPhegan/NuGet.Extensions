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

        private readonly IPackageRepositoryFactory _repositoryFactory;
        private readonly IPackageSourceProvider _sourceProvider;

        [ImportingConstructor]
        public Nugetify(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            Contract.Assert(packageRepositoryFactory != null);
            Contract.Assert(sourceProvider != null);

            _repositoryFactory = packageRepositoryFactory;
            _sourceProvider = sourceProvider;
        }

        public override void ExecuteCommand()
        {
            if (!String.IsNullOrEmpty(Arguments[0]))
            {
                var solutionFile = new FileInfo(Arguments[0]);
                if (solutionFile.Exists && solutionFile.Extension == ".sln")
                {
                    var solutionRoot = solutionFile.Directory;
                    var solution = new Solution(solutionFile.FullName);
                    var simpleProjectObjects = solution.Projects;


                    foreach (var simpleProject in simpleProjectObjects)
                    {
                        var projectPath = Path.Combine(solutionFile.Directory.FullName, simpleProject.RelativePath);
                        if (File.Exists(projectPath))
                        {
                            var projectFileInfo = new FileInfo(projectPath);
                            var project = new Project(projectPath);
                            var references = project.GetItems("Reference");

                            //First, generate the packages.config
                            var referenceMappings = ResolveAssembliesToPackagesConfigFile(projectFileInfo, references);

                            //next, lets rewrite the project file with the mappings to the new location...
                            //Going to have to use the mapping to assembly name that we get back from the resolve above
                            foreach (var mapping in referenceMappings)
                            {

                                var referenceMatch = references.FirstOrDefault(r => ResolveProjectReferenceItemByAssemblyName(r, mapping.Key));
                                if (referenceMatch != null)
                                {
                                    //Remove the old one....
                                    //project.RemoveItem(referenceMatch);
                                    var package = mapping.Value.First();
                                    var fileLocation = GetFileLocationFromPackage(package, mapping.Key);
                                    var newHintPathFull = Path.Combine(solutionRoot.Name, "packages",package.Id, fileLocation);
                                    var newHintPath = GetRelativePath(solutionRoot.Name, newHintPathFull);
                                    referenceMatch.SetMetadataValue("HintPath", newHintPath);
                                }
                            }
                            project.Save();
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
            IQueryable<IPackage> packageSource = _sourceProvider.GetAggregate(_repositoryFactory).GetPackages();

            var assemblyResolver = new RepositoryAssemblyResolver(referenceFiles,
                                                                  packageSource,
                                                                  new PhysicalFileSystem(projectFileInfo.Directory.ToString()),
                                                                  Console);
            var results = assemblyResolver.ResolveAssemblies(false);
            assemblyResolver.OutputPackageConfigFile();

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

    }
}
