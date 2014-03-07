namespace NuGet.Extensions.MSBuild
{
    public interface IBinaryReference {
        bool HasHintPath();
        string HintPath { set; get; }
        string IncludeVersion { get; }
        string IncludeName { get; }
        bool IsForAssembly(string assemblyFilename);
    }
}