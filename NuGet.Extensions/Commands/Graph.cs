using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using NuGet.Commands;
using NuGet.Common;
using QuickGraph;
using QuickGraph.Serialization;
using QuickGraph.Algorithms;

namespace NuGet.Extensions.Commands
{
    [Command("graph", "Provides a DGML graph of either a package and all its dependencies, or an entire feed.", MinArgs = 0)]
    public class Graph : Command
    {
        private readonly List<string> _sources = new List<string>();
        public IPackageRepositoryFactory RepositoryFactory { get; set; }
        public IPackageSourceProvider SourceProvider { get; set; }
        private AdjacencyGraph<string, Edge<string>> _graph;

        [Option("A list of sources to search")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option("Check whether there are any cycles in the graph.")]
        public Boolean DAGCheck { get; set; }

        [Option("Cull any disconnected vertices from the graph.")]
        public Boolean NoLoners { get; set; }

        [Option("Output file name.")]
        public string Output { get; set; }

        [ImportingConstructor]
        public Graph(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            Contract.Assert(packageRepositoryFactory != null);
            Contract.Assert(sourceProvider != null);

            RepositoryFactory = packageRepositoryFactory;
            SourceProvider = sourceProvider;
        }

        public override void ExecuteCommand()
        {
            var sw = new Stopwatch();
            sw.Start();
            var repository = GetRepository();
            var packageSource = repository.GetPackages().Where(p => p.IsLatestVersion);
            var packages = FilterPackageList(packageSource);

            _graph = new AdjacencyGraph<string,Edge<string>>();

            foreach (var package in packages)
            {
                RecursePackageDependencies(package);
            }

            if (NoLoners)
            {
                RemoveLonersFromGraph();
            }

            var dgml = _graph.ToDirectedGraphML(_graph.GetVertexIdentity(),_graph.GetEdgeIdentity(),(n,d) => d.Label = n,(e,l) => l.Label = "");
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
//If we have a vertex that isnt used in an edge, and we are looking for a cleaner graph, then delete the vertex...
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
            _graph.AddVertex(package.Id.ToLowerInvariant());
            foreach (var dependency in package.Dependencies)
            {
                if (!_graph.Vertices.Contains(package.Id.ToLowerInvariant()))
                    RecursePackageDependencies((IPackage)dependency);
                _graph.AddVerticesAndEdge(new Edge<string>(package.Id.ToLowerInvariant(), dependency.Id.ToLowerInvariant()));
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
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;
            return repository;
        }
    }
}
