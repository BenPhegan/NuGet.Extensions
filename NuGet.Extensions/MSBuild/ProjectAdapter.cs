using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace NuGet.Extensions.MSBuild
{
    public class ProjectAdapter : IVsProject
    {
        private readonly Project _project;
        private readonly IProjectLoader _projectLoader;

        public ProjectAdapter(Project project, IProjectLoader projectLoader)
        {
            _project = project;
            _projectLoader = projectLoader;
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
            var projectGuid = r.GetMetadataValue("Project");
            var csprojRelativePath = r.EvaluatedInclude;
            var absoluteProjectPath = Path.Combine(ProjectDirectory.FullName, csprojRelativePath);
            var referencedProjectAdapter = _projectLoader.GetProject(Guid.Parse(projectGuid), absoluteProjectPath);
            return new ProjectReferenceAdapter(referencedProjectAdapter, () => _project.RemoveItem(r), AddBinaryReference);
        }

        private void AddBinaryReference(string includePath, KeyValuePair<string, string> metadata)
        {
            _project.AddItem("Reference", includePath, new[]{metadata});
        }
    }
}