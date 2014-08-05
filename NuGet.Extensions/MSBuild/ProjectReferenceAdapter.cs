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
        private readonly Action<string, KeyValuePair<string, string>> _addBinaryReferenceWithMetadataIfNotExists;

        public ProjectReferenceAdapter(IVsProject project, Func<bool> removeFromParentProject, Action<string, KeyValuePair<string, string>> addBinaryReferenceWithMetadataIfNotExists, bool conditionTrue)
        {
            _project = project;
            _removeFromParentProject = removeFromParentProject;
            _addBinaryReferenceWithMetadataIfNotExists = addBinaryReferenceWithMetadataIfNotExists;
            Condition = conditionTrue;
        }

        public bool TryGetHintPath(out string hintPath)
        {
            hintPath = null;
            return false;
        }

        public void ConvertToNugetReferenceWithHintPath(string hintPath)
        {
            _removeFromParentProject();
            _addBinaryReferenceWithMetadataIfNotExists(_project.AssemblyName, new KeyValuePair<string, string>("HintPath", hintPath));
        }

        public string AssemblyVersion
        {
            get { return null; }
        }

        public string AssemblyName
        {
            get { return _project.AssemblyName; }
        }

        public bool Condition { get; private set; }

        public string AssemblyFilename
        {
            get { return AssemblyName + ".dll"; }
        }

        public bool AssemblyFilenameEquals(string filename)
        {
            return string.Equals(filename, AssemblyFilename, StringComparison.OrdinalIgnoreCase);
        }
    }
}