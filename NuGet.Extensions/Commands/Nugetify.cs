using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
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

        [Option("Comma separated list of key=value pairs of parameters to be used when loading projects, note that SolutionDir is automatically set.")]
        public string MsBuildProperties { get; set; }

        [Option("Format as in the packages.config file: {framework}{version}-{profile}")]
        public string TargetFramework { get; set; }

        [Option("Use hint paths that don't contain the package version")]
        public bool ExcludeVersion { get; set; }

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

        [Option("Create a NuSpec for each processed project with nuspec dependencies on any package dependencies that were added")]
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
            using (var solutionAdapter = new CachingSolutionLoader(solutionFile, GetMsBuildProperties(solutionFile), Console))
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

        private void NugetifyProject(IVsProject projectAdapter, DirectoryInfo solutionRoot, ISharedPackageRepository existingSolutionPackagesRepo)
        {
            var targetFramework = TargetFramework != null ? new FrameworkName(TargetFramework) : null;
            var projectNugetifier = CreateProjectNugetifier(projectAdapter);
            var packagesAdded = projectNugetifier.NugetifyReferences(solutionRoot);
            projectNugetifier.AddNugetReferenceMetadata(existingSolutionPackagesRepo, packagesAdded, targetFramework);
            projectAdapter.Save();

            if (NuSpec)
            {
                var manifestDependencies = projectNugetifier.GetManifestDependencies(packagesAdded);
                var nuspecBuilder = new NuspecBuilder(projectAdapter.AssemblyName);
                nuspecBuilder.SetMetadata(this, manifestDependencies);
                nuspecBuilder.SetDependencies(manifestDependencies, TargetFramework);
                nuspecBuilder.Save(Console);
            }
        }

        private ProjectNugetifier CreateProjectNugetifier(IVsProject projectAdapter)
        {
            var projectFileSystem = new PhysicalFileSystem(projectAdapter.ProjectDirectory.ToString());
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;
            var hintPathGenerator = new HintPathGenerator(!ExcludeVersion);
            return new ProjectNugetifier(projectAdapter, repository, projectFileSystem, Console, hintPathGenerator);
        }

        private IDictionary<string, string> GetMsBuildProperties(FileInfo solutionFile)
        {
            var buildProperties = GetParsedBuildProperties();
            buildProperties["SolutionDir"] = solutionFile.Directory.FullName;
            return buildProperties;
        }

        private IDictionary<string, string> GetParsedBuildProperties()
        {
            if (MsBuildProperties == null) return new Dictionary<string, string>();
            var keyValuePairs = MsBuildProperties.Split(',');
            var twoElementArrays = keyValuePairs.Select(kvp => kvp.Split('=')).ToList();
            foreach (var errorKvp in twoElementArrays.Where(a => a.Length != 2)) throw new ArgumentException(string.Format("Key value pair near {0} is formatted incorrectly", string.Join(",", errorKvp[0])));
            return twoElementArrays.ToDictionary(kvp => kvp[0].Trim(), kvp => kvp[1].Trim());
        }
    }
}