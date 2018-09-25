using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Extensions.Commands.GraphCommand;
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Serialization;

namespace NuGet.Extensions.Commands
{
    [Command("graph", "Provides a DGML graph of either a package and all its dependencies, or an entire feed.", MinArgs = 0)]
    public class Graph : Command
    {
        private readonly List<string> _sources = new List<string>();
        private AdjacencyGraph<IPackageInfo, Edge<IPackageInfo>> _graph;
        private IPackageInfoFactory _packageInfoFactory;

        [Option("A list of sources to search")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option("Check whether there are any cycles in the graph.")]
        public Boolean DAGCheck { get; set; }

        [Option("Cull any disconnected vertices from the graph.")]
        public Boolean NoLoners { get; set; }

        private string _output = "graph.dgml";

        [Option("Output file name.")]
        public string Output
        {
            get { return _output; }
            set { _output = value; }
        }


        [Option("With version details.")]
        public bool WithVersion { get; set; }

        [ImportingConstructor]
        public Graph () {}

        public override void ExecuteCommand()
        {
            var sw = new Stopwatch();
            sw.Start();

            var repository = GetRepository();
            var packageSource = repository.GetPackages();
            var packages = FilterPackageList(packageSource);

            _packageInfoFactory = WithVersion
                                      ? (IPackageInfoFactory) new FullPackageInfoFactory()
                                      : new SimplePackageInfoFactory();
            _graph = new AdjacencyGraph<IPackageInfo, Edge<IPackageInfo>>();

            foreach (var package in packages)
            {
                RecursePackageDependencies(package);
            }

            if (NoLoners)
            {
                RemoveLonersFromGraph();
            }

            var dgml = _graph.ToDirectedGraphML(_graph.GetVertexIdentity(), 
                                                _graph.GetEdgeIdentity(),
                                                (n, d) =>
                                                {
                                                    d.Label = n.ToString();
                                                    d.Description = n.Details;
                                                },
                                                (e, l) => l.Label = e.Target.TargetInfo);
            if (File.Exists(Output))
                File.Delete(Output);

            dgml.WriteXml(Output);

            var isDirectedAcyclicGraph = _graph.IsDirectedAcyclicGraph();
            if (DAGCheck)
            {
                Console.WriteLine();
                Console.WriteLine(isDirectedAcyclicGraph ? "Graph is a DAG." : "Graph is CYCLIC");
            }

            Console.WriteLine();
            sw.Stop();
            OutputElapsedTime(sw);
            Environment.Exit(DAGCheck ? isDirectedAcyclicGraph ? 0 : 1 : 0);
        }

        private void RemoveLonersFromGraph()
        {
            //If we have a vertex that isn't used in an edge, and we are looking for a cleaner graph, then delete the vertex...
            var toDelete =
                _graph.Vertices.Where(vertex => !_graph.Edges.Any(e => e.Source.Equals(vertex) || e.Target.Equals(vertex))).
                      ToList();
            toDelete.ForEach(v =>
                             {
                                 Console.WriteLine("Removing loner: {0}", v);
                                 _graph.RemoveVertex(v);
                             });
        }

        private void RecursePackageDependencies(IPackage package)
        {
            Console.WriteLine("Adding package and dependencies: {0}", package.Id);
            _graph.AddVertex(_packageInfoFactory.From(package));
            //We are going to ignore the TargetFramework for these purposes...
            foreach (PackageDependency dependency in package.DependencySets.SelectMany(p => p.Dependencies))
            {
                _graph.AddVerticesAndEdge(new Edge<IPackageInfo>(_packageInfoFactory.From(package),
                                                                 _packageInfoFactory.From(dependency)));
            }
        }


        private IEnumerable<IPackage> FilterPackageList(IQueryable<IPackage> packageSource)
        {
            IQueryable<IPackage> packages;
            if (Arguments.Count > 0 && !string.IsNullOrEmpty(Arguments[0]))
            {
                packages = packageSource.Where(p => p.Id.Equals(Arguments[0], StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                packages = packageSource.OrderBy(p => p.Id);
            }
            return packages;
        }

        private void OutputElapsedTime(Stopwatch sw)
        {
            Console.WriteLine("Completed graph in {0} seconds", sw.Elapsed.TotalSeconds);
        }

        private IPackageRepository GetRepository()
        {
            FixSource();

            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;
            return repository;
        }

        private void FixSource()
        {
            // Allow to run without source or with "." as current folder
            if (Source.Count == 0 || (Source.Count == 1 && Source.Contains(".")))
            {
                Source.Clear();
                Source.Add(Environment.CurrentDirectory);
            }
        }
    }
}