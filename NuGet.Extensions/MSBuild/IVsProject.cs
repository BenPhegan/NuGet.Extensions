using System.Collections.Generic;

namespace NuGet.Extensions.MSBuild
{
    public interface IVsProject {
        IEnumerable<IBinaryReference> GetBinaryReferences();
        string GetAssemblyName();
        void Save();
        void AddPackagesConfig();
    }
}