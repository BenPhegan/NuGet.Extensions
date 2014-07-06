using System.IO;

namespace NuGet.Extensions.ReferenceAnalysers
{
    public interface IHintPathGenerator
    {
        string ForAssembly(DirectoryInfo solutionDir, DirectoryInfo projectDir, IPackage package, string assemblyFilename);
    }
}