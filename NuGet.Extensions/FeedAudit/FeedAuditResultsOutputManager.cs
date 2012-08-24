using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet.Extensions.FeedAudit
{
    public class FeedAuditResultsOutputManager
    {
        private readonly List<FeedAuditResult> _results;
        
        public FeedAuditResultsOutputManager(List<FeedAuditResult> auditResults)
        {
            _results = auditResults;
        }

        public void Output(TextWriter writer)
        {
            var outputList = new List<AuditResultsOutput>();
            foreach (var result in _results)
            {
                outputList.AddRange(result.ResolvedAssemblyReferences.Select(u => new AuditResultsOutput {PackageName = result.Package.Id, Category = "Resolved Assembly", Item = u.Name}));
                outputList.AddRange(result.UnloadablePackageFiles.Select(u => new AuditResultsOutput {PackageName = result.Package.Id, Category = "Unloadable Package File", Item = u}));
                outputList.AddRange(result.UnresolvedAssemblyReferences.Select(u => new AuditResultsOutput { PackageName = result.Package.Id, Category = "Unresolved Assembly", Item = u.Name }));
                outputList.AddRange(result.UnresolvedDependencies.Select(u => new AuditResultsOutput { PackageName = result.Package.Id, Category = "Unresolved Package Dependency", Item = u.Id }));
                outputList.AddRange(result.UnusedPackageDependencies.Select(u => new AuditResultsOutput { PackageName = result.Package.Id, Category = "Unused Package Dependency", Item = u.Id }));
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
}