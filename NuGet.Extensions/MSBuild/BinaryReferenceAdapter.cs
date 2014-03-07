using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace NuGet.Extensions.MSBuild
{
    public class BinaryReferenceAdapter : IBinaryReference
    {
        private readonly ProjectItem _reference;

        public BinaryReferenceAdapter(ProjectItem reference)
        {
            _reference = reference;
        }

        public bool HasHintPath()
        {
            return _reference.HasMetadata("HintPath");
        }

        public string HintPath
        {
            set { _reference.SetMetadataValue("HintPath", value); }
            get { return _reference.GetMetadataValue("HintPath"); }
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
            if (HasHintPath())
            {
                var hintpath = HintPath;
                var fileInfo = new FileInfo(hintpath);
                return fileInfo.Name.Equals(assemblyFilename, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}