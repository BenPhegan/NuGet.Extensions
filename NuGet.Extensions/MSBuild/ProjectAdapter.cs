using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace NuGet.Extensions.MSBuild
{
    public class ProjectAdapter : IVsProject
    {
        private readonly Project _project;
        private readonly string _packagesConfigFilename;

        public ProjectAdapter(string projectPath, string packagesConfigFilename, IDictionary<string, string> globalMsBuildProperties = null)
            : this(CreateMsBuildProject(projectPath, globalMsBuildProperties), packagesConfigFilename)
        {
        }

        public ProjectAdapter(Project project, string packagesConfigFilename)
        {
            _project = project;
            _packagesConfigFilename = packagesConfigFilename;
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

        public void AddPackagesConfig()
        { //Add the packages.config to the project content, otherwise later versions of the VSIX fail...
            if (!HasPackagesConfig())
            {
                _project.Xml.AddItemGroup().AddItem("None", _packagesConfigFilename);
            }
        }

        private bool HasPackagesConfig()
        {
            return _project.GetItems("None").Any(i => i.UnevaluatedInclude.Equals(_packagesConfigFilename));
        }

        public static List<string> GetReferencedAssemblies(IEnumerable<IReference> references)
        {
            var referenceFiles = new List<string>();

            foreach (var reference in references)
            {
                //TODO deal with GAC assemblies that we want to replace as well....
                string hintPath;
                if (reference.TryGetHintPath(out hintPath))
                {
                    referenceFiles.Add(Path.GetFileName(hintPath));
                }
            }
            return referenceFiles;
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