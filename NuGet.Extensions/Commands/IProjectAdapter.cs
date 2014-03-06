using System.Collections.Generic;
using Microsoft.Build.Evaluation;

namespace NuGet.Extensions.Commands
{
    public interface IProjectAdapter {
        IEnumerable<BinaryReferenceAdapter> GetBinaryReferences();
        string GetAssemblyName();
        void Save();
        void AddPackagesConfig();
    }
}