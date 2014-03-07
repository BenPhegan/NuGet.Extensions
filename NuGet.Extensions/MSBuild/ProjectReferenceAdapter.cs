using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace NuGet.Extensions.MSBuild
{
    public class ProjectReferenceAdapter : IReference
    {
        private readonly Func<bool> _removeFromParentProject;
        private readonly Action<string, KeyValuePair<string, string>> _addBinaryReferenceWithMetadata;
        private readonly ProjectItem _reference;
        private string _assemblyName;

        public ProjectReferenceAdapter(Func<bool> removeFromParentProject, Action<string, KeyValuePair<string, string>> addBinaryReferenceWithMetadata, ProjectItem reference)
        {
            _removeFromParentProject = removeFromParentProject;
            _addBinaryReferenceWithMetadata = addBinaryReferenceWithMetadata;
            _reference = reference;
        }

        public bool TryGetHintPath(out string hintPath)
        {
            hintPath = null;
            return false;
        }

        public void ConvertToNugetReferenceWithHintPath(string hintPath)
        {
            _removeFromParentProject();
            _addBinaryReferenceWithMetadata(_assemblyName, new KeyValuePair<string, string>("HintPath", hintPath));
        }

        public string IncludeVersion
        {
            get { return null; }
        }

        public string IncludeName
        {
            get { return _reference.EvaluatedInclude.Contains(',') ? _reference.EvaluatedInclude.Split(',')[0] : _reference.EvaluatedInclude; }
        }

        public bool IsForAssembly(string assemblyFilename)
        {
            throw new NotImplementedException();
        }
    }
}