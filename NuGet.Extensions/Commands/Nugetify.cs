using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Extensions.MSBuild;
using NuGet.Extensions.Nuspec;
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

        [Option("Comma separated list of key=value pairs of parameters to be used when loading projects")]
        public string MsBuildProperties { get; set; }

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
            using (var solutionAdapter = new SolutionProjectLoader(solutionFile, Console, ParsedBuildProperties))
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

        public IDictionary<string, string> ParsedBuildProperties
        {
            get
            {
                if (MsBuildProperties == null) return new Dictionary<string, string>();
                var keyValuePairs = MsBuildProperties.Split(',');
                var twoElementArrays = keyValuePairs.Select(kvp => kvp.Split('=')).ToList();
                foreach (var errorKvp  in twoElementArrays.Where(a => a.Length != 2)) throw new ArgumentException(string.Format("Key value pair near {0} is formatted incorrectly", string.Join(",", errorKvp[0])));
                return twoElementArrays.ToDictionary(kvp => kvp[0].Trim(), kvp => kvp[1].Trim());
            }
        }

        private void NugetifyProject(IVsProject projectAdapter, DirectoryInfo solutionRoot, ISharedPackageRepository existingSolutionPackagesRepo)
        {
            var projectNugetifier = CreateProjectNugetifier(projectAdapter);
            projectNugetifier.NugetifyReferences(solutionRoot);
            var manifestDependencies = projectNugetifier.AddNugetReferenceMetadata(existingSolutionPackagesRepo, NuSpec);
            projectAdapter.Save();

            //Create nuspec regardless of whether we have added dependencies
            if (NuSpec)
            {
                var nuspecBuilder = new NuspecBuilder(projectAdapter.AssemblyName);
                nuspecBuilder.SetMetadata(this, manifestDependencies);
                nuspecBuilder.SetDependencies(manifestDependencies);
                nuspecBuilder.Save(Console);
            }
        }

        private ProjectNugetifier CreateProjectNugetifier(IVsProject projectAdapter)
        {
            var projectFileSystem = new PhysicalFileSystem(projectAdapter.ProjectDirectory.ToString());
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;
            return new ProjectNugetifier(projectAdapter, repository, projectFileSystem, Console);
        }
    }
}