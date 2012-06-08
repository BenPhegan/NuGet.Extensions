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
using QuickGraph.Serialization;
using QuickGraph.Serialization.DirectedGraphML;
using QuickGraph.Algorithms;
using System.Text.RegularExpressions;

namespace NuGet.Extensions.Commands
{
    [Command("teamcity", "Graphs details regarding NuGet and TeamCity", MinArgs = 0)]
    public class TeamCity : Command
    {
        private AdjacencyGraph<string, TaggedEquatableEdge<string, string>> _simpleGraph = new AdjacencyGraph<string, TaggedEquatableEdge<string, string>>();
        private AdjacencyGraph<VertexBase, EquatableEdge<VertexBase>> _fancyGraph = new AdjacencyGraph<VertexBase, EquatableEdge<VertexBase>>();
        private Dictionary<string, BuildPackageMapping> _mappings = new Dictionary<string, BuildPackageMapping>();
        private string _outputFilename = "TeamCityGraph.dgml";

        [Option("Project to confine search within")]
        public string Project { get; set; }

        [Option("Target TeamCity server")]
        public string TeamCityServer { get; set; }

        [Option("Constrain to a single target feed.")]
        public string Feed { get; set; }

        [Option("Outputs a Package as a node, rather than just as a label on an edge.")]
        public Boolean PackageAsVertex { get; set; }

        [Option("Don't use the presence of a package in the artifacts as evidence of a publish")]
        public Boolean NoArtifact { get; set; }

        [Option("Filename to output")]
        public string Output 
        { 
            get { return _outputFilename; }
            set { _outputFilename = value; } 
        }

        [ImportingConstructor]
        public TeamCity()
        {}

        public override void ExecuteCommand()
        {
            var sw = new Stopwatch();
            sw.Start();
            var api = new TeamCityApi(TeamCityServer);
            var buildConfigs = string.IsNullOrEmpty(Project)
                                   ? api.GetBuildTypes()
                                   : api.GetBuildTypes().Where(b => b.ProjectName.Equals(Project, StringComparison.InvariantCultureIgnoreCase));

            foreach (var buildConfig in buildConfigs)
            {
                var details = api.GetBuildTypeDetailsById(buildConfig.Id);
                
                AddPublishDataFromSteps(buildConfig, details);

                AddSubscribeDataFromTriggers(buildConfig, details);

                if(!NoArtifact)
                    AddPublishDataFromArtifacts(buildConfig, api);
            }


            if (PackageAsVertex)
            {
                BuildGraphWithPackagesAsVertices(_mappings);
                _fancyGraph.ToDirectedGraphML(_fancyGraph.GetVertexIdentity(), _fancyGraph.GetEdgeIdentity(), GetNodeFormat(), GetEdgeFormat()).WriteXml(_outputFilename);
            }
            else
            {
                BuildGraphWithPackagesAsLabels(_mappings);
                _simpleGraph.ToDirectedGraphML(_simpleGraph.GetVertexIdentity(), _simpleGraph.GetEdgeIdentity(),(s,n) => n.Label = s,(s, e) => e.Label = s.Tag).WriteXml(_outputFilename);
            }

            Console.WriteLine();
            sw.Stop();
            OutputElapsedTime(sw);
            Environment.Exit(0);

        }

        private static Action<VertexBase, DirectedGraphNode> GetNodeFormat()
        {
            Action<VertexBase, DirectedGraphNode> formatNode = (v, n) =>
                                                                   {
                                                                       n.Label = v.Name;
                                                                       if (v is PackageVertex)
                                                                       {
                                                                           SetNodeDetails(n, null, "None", "Package");
                                                                       }
                                                                       else
                                                                       {
                                                                           SetNodeDetails(n, "Green", "RoundedRectangle", "Build");
                                                                       }
                                                                   };
            return formatNode;
        }

        private static Action<EquatableEdge<VertexBase>, DirectedGraphLink> GetEdgeFormat()
        {
            Action<EquatableEdge<VertexBase>, DirectedGraphLink> formatEdge = (e, l) => { l.Label = ""; };
            return formatEdge;
        }

        private static void SetNodeDetails(DirectedGraphNode n, string background, string shape, string category)
        {
            if (!string.IsNullOrEmpty(background))
                n.Background = background;
            if (!string.IsNullOrEmpty(shape))
                n.Shape = shape;
            n.Category = new DirectedGraphNodeCategory[]
                             {
                                 new DirectedGraphNodeCategory {Ref = category}
                             };
        }

