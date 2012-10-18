using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NuGet.Extensions.FeedAudit
{
    public class FeedAuditResultsOutputManager
    {
        private readonly List<FeedAuditResult> _results;
        private readonly AuditEventTypes _auditEventTypes;

        public FeedAuditResultsOutputManager(List<FeedAuditResult> auditResults, AuditEventTypes auditEventTypes)
        {
            _results = auditResults;
            _auditEventTypes = auditEventTypes;
        }

        public void Output(TextWriter writer)
        {
            var outputList = new List<AuditResultsOutput>();
            foreach (var result in _results)
            {
                if (_auditEventTypes.HasFlag(AuditEventTypes.ResolvedAssemblyReferences))
                    outputList.AddRange(result.ResolvedAssemblyReferences.Select(u => new AuditResultsOutput {PackageName = result.Package.Id, Category = "Resolved Assembly", Item = u.Name}));
                if (_auditEventTypes.HasFlag(AuditEventTypes.UnloadablePackageFiles))
                    outputList.AddRange(result.UnloadablePackageFiles.Select(u => new AuditResultsOutput { PackageName = result.Package.Id, Category = "Unloadable Package File", Item = u }));
                if (_auditEventTypes.HasFlag(AuditEventTypes.UnresolvedAssemblyReferences))
                    outputList.AddRange(result.UnresolvedAssemblyReferences.Select(u => new AuditResultsOutput { PackageName = result.Package.Id, Category = "Unresolved Assembly", Item = u.Name }));
                if (_auditEventTypes.HasFlag(AuditEventTypes.UnresolvedDependencies))
                    outputList.AddRange(result.UnresolvedDependencies.Select(u => new AuditResultsOutput { PackageName = result.Package.Id, Category = "Unresolved Package Dependency", Item = u.Id }));
                if (_auditEventTypes.HasFlag(AuditEventTypes.UnusedPackageDependencies))
                    outputList.AddRange(result.UnusedPackageDependencies.Select(u => new AuditResultsOutput { PackageName = result.Package.Id, Category = "Unused Package Dependency", Item = u.Id }));
                if (_auditEventTypes.HasFlag(AuditEventTypes.UsedPackageDependencies))
                    outputList.AddRange(result.UsedPackageDependencies.Select(u => new AuditResultsOutput { PackageName = result.Package.Id, Category = "Used Package Dependency", Item = u.Id }));
                if (_auditEventTypes.HasFlag(AuditEventTypes.FeedResolvableReferences))
                    outputList.AddRange(result.FeedResolvableReferences.Select(u => new AuditResultsOutput { PackageName = result.Package.Id, Category = "Feed Resolvable Assembly", Item = u.Name }));
                if (_auditEventTypes.HasFlag(AuditEventTypes.GacResolvableReferences))
                    outputList.AddRange(result.GacResolvableReferences.Select(u => new AuditResultsOutput { PackageName = result.Package.Id, Category = "GAC Resolvable Assembly", Item = u.Name }));
            }

            foreach (var output in outputList)
                writer.WriteLine(output.ToString());
            writer.Close();
        }
    
        public void OutputUnresolvableReferences(TextWriter writer)
        {
            var outputList = new List<AuditResultsOutput>();
            foreach (var result in _results)
            {
                foreach (var unresolved in result.UnresolvedAssemblyReferences)
                {
                    if (!_results.Any(r => r.FeedResolvableReferences.Any(fr => fr.Name.Equals(unresolved.Name, StringComparison.OrdinalIgnoreCase))) &&
                        !_results.Any(r => r.GacResolvableReferences.Any(gr => gr.Name.Equals(unresolved.Name, StringComparison.OrdinalIgnoreCase))))
                        outputList.AddRange(result.UnresolvedAssemblyReferences.Select(u => new AuditResultsOutput { PackageName = result.Package.Id, Category = "Feed/GAC Unresolvable Assembly", Item = unresolved.Name }));
                }
            }
            foreach (var output in outputList.GroupBy(a => a.Item, (key, group) => group.First()))
                writer.WriteLine(output.ToString());
            writer.Close();
        }


        class AuditResultsOutput
        {
            public override string ToString()
            {
                return string.Format("{0},{1},{2}",PackageName, Category, Item);
            }
            public string PackageName;
            public string Category;
            public string Item;
        }
    }
}