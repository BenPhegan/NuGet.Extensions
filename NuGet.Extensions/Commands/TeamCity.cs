using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Commands;
using System.ComponentModel.Composition;
using NuGet.Common;
using System.IO;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using NuGet.Extensions.TeamCity;
using NuGet.Extras.Repositories;
using QuickGraph;

namespace NuGet.Extensions.Commands
{
    [Command("teamcity", "Graphs details regarding NuGet and TeamCity", MinArgs = 0)]
    public class TeamCity : Command
    {
        private readonly List<string> _sources = new List<string>();
        public IPackageRepositoryFactory RepositoryFactory { get; set; }
        public IPackageSourceProvider SourceProvider { get; set; }
        private RepositoryAssemblyResolver _resolver;
        private IFileSystem _fileSystem;
        private AdjacencyGraph<string,Edge<string>> _adjacencyGraph;
        private Dictionary<string, BuildPackageMapping> _mappings = new Dictionary<string, BuildPackageMapping>();

        [Option("Project to confine search within")]
        public string Project { get; set; }

        [Option("A list of sources to search")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option("Target TeamCity server")]
        public string TeamCityServer { get; set; }


        [ImportingConstructor]
        public TeamCity(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            Contract.Assert(packageRepositoryFactory != null);
            Contract.Assert(sourceProvider != null);

            RepositoryFactory = packageRepositoryFactory;
            SourceProvider = sourceProvider;
        }

        public override void ExecuteCommand()
        {
            var sw = new Stopwatch();
            var api = new TeamCityApi(TeamCityServer);
            var buildConfigs = string.IsNullOrEmpty(Project)
                                   ? api.GetBuildTypes()
                                   : api.GetBuildTypes().Where(b => b.ProjectName.Equals(Project, StringComparison.InvariantCultureIgnoreCase));

            foreach (var buildConfig in buildConfigs)
            {
                var details = api.GetBuildTypeDetailsById(buildConfig.Id);
                
                //Check for nuget publish steps
                foreach (var publishStep in details.Steps.Where(s => s.Type.Equals("jb.nuget.publish")))
                {
                    AddBuildPackageMappingIfRequired(buildConfig);
                    var package = publishStep.Properties.First(p => p.Name.Equals("nuget.publish.files")).value.Replace(".nupkg", "");
                    if (!_mappings[buildConfig.Name].Publishes.Contains(package))
                        _mappings[buildConfig.Name].Publishes.Add(package);
                }

                //Check for nuget trigger steps
                foreach (var trigger in details.Triggers.Where(t => t.Type.Equals("nuget.simple")))
                {
                    var package = trigger.Properties.First(p => p.Name.Equals("nuget.package")).value;
                    AddBuildPackageMappingIfRequired(buildConfig);
                    if (!_mappings[buildConfig.Name].Subscribes.Contains(package))
                        _mappings[buildConfig.Name].Subscribes.Add(package);                    
                }

                //check artifacts..
                foreach (var artifact in api.GetArtifactListByBuildType(buildConfig.Id).Where(a => a.Ext.Equals("nupkg")))
                {
                    var package = artifact.Name;
                    AddBuildPackageMappingIfRequired(buildConfig);
                    if (!_mappings[buildConfig.Name].Publishes.Contains(package))
                        _mappings[buildConfig.Name].Publishes.Add(package);
                }
            }

            _adjacencyGraph = BuildGraph(_mappings);

            Console.WriteLine();
            sw.Stop();
            OutputElapsedTime(sw);
            Environment.Exit(0);

        }

        private AdjacencyGraph<string, Edge<string>> BuildGraph(Dictionary<string, BuildPackageMapping> mappings)
        {
            throw new NotImplementedException();
        }

        private void AddBuildPackageMappingIfRequired(BuildType buildConfig)
        {
            if (!_mappings.ContainsKey(buildConfig.Name))
                _mappings.Add(buildConfig.Name, new BuildPackageMapping() {Build = buildConfig.Name});
        }

        private void OutputElapsedTime(Stopwatch sw)
        {
            Console.WriteLine("Completed search in {0} seconds", sw.Elapsed.TotalSeconds);
        }

        private IPackageRepository GetRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;
            return repository;
        }

        protected virtual IFileSystem CreateFileSystem(string root)
        {
            return new PhysicalFileSystem(root);
        }
    }

    public class BuildPackageMapping
    {
        public string Build { get; set; }
        public List<String> Publishes { get; set; }
        public List<String> Subscribes { get; set; }
    }
    
}