        private void AddPublishDataFromArtifacts(BuildType buildConfig, TeamCityApi api)
        {
            //check artifacts..
            //TODO we cant detect feed here, so turn off when feed specific?
            foreach (var artifact in api.GetArtifactListByBuildType(buildConfig.Id).Where(a => a.Ext.Equals("nupkg")))
            {
                var package = Regex.Match(artifact.Name, @".+?(?=(?:(?:[\._]\d+){2,})$)").Value;
                AddBuildPackageMappingIfRequired(buildConfig);
                if (!_mappings[buildConfig.Name].Publishes.Contains(package))
                    _mappings[buildConfig.Name].Publishes.Add(package);
            }
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
                if (!_mappings[buildConfig.Name].Subscribes.Contains(package))
                    _mappings[buildConfig.Name].Subscribes.Add(package);
            }
        }

        private void AddPublishDataFromSteps(BuildType buildConfig, BuildTypeDetails details)
        {
            //Check for nuget publish steps
            var steps = details.Steps.Where(s => s.Type.Equals("jb.nuget.publish"));
            //Constrain by feed if required.
            if (!string.IsNullOrEmpty(Feed))
                steps = steps.Where(s =>
                                        {
                                            var prop = s.Properties.FirstOrDefault(p => p.Name.Equals("nuget.publish.source"));
                                            if (prop != null && prop.value.Equals(Feed))
                                                return true;
                                            return false;
                                        });

            foreach (var publishStep in steps)
            {
                AddBuildPackageMappingIfRequired(buildConfig);
                var package = publishStep.Properties.First(p => p.Name.Equals("nuget.publish.files")).value.Replace(".nupkg", "");
                if (!_mappings[buildConfig.Name].Publishes.Contains(package))
                    _mappings[buildConfig.Name].Publishes.Add(package);
            }
        }

        private void BuildGraphWithPackagesAsLabels(Dictionary<string, BuildPackageMapping> mappings)
        {
            foreach (var mapping in mappings)
            {
                foreach (var subscription in mapping.Value.Subscribes)
                {
                    var matches = mappings.Where(n => n.Value.Publishes.Any(p => p.Equals(subscription)));
                    foreach (var match in matches)
                    {
                        _simpleGraph.AddVerticesAndEdge(new TaggedEquatableEdge<string, string>(match.Key, mapping.Key,subscription));
                    }
                }
            }
        }

        private void BuildGraphWithPackagesAsVertices(Dictionary<string, BuildPackageMapping> mappings)
        {
            foreach (var mapping in mappings)
            {
                var build = FindOrCreateVertex<BuildVertex>(mapping.Key);
                foreach (var subscription in mapping.Value.Subscribes)
                {
                    var package = FindOrCreateVertex<PackageVertex>(subscription);
                    _fancyGraph.AddEdge(new EquatableEdge<VertexBase>(package, build));
                }

                foreach (var publications in mapping.Value.Publishes)
                {
                    var package = FindOrCreateVertex<PackageVertex>(publications);
                    _fancyGraph.AddEdge(new EquatableEdge<VertexBase>(build, package));
                }
            }
        }

        private VertexBase FindOrCreateVertex<T>(string name) where T : VertexBase, new()
        {
            Func<VertexBase, bool> func = v => v.GetType() == typeof (T) && v.Name.Equals(name);
            var vertex = _fancyGraph.Vertices.FirstOrDefault(func);
            if (vertex == null)
            {
                vertex = new T() {Name = name};
                _fancyGraph.AddVertex(vertex);
            }
            return vertex;
        }

        private void AddBuildPackageMappingIfRequired(BuildType buildConfig)
        {
            if (!_mappings.ContainsKey(buildConfig.Name))
                _mappings.Add(buildConfig.Name, new BuildPackageMapping()
                                                    {
                                                        Build = buildConfig.Name,
                                                        Publishes = new List<string>(),
                                                        Subscribes = new List<string>()
                                                    });
        }

        private void OutputElapsedTime(Stopwatch sw)
        {
            Console.WriteLine("Completed search in {0} seconds", sw.Elapsed.TotalSeconds);
        }
    }

    internal class VertexBase
    {
        public string Name { get; set; }
    }

    internal class PackageVertex : VertexBase {}
    internal class BuildVertex : VertexBase {}

    public class BuildPackageMapping
    {
        public string Build { get; set; }
        public List<String> Publishes { get; set; }
        public List<String> Subscribes { get; set; }
    }
    
}
