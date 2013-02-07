using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Evaluation;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Extensions.ExtensionMethods;
using NuGet.Extensions.GetLatest.MSBuild;
using NuGet.Extensions.MSBuild;
using NuGet.Extras.Repositories;

namespace NuGet.Extensions.Commands
{
    [Command("solutionaudit","Audits a solution against its stated packages.config NuGet dependencies.")]
    public class SolutionAudit : Command
    {
        private readonly IPackageRepositoryFactory _factory;
        private readonly IPackageSourceProvider _provider;
        private readonly List<string> _sources = new List<string>();

        [Option("A list of sources to search")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }
        [ImportingConstructor]
        public SolutionAudit(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            _factory = packageRepositoryFactory;
            _provider = sourceProvider;
        }

        public override void ExecuteCommand()
        {
            if (Arguments.Count == 0 || String.IsNullOrEmpty(Arguments[0])) return;
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

            var solution = new Solution(solutionFile.FullName);
            var simpleProjectObjects = solution.Projects;

            Console.WriteLine("Processing {0} projects in solution {1}...", simpleProjectObjects.Count, solutionFile.Name);
            Console.WriteLine("Caching package information from feed...");
            var packages = GetRepository().GetPackages().Where(p => p.IsLatestVersion).OrderBy(p => p.Id).ToList().AsQueryable();

            foreach (var simpleProject in simpleProjectObjects)
            {
                var projectPath = Path.Combine(solutionRoot.FullName, simpleProject.RelativePath);
                if (!File.Exists(projectPath))
                {
                    Console.WriteWarning("Project: {0} was not found on disk", simpleProject.ProjectName);
                    continue;
                }
                var projectFileInfo = new FileInfo(projectPath);
                var projectDirectory = projectFileInfo.Directory.FullName;

                var packagesConfigFileLocation = Path.Combine(projectDirectory, "packages.config");
                if (!File.Exists(packagesConfigFileLocation))
                {
                    Console.WriteWarning("No packages.config file, skipping...");
                    continue;
                }
                var packagesConfigFile = new PackageReferenceFile(packagesConfigFileLocation);

                Console.WriteLine();
                Console.WriteLine("Processing Project: {0}", simpleProject.ProjectName);
                var project = new Project(projectPath, new Dictionary<string, string>(), null, new ProjectCollection());
                var outputFilePath = GetProjectOutputFileLocation(projectDirectory, project);
                if (!File.Exists(outputFilePath))
                {
                    Console.WriteWarning("Could not find the output file for project, please build it first : {0}", outputFilePath);
                }
                var assembly = Assembly.LoadFile(outputFilePath);
                
                //So, one is what we use for our output, and one we use for our build, and one ends up in the output directory...
                var assemblyReferences = assembly.GetReferencedAssemblies();
                var projectFileReferences = project.GetItems("Reference").GetReferencedAssembliesAsStrings();
                var filesInOutputDirectory = Directory.GetFiles(new FileInfo(outputFilePath).DirectoryName, "*.dll");
                var packageReferences = packagesConfigFile.GetPackageReferences();
                var projectReferences = project.GetProjectReferences();

                //And now, do the magix!
                var distinctAssemblyManifestReferences = assemblyReferences.Select(a => a.Name).Distinct().ToList();
                //Remove any project references, as they should be found locally and not on the feed.
                var filteredDistinctAssemblyManifestReferences = distinctAssemblyManifestReferences.Where(a => !projectReferences.Any(b => b.StartsWith(a))).ToList();
                var assemblyResolver = new RepositoryAssemblyResolver(filteredDistinctAssemblyManifestReferences.Select(a => String.Format("{0}.dll", a)).ToList(),
                                                      packages,
                                                      new PhysicalFileSystem(projectPath),
                                                      Console);
                var manifestReferencePackageMappings = assemblyResolver.ResolveAssemblies(false);
                //Get the smallest package for each key in the returned matches
                var smallestPackageSet = manifestReferencePackageMappings
                    .Where(m => m.Value.Any())
                    .Select(mapping => mapping.Value.OrderBy(p => p.GetFiles()
                        .Count())
                        .First())
                        .ToList();

                //Compare against our package set...
                var unusedReferences = new List<PackageReference>();
                foreach (var reference in packageReferences)
                {
                    if (!smallestPackageSet.Any(p => p.Id.Equals(reference.Id,StringComparison.InvariantCultureIgnoreCase)))
                        unusedReferences.Add(reference);
                }

                 unusedReferences.ForEach(a => Console.WriteLine("{0} - {1} : Appears to be unused.", a.Id, a.Version));

                Console.WriteLine("Project completed!");
            }
            Console.WriteLine("Complete!");
        }

        private string GetProjectOutputFileLocation(string projectPath, Project project)
        {
            var assemblyOutput = project.GetPropertyValue("AssemblyName");
            var outputType = project.GetPropertyValue("OutputType");
            var assemblyOutputFilename = string.Format("{0}{1}", assemblyOutput, GetAssemblyExtension(outputType));
            var outputPath = project.GetPropertyValue("OutputPath");
            var outputFilePath = Path.Combine(Path.Combine(projectPath, outputPath), assemblyOutputFilename);
            return outputFilePath;
        }

        private static string GetAssemblyExtension(string outputType)
        {
            switch (outputType)
            {
                case  "Library" :
                    return ".dll";

                case "WinExe" :
                    return ".exe";

                case "Exe" :
                    return ".exe";

                default :
                    return string.Empty;
            }
        }

        private IPackageRepository GetRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(_factory, _provider, Source);
            repository.Logger = Console;
            return repository;
        }
    }
}
