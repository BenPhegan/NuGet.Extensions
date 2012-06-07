using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;

namespace NuGet.Extensions.TeamCity
{
    class TeamCityApi
    {
        private string _teamCityServer;
        private RestClient _client;

        public TeamCityApi(string server)
        {
            _teamCityServer = server;
            _client = new RestClient(_teamCityServer + "/guestAuth/");

        }
        public List<BuildType> GetBuildTypes()
        {
            var request = new RestRequest("app/rest/buildTypes", Method.GET) { RequestFormat = DataFormat.Xml };
            request.AddHeader("Accept", "application/xml");

            var buildConfigs = _client.Execute<List<BuildType>>(request);
            return buildConfigs.Data;
        }

        public BuildTypeDetails GetBuildTypeDetailsById(string id)
        {
            var buildDetails = new RestRequest("app/rest/buildTypes/id:{ID}", Method.GET);
            buildDetails.AddParameter("ID", id, ParameterType.UrlSegment);
            buildDetails.RequestFormat = DataFormat.Xml;
            buildDetails.AddHeader("Accept", "application/xml");
            var response = _client.Execute<BuildTypeDetails>(buildDetails);
            return response.Data;
        }

        public IEnumerable<Artifact> GetArtifactListByBuildType(string buildType)
        {
            var request = new RestRequest("repository/download/{ID}/lastSuccessful/teamcity-ivy.xml");
            request.AddParameter("ID", buildType, ParameterType.UrlSegment);
            request.RequestFormat = DataFormat.Xml;
            request.AddHeader("Accept", "application/xml");
            var response = _client.Execute<IvyModule>(request);
            return response.Data.Publications;
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

    public class Feature : GenericTeamCityPropertyGroup { }

    public class Step : GenericTeamCityPropertyGroup
    {
        public string Name { get; set; }
    }

    public class Trigger : GenericTeamCityPropertyGroup { }

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

    public class Project : GenericTeamCityStubValue { }

    public class VcsRoot : GenericTeamCityStubValue { }

    public class GenericTeamCityStubValue
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Href { get; set; }
    }

    public enum BuildStepType
    {
        SimpleRunner,
        VSSolution,
        NUnit,
        NuGetPublish
    }

    public class IvyModule
    {
        public List<Artifact> Publications { get; set; }
    }

    public class Artifact
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Ext { get; set; }
    }
}
