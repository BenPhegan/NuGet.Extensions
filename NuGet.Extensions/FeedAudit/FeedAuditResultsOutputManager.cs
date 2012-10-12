using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet.Extensions.FeedAudit
{
    public class FeedAuditResultsOutputManager
    {
        private readonly List<FeedAuditResult> _results;
        private AuditEventTypes _auditEventTypes;

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
            }

            foreach (var output in outputList)
                writer.WriteLine(output.ToString());
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

    [Flags]
    public enum AuditEventTypes
    {
        ResolvedAssemblyReferences = 1,
        UnloadablePackageFiles = 2,
        UnresolvedAssemblyReferences = 4,
        UnresolvedDependencies = 8,
        UnusedPackageDependencies = 16,
        UsedPackageDependencies = 32
    }
}