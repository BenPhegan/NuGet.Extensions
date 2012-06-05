using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Commands;
using System.ComponentModel.Composition;
using NuGet.Common;
using System.IO;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using NuGet.Extras.Repositories;
using RestSharp;

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
            Stopwatch sw = new Stopwatch(); 
            var client = new RestClient(TeamCityServer + "/guestAuth/app/rest/");
            var request = new RestRequest("buildTypes", Method.GET);
            request.RequestFormat = DataFormat.Xml;
            request.AddHeader("Accept", "application/xml");

            var buildConfigs = client.Execute<List<BuildType>>(request);
            Console.WriteLine(request.Resource);
            foreach (var buildConfig in buildConfigs.Data)
            {
                Console.WriteLine(buildConfig);
                var buildDetails = new RestRequest("buildTypes/id:{ID}", Method.GET);
                buildDetails.AddParameter("ID", buildConfig.Id, ParameterType.UrlSegment);
                buildDetails.RequestFormat = DataFormat.Xml;
                buildDetails.AddHeader("Accept", "application/xml");
                var response = client.Execute<BuildTypeDetails>(buildDetails);
                Console.WriteLine(response.Data);
            }

            Console.WriteLine();
            sw.Stop();
            Environment.Exit(0);

        }

        private IEnumerable<string> GetAssemblyListFromDirectory()
        {
            var assemblies = new List<string>();
            var fqfn = _fileSystem.GetFiles(Arguments[0], "*.dll");
            var filenames = fqfn.Select(f =>
            {
                var sfn = new FileInfo(f);
                return sfn.Name;
            });
            return filenames;

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

    public class BuildType
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Href { get; set; }
        public string ProjectName { get; set; }
        public string ProjectId { get; set; }
        public Uri WebUrl { get; set; }

        public override string ToString()
        {
            return string.Format("Id:{0} Name:{1} HREF:{2} ProjectName:{3} ProjectID:{4} WebUrl:{5}", Id, Name, Href, ProjectName, ProjectId, WebUrl);
        }
    }

    public class BuildTypeDetails : BuildType
    {
        public string Description { get; set; }
        public bool Paused { get; set; }
        public Project Project { get; set; }
        public List<VcsRootEntry> VcsRootEntries { get; set; }
        public Settings Settings { get; set; }
        public List<string> Builds { get; set; }
        public List<Trigger> Triggers { get; set; }
        public List<Step> Steps { get; set; }
        public List<Feature> Features { get; set; } 
    }

    public class Feature : GenericTeamCityPropertyGroup {}

    public class Step : GenericTeamCityPropertyGroup
    {
        public string Name { get; set; }
    }

    public class Trigger : GenericTeamCityPropertyGroup{}

    public class GenericTeamCityPropertyGroup
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public List<Property> Properties { get; set; } 
    }

    public class Settings
    {
        public List<Property> Properties { get; set; }
    }

    public class Property
    {
        public string Name { get; set; }
        public string value { get; set; }
    }

    public class VcsRootEntry
    {
        public string Id { get; set; }
        public string CheckoutRules { get; set; }
        public List<VcsRoot> VcsRoot { get; set; }
    }

    public class Project : GenericTeamCityStubValue {}

    public class VcsRoot : GenericTeamCityStubValue {}

    public class GenericTeamCityStubValue
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Href { get; set; }
    }

    
}
