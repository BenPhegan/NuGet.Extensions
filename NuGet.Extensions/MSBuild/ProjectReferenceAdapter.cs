using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NuGet.Extensions.MSBuild
{
    [DebuggerDisplay("{AssemblyName}")]
    public class ProjectReferenceAdapter : IReference
    {
        private readonly Func<bool> _removeFromParentProject;
        private readonly IVsProject _project;
        private readonly Action<string, KeyValuePair<string, string>> _addBinaryReferenceWithMetadata;

        public ProjectReferenceAdapter(IVsProject project, Func<bool> removeFromParentProject, Action<string, KeyValuePair<string, string>> addBinaryReferenceWithMetadata)
        {
            _project = project;
            _removeFromParentProject = removeFromParentProject;
            _addBinaryReferenceWithMetadata = addBinaryReferenceWithMetadata;
        }

        public bool TryGetHintPath(out string hintPath)
        {
            hintPath = null;
            return false;
        }

        public void ConvertToNugetReferenceWithHintPath(string hintPath)
        {
            _removeFromParentProject();
            _addBinaryReferenceWithMetadata(_project.AssemblyName, new KeyValuePair<string, string>("HintPath", hintPath));
        }

        public string AssemblyVersion
        {
            get { return null; }
        }

        public string AssemblyName
        {
            get { return _project.AssemblyName; }
        }

        public bool IsForAssembly(string assemblyFilename)
        {
            return (AssemblyName + ".dll").Equals(assemblyFilename, StringComparison.OrdinalIgnoreCase);
        }
    }
}