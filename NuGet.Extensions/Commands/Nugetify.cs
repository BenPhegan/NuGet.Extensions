using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text.RegularExpressions;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.ReferenceAnalysers;

namespace NuGet.Extensions.Commands
{
    [Command("nugetify", "Given a solution, attempts to replace all file references with package references, adding all required" +
                         " packages.config files as it goes.", MinArgs = 1, MaxArgs = 1)]
    public class Nugetify : Command, INuspecDataSource
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

        [Option("NuSpec project URL")]
        public string ProjectUrl { get; set; }

        [Option("NuSpec license URL")]
        public string LicenseUrl { get; set; }

        [Option("NuSpec icon URL")]
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

        [Option("NuSpec author")]
        public string Author { get; set; }

        [Option("NuSpec require license acceptance (defaults to false)")]
        public bool RequireLicenseAcceptance { get; set; }

        [Option(("NuSpec copyright"))]
        public string Copyright { get; set; }

        [Option("NuSpec owners")]
        public string Owners { get; set; }

        public override void ExecuteCommand()
        {
            if (!String.IsNullOrEmpty(Arguments[0]))
            {
                var solutionFile = new FileInfo(Arguments[0]);
                if (solutionFile.Exists && solutionFile.Extension == ".sln")
                {
                    NugetifySolution(solutionFile);
                }
                else
                {
                    Console.WriteError("Could not find solution file : {0}", solutionFile);
                }
            }
        }

        private void NugetifySolution(FileInfo solutionFile)
        {
            Console.WriteLine("Loading projects from solution {0}", solutionFile.Name);

            var existingSolutionPackagesRepo = new SharedPackageRepository(Path.Combine(solutionFile.Directory.FullName, "packages"));
            using (var solutionAdapter = new SolutionProjectLoader(solutionFile, Console))
            {
                var projectAdapters = solutionAdapter.GetProjects();

                Console.WriteLine("Processing {0} projects...", projectAdapters.Count);
                foreach (var projectAdapter in projectAdapters)
                {
                    Console.WriteLine();
                    Console.WriteLine("Processing project: {0}", projectAdapter.ProjectName);

                    NugetifyProject(projectAdapter, solutionFile.Directory, existingSolutionPackagesRepo);

                    Console.WriteLine("Project completed!");
                }
            }
            Console.WriteLine("Complete!");
        }

        private void NugetifyProject(ProjectAdapter projectAdapter, DirectoryInfo solutionRoot, ISharedPackageRepository existingSolutionPackagesRepo)
        {
            var referenceNugetifier = CreateReferenceNugetifier(projectAdapter);
            referenceNugetifier.NugetifyReferences(solutionRoot);
            var manifestDependencies = referenceNugetifier.AddNugetReferenceMetadata(existingSolutionPackagesRepo, NuSpec);
            projectAdapter.Save();

            //Create nuspec regardless of whether we have added dependencies
            if (NuSpec)
            {
                var assemblyOutput = projectAdapter.AssemblyName;
                var manifest = CreateNuspecManifest(this, assemblyOutput, manifestDependencies);
                string destination = assemblyOutput + Constants.ManifestExtension;
                Save(Console, manifest, destination);
            }
        }

        private ProjectNugetifier CreateReferenceNugetifier(ProjectAdapter projectAdapter)
        {
            var projectFileSystem = new PhysicalFileSystem(projectAdapter.ProjectDirectory.ToString());
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;
            return new ProjectNugetifier(projectAdapter, repository, projectFileSystem, Console);
        }

        private static Manifest CreateNuspecManifest(Nugetify nuspecData, string assemblyOutput, List<ManifestDependency> manifestDependencies, string targetFramework = ".NET Framework, Version=4.0")
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
                                           Id = nuspecData.Id ?? assemblyOutput,
                                           Title = nuspecData.Title ?? assemblyOutput,
                                           Version = "$version$",
                                           Description = nuspecData.Description ?? assemblyOutput,
                                           Authors = nuspecData.Author ?? "$author$",
                                           Tags = nuspecData.Tags ?? "$tags$",
                                           LicenseUrl = nuspecData.LicenseUrl ?? "$licenseurl$",
                                           RequireLicenseAcceptance = nuspecData.RequireLicenseAcceptance,
                                           Copyright = nuspecData.Copyright ?? "$copyright$",
                                           IconUrl = nuspecData.IconUrl ?? "$iconurl$",
                                           ProjectUrl = nuspecData.ProjectUrl ?? "$projrcturl$",
                                           Owners = nuspecData.Owners ?? nuspecData.Author ?? "$author$"                                          
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

            //Dont add a releasenotes node if we dont have any to add...
            if (!String.IsNullOrEmpty(nuspecData.ReleaseNotes)) manifest.Metadata.ReleaseNotes = nuspecData.ReleaseNotes;

            return manifest;
        }

        private static void Save(IConsole console, Manifest manifest, string nuspecFile)
        {
            try
            {
                console.WriteLine("Saving new NuSpec: {0}", nuspecFile);
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
                console.WriteError("Could not save file: {0}", nuspecFile);
                throw;
            }
        }

        private static string RemoveSchemaNamespace(string content)
        {
            // This seems to be the only way to clear out xml namespaces.
            return Regex.Replace(content, @"(xmlns:?[^=]*=[""][^""]*[""])", String.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }
    }
}