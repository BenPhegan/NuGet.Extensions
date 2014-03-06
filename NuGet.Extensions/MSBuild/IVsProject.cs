using System.Collections.Generic;
using Microsoft.Build.Evaluation;

namespace NuGet.Extensions.Commands
{
    public interface IVsProject {
        IEnumerable<IBinaryReference> GetBinaryReferences();
        string GetAssemblyName();
        void Save();
        void AddPackagesConfig();
    }
}