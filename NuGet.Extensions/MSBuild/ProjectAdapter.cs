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

        public ProjectAdapter(Project project, string packagesConfigFilename)
        {
            _project = project;
            _packagesConfigFilename = packagesConfigFilename;
        }

        public IEnumerable<IBinaryReference> GetBinaryReferences()
        {
            return _project.GetItems("Reference").Select(r => new BinaryReferenceAdapter(r));
        }

        public string AssemblyName
        {
            get { return _project.GetPropertyValue("AssemblyName"); }
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
                Save();
            }
        }

        private bool HasPackagesConfig()
        {
            return _project.GetItems("None").Any(i => i.UnevaluatedInclude.Equals(_packagesConfigFilename));
        }

        public static List<string> GetReferencedAssemblies(IEnumerable<IBinaryReference> references)
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
    }
}