using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Common;

namespace NuGet.Extensions.Repositories
{
    public class AssemblyToPackageMapping 
    {
        private readonly Dictionary<string, List<IPackage>> _assemblyToPackageMapping;
        private readonly IConsole _console;
        private readonly IFileSystem _fileSystem;
        private readonly Lazy<IDictionary<string,List<IPackage>>> _resolvedMappings;
        private readonly Lazy<IList<string>> _failedMappings;

        public AssemblyToPackageMapping(IConsole console, IFileSystem fileSystem, Dictionary<string, List<IPackage>> assemblyToPackageMapping)
        {
            _console = console;
            _fileSystem = fileSystem;
            _assemblyToPackageMapping = assemblyToPackageMapping;
            _resolvedMappings = new Lazy<IDictionary<string, List<IPackage>>>(GetResolvedMappings);
            _failedMappings = new Lazy<IList<string>>(GetFailedMappings);
        }

        public IList<string> FailedMappings { get { return _failedMappings.Value; } }
        public IDictionary<string, List<IPackage>> ResolvedMappings { get { return _resolvedMappings.Value; } }


        /// <summary>
        /// Outputs a package.config file reflecting the set of packages that provides the requested set of assemblies.
        /// </summary>
        public void OutputPackageConfigFile()
        {
            var packagesConfig = Constants.PackageReferenceFile;
            if (_fileSystem.FileExists(packagesConfig))
                _fileSystem.DeleteFile(packagesConfig);

            if (!_fileSystem.FileExists(packagesConfig))
            {
                var prf = new PackageReferenceFile(_fileSystem, string.Format(".\\{0}", packagesConfig));
                foreach (var assemblyToPackageMapping in ResolvedMappings)
                {
                    IPackage smallestPackage;
                    if (assemblyToPackageMapping.Value.Count > 1)
                    {
                        smallestPackage = assemblyToPackageMapping.Value.OrderBy(l => l.GetFiles().Count()).FirstOrDefault();
                        _console.WriteLine(String.Format("{0} : Choosing {1} from {2} choices.", assemblyToPackageMapping.Key, smallestPackage.Id, assemblyToPackageMapping.Value.Count()));
                    }
                    else
                    {
                        smallestPackage = assemblyToPackageMapping.Value.First();
                    }
                    //Only add if we do not have another instance of the ID, not the id/version combo....
                    if (!prf.GetPackageReferences().Any(p => p.Id == smallestPackage.Id))
                        prf.AddEntry(smallestPackage.Id, smallestPackage.Version);
                }
            }
            else
            {
                _console.WriteError("Please move the existing packages.config file....");
            }
        }

        private Dictionary<string, List<IPackage>> GetResolvedMappings()
        {
            return _assemblyToPackageMapping.Where(assemblyToPackageMapping => assemblyToPackageMapping.Value.Any()).ToDictionary(m => m.Key, m=> m.Value);
        }

        private IList<string> GetFailedMappings()
        {
            return _assemblyToPackageMapping.Where(assemblyToPackageMapping => !assemblyToPackageMapping.Value.Any()).Select(mapping => mapping.Key).ToList();
        }
    }
}