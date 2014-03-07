using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.ReferenceAnalysers;

namespace NuGet.Extensions.Commands
{
    [Command("nugetify", "Given a solution, attempts to replace all file references with package references, adding all required" +
                         " packages.config files as it goes.", MinArgs = 1, MaxArgs = 1)]
    public class Nugetify : Command
    {
        private readonly List<string> _sources = new List<string>();

        [ImportingConstructor]
        public Nugetify()
        {
        }

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
            if (!String.IsNullOrEmpty(Arguments[0]))
            {
                var solutionFile = new FileInfo(Arguments[0]);
                if (solutionFile.Exists && solutionFile.Extension == ".sln")
                {
                    var solutionRoot = solutionFile.Directory;
                    var sharedPackagesRepository = new SharedPackageRepository(Path.Combine(solutionRoot.FullName, "packages"));
                    var solution = new Solution(solutionFile.FullName);
                    var simpleProjectObjects = solution.Projects;

                    Console.WriteLine("Processing {0} projects in solution {1}...", simpleProjectObjects.Count, solutionFile.Name);
                    foreach (var simpleProject in simpleProjectObjects)
                    {
                        NugetifyProject(solutionFile, simpleProject, solutionRoot, sharedPackagesRepository);
                    }
                    Console.WriteLine("Complete!");
                }
                else
                {
                    Console.WriteError("Could not find solution file : {0}", solutionFile);
                }
            }
        }

        private void NugetifyProject(FileInfo solutionFile, SolutionProject simpleProject, DirectoryInfo solutionRoot, ISharedPackageRepository sharedPackagesRepository)
        {
            var projectPath = Path.Combine(solutionFile.Directory.FullName, simpleProject.RelativePath);
            if (File.Exists(projectPath))
            {
                Console.WriteLine();
                Console.WriteLine("Processing Project: {0}", simpleProject.ProjectName);
                var projectFileInfo = new FileInfo(projectPath);
                var project = new Project(projectPath, new Dictionary<string, string>(), null, new ProjectCollection());
                var projectFileSystem = new PhysicalFileSystem(projectFileInfo.Directory.ToString());
                var packagesConfigFilename = "packages.config";
                var packageReferenceFile = new PackageReferenceFile(projectFileSystem, packagesConfigFilename);
                var projectAdapter = new ProjectAdapter(project, packagesConfigFilename);
                var packageRepository = GetRepository();
                var referenceNugetifier = new ReferenceNugetifier(Console, NuSpec, projectFileInfo, solutionRoot, projectFileSystem, projectAdapter, packageReferenceFile, packageRepository, packagesConfigFilename);
                var projectReferences = ParseProjectReferences(project, Console);
                var manifestDependencies = referenceNugetifier.NugetifyReferences(sharedPackagesRepository, projectReferences);

                //Create nuspec regardless of whether we have added dependencies
                if (NuSpec) CreateAndOutputNuSpecFile(projectAdapter.AssemblyName, manifestDependencies);

                Console.WriteLine("Project completed!");
            }
            else Console.WriteWarning("Project: {0} was not found on disk", simpleProject.ProjectName);
        }

        private void CreateAndOutputNuSpecFile(string assemblyOutput, List<ManifestDependency> manifestDependencies, string targetFramework = ".NET Framework, Version=4.0")
        {
            var manifest = new Manifest
                               {
                                   Metadata =
                                       {
                                           //TODO need to revisit and get the TargetFramework from the assembly...
                                           DependencySets = new List<ManifestDependencySet>
                                                            {
                                                   new ManifestDependencySet{Dependencies = manifestDependencies,TargetFramework = targetFramework}
                                               },
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
            if (!String.IsNullOrEmpty(ReleaseNotes))
                manifest.Metadata.ReleaseNotes = ReleaseNotes;

            try
            {
                Console.WriteLine("Saving new NuSpec: {0}", nuspecFile);
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

        private static string RemoveSchemaNamespace(string content)
        {
            // This seems to be the only way to clear out xml namespaces.
            return Regex.Replace(content, @"(xmlns:?[^=]*=[""][^""]*[""])", String.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline);
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

        private IPackageRepository GetRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;
            return repository;
        }
    }
}