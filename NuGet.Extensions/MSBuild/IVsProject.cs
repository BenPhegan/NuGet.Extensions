using System.Collections.Generic;
using System.IO;

namespace NuGet.Extensions.MSBuild
{
    public interface IVsProject {
        IEnumerable<IReference> GetBinaryReferences();
        string AssemblyName { get; }
        DirectoryInfo ProjectDirectory { get; }
        void Save();
        void AddPackagesConfig();
    }
}