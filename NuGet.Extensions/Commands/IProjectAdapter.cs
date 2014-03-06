using System.Collections.Generic;
using Microsoft.Build.Evaluation;

namespace NuGet.Extensions.Commands
{
    public interface IProjectAdapter {
        ICollection<ProjectItem> GetBinaryReferences();
        string GetAssemblyName();
        void Save();
        void AddPackagesConfig();
    }
}