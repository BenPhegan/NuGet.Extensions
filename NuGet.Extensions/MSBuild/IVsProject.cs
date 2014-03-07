using System.Collections.Generic;

namespace NuGet.Extensions.MSBuild
{
    public interface IVsProject {
        IEnumerable<IBinaryReference> GetBinaryReferences();
        string AssemblyName { get; }
        void Save();
        void AddPackagesConfig();
    }
}