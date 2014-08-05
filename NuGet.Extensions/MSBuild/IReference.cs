namespace NuGet.Extensions.MSBuild
{
    public interface IReference {
        string AssemblyVersion { get; }
        string AssemblyName { get; }
        bool Condition { get; }
        string AssemblyFilename { get; }
        bool TryGetHintPath(out string hintPath);

        /// <summary>
        /// Note: The parent project must be saved in order for this change to persist
        /// </summary>
        void ConvertToNugetReferenceWithHintPath(string hintPath);

        bool AssemblyFilenameEquals(string filename);
    }
}