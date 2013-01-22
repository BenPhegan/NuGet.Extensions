using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Evaluation;

namespace NuGet.Extensions.ExtensionMethods
{
    public static class ProjectExtensions
    {
        public static List<string> GetProjectReferences(this Project project)
        {
            var refs = new List<string>();
            var references = project.GetItems("ProjectReference");
            foreach (var reference in references)
            {
                var refProject = new Project(Path.Combine(project.DirectoryPath, reference.UnevaluatedInclude),new Dictionary<string, string>(),null,new ProjectCollection());
                refs.Add(refProject.GetPropertyValue("AssemblyName"));
            }
            return refs;
        }
    }
}
