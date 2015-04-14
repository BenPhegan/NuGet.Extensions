using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NuGet.Extensions.MSBuild
{
    [DebuggerDisplay("{ProjectName} {AssemblyName}")]
    public class NullProjectAdapter : IVsProject {
        private readonly string _projectPath;

        public NullProjectAdapter(string projectPath)
        {
            _projectPath = projectPath;
        }

        public IEnumerable<IReference> GetBinaryReferences()
        {
            return Enumerable.Empty<IReference>();
        }

        public string AssemblyName
        {
            get { return "Unknown"; }
        }

        public string ProjectName
        {
            get { return Path.GetFileNameWithoutExtension(_projectPath); }
        }

        public DirectoryInfo ProjectDirectory
        {
            get { return new FileInfo(_projectPath).Directory; }
        }

        public void Save()
        {
        }

        public void AddFile(string filename)
        {
        }

        public IEnumerable<IReference> GetProjectReferences()
        {
            return Enumerable.Empty<IReference>();
        }
    }
}