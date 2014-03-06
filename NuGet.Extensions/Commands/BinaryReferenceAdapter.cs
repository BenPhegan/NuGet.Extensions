using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace NuGet.Extensions.Commands
{
    public class BinaryReferenceAdapter {
        private readonly ProjectItem _reference;

        public BinaryReferenceAdapter(ProjectItem reference)
        {
            _reference = reference;
        }

        public string GetHintPath()
        {
            return _reference.GetMetadataValue("HintPath");
        }

        public bool HasHintPath()
        {
            return _reference.HasMetadata("HintPath");
        }

        public ProjectMetadata SetHintPath(string newHintPathRelative)
        {
            return _reference.SetMetadataValue("HintPath", newHintPathRelative);
        }

        public string GetIncludeVersion()
        {
            return _reference.EvaluatedInclude.Contains(',') ? _reference.EvaluatedInclude.Split(',')[1].Split('=')[1] : null;
        }

        public string GetIncludeName()
        {
            return _reference.EvaluatedInclude.Contains(',') ? _reference.EvaluatedInclude.Split(',')[0] : _reference.EvaluatedInclude;
        }

        public bool IsForAssembly(string assemblyFilename)
        {
            if (HasHintPath())
            {
                var hintpath = GetHintPath();
                var fileInfo = new FileInfo(hintpath);
                return fileInfo.Name.Equals(assemblyFilename, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}