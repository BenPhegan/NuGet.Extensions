namespace NuGet.Extensions.MSBuild
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