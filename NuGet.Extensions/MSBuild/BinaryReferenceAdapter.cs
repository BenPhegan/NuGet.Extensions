using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Build.Evaluation;
using NuGet.Extensions.ReferenceAnalysers;

namespace NuGet.Extensions.MSBuild
{
    [DebuggerDisplay("{AssemblyName}")]
    public class BinaryReferenceAdapter : IReference
    {
        private readonly ProjectItem _reference;

        public BinaryReferenceAdapter(ProjectItem reference, bool conditionTrue)
        {
            _reference = reference;
            Condition = conditionTrue;
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

        public bool AssemblyFilenameEquals(string filename)
        {
            return string.Equals(filename, AssemblyFilename, StringComparison.OrdinalIgnoreCase);
        }

        public string AssemblyVersion
        {
            get { return _reference.EvaluatedInclude.Contains(',') ? _reference.EvaluatedInclude.Split(',')[1].Split('=')[1] : null; }
        }

        public string AssemblyName
        {
            get { return _reference.EvaluatedInclude.Contains(',') ? _reference.EvaluatedInclude.Split(',')[0] : _reference.EvaluatedInclude; }
        }

        public string AssemblyFilename
        {
            get
            {
                string hintPath;
                if (TryGetHintPath(out hintPath))
                {
                    return Path.GetFileName(hintPath);
                }
                else
                {
                    return AssemblyName + ".dll";
                }
            }
        }

        public bool Condition { get; private set; }
    }
}