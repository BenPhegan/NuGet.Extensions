using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace NuGet.Extensions.MSBuild
{
    public class ProjectAdapter : IVsProject
    {
        private readonly Project _project;

        public ProjectAdapter(string projectPath, IDictionary<string, string> globalMsBuildProperties = null)
            : this(CreateMsBuildProject(projectPath, globalMsBuildProperties))
        {
        }

        public ProjectAdapter(Project project)
        {
            _project = project;
        }

        private static Project CreateMsBuildProject(string projectPath, IDictionary<string, string> globalMsBuildProperties)
        {
            return new Project(projectPath, globalMsBuildProperties, null, new ProjectCollection());
        }

        public IEnumerable<IReference> GetBinaryReferences()
        {
            return _project.GetItems("Reference").Select(r => new BinaryReferenceAdapter(r));
        }

        public string AssemblyName
        {
            get { return _project.GetPropertyValue("AssemblyName"); }
        }

        public string ProjectName
        {
            get { return _project.GetPropertyValue("ProjectName"); }
        }

        public DirectoryInfo ProjectDirectory
        {
            get { return new DirectoryInfo(_project.DirectoryPath); }
        }

        public void Save()
        {
            _project.Save();
        }

        public void AddFile(string filename)
        { //Add the packages.config to the project content, otherwise later versions of the VSIX fail...
            if (!FileAlreadyReferenced(filename))
            {
                _project.Xml.AddItemGroup().AddItem("None", filename);
            }
        }

        private bool FileAlreadyReferenced(string filename)
        {
            return _project.GetItems("None").Any(i => i.UnevaluatedInclude.Equals(filename));
        }

        public IEnumerable<IReference> GetProjectReferences()
        {
            return _project.GetItems("ProjectReference").Select(GetProjectReferenceAdapter);
        }

        private ProjectReferenceAdapter GetProjectReferenceAdapter(ProjectItem r)
        {
            return new ProjectReferenceAdapter(() => _project.RemoveItem(r), AddBinaryReference, r);
        }

        private void AddBinaryReference(string includePath, KeyValuePair<string, string> metadata)
        {
            _project.AddItem("Reference", includePath, new[]{metadata});
        }
    }
}