using Microsoft.Build.Evaluation;

namespace NuGet.Extensions.Commands
{
    public interface IBinaryReference {
        string GetHintPath();
        bool HasHintPath();
        void SetHintPath(string newHintPathRelative);
        string GetIncludeVersion();
        string GetIncludeName();
        bool IsForAssembly(string assemblyFilename);
    }
}