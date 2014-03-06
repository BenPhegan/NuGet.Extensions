using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace NuGet.Extensions.Commands
{
    public class BinaryReferenceAdapter {
        public BinaryReferenceAdapter() {}

        private static string GetHintPath(ProjectItem reference)
        {
            return reference.GetMetadataValue("HintPath");
        }

        private static bool HasHintPath(ProjectItem reference)
        {
            return reference.HasMetadata("HintPath");
        }

        public static ProjectMetadata SetHintPath(ProjectItem referenceMatch, string newHintPathRelative)
        {
            return referenceMatch.SetMetadataValue("HintPath", newHintPathRelative);
        }

        public static string GetIncludeVersion(ProjectItem referenceMatch)
        {
            return referenceMatch.EvaluatedInclude.Contains(',') ? referenceMatch.EvaluatedInclude.Split(',')[1].Split('=')[1] : null;
        }

        public static string GetIncludeName(ProjectItem referenceMatch)
        {
            return referenceMatch.EvaluatedInclude.Contains(',') ? referenceMatch.EvaluatedInclude.Split(',')[0] : referenceMatch.EvaluatedInclude;
        }

        public bool ResolveProjectReferenceItemByAssemblyName(ProjectItem reference, string mapping)
        {
            if (HasHintPath(reference))
            {
                var hintpath = GetHintPath(reference);
                var fileInfo = new FileInfo(hintpath);
                return fileInfo.Name.Equals(mapping, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}