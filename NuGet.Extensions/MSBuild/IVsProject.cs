using System.Collections.Generic;

namespace NuGet.Extensions.MSBuild
{
    public interface IVsProject {
        IEnumerable<IReference> GetBinaryReferences();
        string AssemblyName { get; }
        void Save();
        void AddPackagesConfig();
    }
}