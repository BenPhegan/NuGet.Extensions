using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Extensions.TeamCity;

namespace NuGet.Extensions.Commands
{
    [Command("triggercheck", "Provides the ability to check TeamCity package triggers against a feed.", MinArgs = 0)]
    public class TriggerCheck : Command
    {
        private readonly List<BuildPackageMapping> _mappings = new List<BuildPackageMapping>();
        private readonly List<string> _sources = new List<string>();

        [Option("A list of sources to search")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option("Project to confine search within", AltName = "p")]
        public string Project { get; set; }

        [Option("Target TeamCity server", AltName = "t")]
        public string TeamCityServer { get; set; }

        [Option("Constrain to a single target feed.", AltName = "f")]
        public string Feed { get; set; }

        [ImportingConstructor]
        public TriggerCheck(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            RepositoryFactory = packageRepositoryFactory;
            SourceProvider = sourceProvider;
        }

        protected IPackageSourceProvider SourceProvider{ get; set; }

        protected IPackageRepositoryFactory RepositoryFactory{get; set; }
        
        public override void ExecuteCommand()
        {
            //HACK Must be a better way to do this??
            if (string.IsNullOrEmpty(TeamCityServer))
            {
                HelpCommand.Arguments.Add("triggercheck");
                HelpCommand.ExecuteCommand();
                return;
            }

            //Is there a simpler way?
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory,SourceProvider, Source);

            var sw = new Stopwatch();
            sw.Start();
            Console.WriteLine("Attempting to check triggers for TeamCity server: {0}", TeamCityServer);
            var api = new TeamCityApi(TeamCityServer);
            var buildConfigs = string.IsNullOrEmpty(Project)
                                   ? api.GetBuildTypes().ToList()
                                   : api.GetBuildTypes().Where(b => b.ProjectName.Equals(Project, StringComparison.InvariantCultureIgnoreCase)).ToList();

            Console.WriteLine("Processing {0} build configurations...", buildConfigs.Count());
            foreach (var buildConfig in buildConfigs)
            {
                var details = api.GetBuildTypeDetailsById(buildConfig.Id);
                AddSubscribeDataFromTriggers(buildConfig, details);
            }
            var failures = new Dictionary<string, List<string>>(); 
            foreach (var mapping in _mappings)
            {
                foreach (var trigger in mapping.Subscribes)
                {
                    var package = repository.FindPackagesById(trigger).FirstOrDefault();
                    if (package != null) continue;
                    //Fail
                    if (!failures.ContainsKey(mapping.Build))
                        failures.Add(mapping.Build, new List<string>());
                    failures[mapping.Build].Add(trigger);
                }
            }

            Console.WriteLine("BuildType,Trigger");
            foreach (var failure in failures)
            {
                foreach (var package in failure.Value)
                    Console.WriteLine("{0},{1}", failure.Key, package);
            }

            Console.WriteLine();
            sw.Stop();
            OutputElapsedTime(sw);
            Environment.Exit(0);
        }

        private void AddSubscribeDataFromTriggers(BuildType buildConfig, BuildTypeDetails details)
        {
            //Check for nuget trigger steps
            var triggers = details.Triggers.Where(t => t.Type.Equals("nuget.simple"));

            //Constrain by feed if required
            if (!string.IsNullOrEmpty(Feed))
                triggers = triggers.Where(t =>
                {
                    var prop = t.Properties.FirstOrDefault(p => p.Name.Equals("nuget.source"));
                    if (prop != null && prop.value.Equals(Feed))
                        return true;
                    return false;
                });

            foreach (var trigger in triggers)
            {
                var package = trigger.Properties.First(p => p.Name.Equals("nuget.package")).value;
                AddBuildPackageMappingIfRequired(buildConfig);
                AddSubscription(buildConfig, package);
            }
        }

        private void AddSubscription(BuildType buildConfig, string package)
        {
            var buildPackageMapping = GetFirstOrDefaultMapping(buildConfig.Name);
            if (buildPackageMapping != null && !buildPackageMapping.Subscribes.Contains(package))
                buildPackageMapping.Subscribes.Add(package);
        }

        private void AddBuildPackageMappingIfRequired(BuildType buildConfig)
        {
            if (GetFirstOrDefaultMapping(buildConfig.Name) == null)
                _mappings.Add(new BuildPackageMapping()
                {
                    Build = buildConfig.Name,
                    Publishes = new List<string>(),
                    Subscribes = new List<string>()
                });
        }

        private BuildPackageMapping GetFirstOrDefaultMapping(string buildConfigName)
        {
            return _mappings.FirstOrDefault(m => m.Build.Equals(buildConfigName,StringComparison.InvariantCultureIgnoreCase));
        }

        private void OutputElapsedTime(Stopwatch sw)
        {
            Console.WriteLine("Completed check in {0} seconds", sw.Elapsed.TotalSeconds);
        }

        private class BuildPackageMapping
        {
            public string Build { get; set; }
            public List<String> Publishes { get; set; }
            public List<String> Subscribes { get; set; }
        }
    }

}
