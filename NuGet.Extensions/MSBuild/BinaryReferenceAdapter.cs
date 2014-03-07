using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Build.Evaluation;

namespace NuGet.Extensions.MSBuild
{
    public class BinaryReferenceAdapter : IReference
    {
        private readonly ProjectItem _reference;

        public BinaryReferenceAdapter(ProjectItem reference)
        {
            _reference = reference;
        }

        public bool TryGetHintPath(out string hintPath)
        {
            hintPath = null;
            var hasHintPath = _reference.HasMetadata("HintPath");
            if (hasHintPath) hintPath = _reference.GetMetadataValue("HintPath"); ;
            return hasHintPath;
        }

        public void ConvertToNugetReferenceWithHintPath(string hintPath)
        {
            _reference.SetMetadataValue("HintPath", hintPath);
        }

        public string IncludeVersion
        {
            get { return _reference.EvaluatedInclude.Contains(',') ? _reference.EvaluatedInclude.Split(',')[1].Split('=')[1] : null; }
        }

        public string IncludeName
        {
            get { return _reference.EvaluatedInclude.Contains(',') ? _reference.EvaluatedInclude.Split(',')[0] : _reference.EvaluatedInclude; }
        }

        public bool IsForAssembly(string assemblyFilename)
        {
            string hintpath;

            if (TryGetHintPath(out hintpath))
            {
                var fileInfo = new FileInfo(hintpath);
                return fileInfo.Name.Equals(assemblyFilename, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}