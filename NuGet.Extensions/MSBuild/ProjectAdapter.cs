using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace NuGet.Extensions.MSBuild
{
    [DebuggerDisplay("{ProjectName} {AssemblyName}")]
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
            const string itemType = "Reference";
            var allBinaryReferences = _project.GetItemsIgnoringCondition(itemType);
            var conditionTrueReferences = new HashSet<ProjectItem>(_project.GetItems(itemType));
            return allBinaryReferences.Select(r => new BinaryReferenceAdapter(r, conditionTrueReferences.Contains(r)));
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
            var allrojectReferences = _project.GetItemsIgnoringCondition("ProjectReference");
            var conditionTrueProjectReferences = new HashSet<ProjectItem>(_project.GetItems("ProjectReference"));
            return allrojectReferences.Select(r => GetProjectReferenceAdapter(r, conditionTrueProjectReferences.Contains(r)));
        }

        private ProjectReferenceAdapter GetProjectReferenceAdapter(ProjectItem r, bool conditionTrue)
        {
            var projectGuidString = r.GetMetadataValue("Project");
            var csprojRelativePath = r.EvaluatedInclude;
            var absoluteProjectPath = Path.Combine(ProjectDirectory.FullName, csprojRelativePath);
            var guid = ParsedOrDefault(projectGuidString);
            var referencedProjectAdapter = _projectLoader.GetProject(guid, absoluteProjectPath);
            return new ProjectReferenceAdapter(referencedProjectAdapter, () => _project.RemoveItem(r), AddBinaryReferenceIfNotExists, conditionTrue);
        }

        private static Guid? ParsedOrDefault(string projectGuidString)
        {
            Guid guid;
            if (Guid.TryParse(projectGuidString, out guid)) return guid;
            return null;
        }

        private void AddBinaryReferenceIfNotExists(string includePath, KeyValuePair<string, string> metadata)
        {
            var existingReferences = GetBinaryReferences().Where(r => string.Equals(r.AssemblyName, includePath, StringComparison.InvariantCultureIgnoreCase));
            if (!existingReferences.Any()) _project.AddItem("Reference", includePath, new[]{metadata});
        }
    }
}