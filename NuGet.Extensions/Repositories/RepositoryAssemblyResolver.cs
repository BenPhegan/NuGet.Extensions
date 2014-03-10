using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NuGet.Common;

namespace NuGet.Extensions.Repositories
{
    /// <summary>
    /// Provides the ability to search across IQueryable package sources for a set of packages that contain a particular assembly or set of assemblies.
    /// </summary>
    public class RepositoryAssemblyResolver
    {
        readonly HashSet<string> _assemblies = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        readonly IQueryable<IPackage> _packageSource;
        private readonly IFileSystem _fileSystem;
        private readonly IConsole _console;
        private readonly Dictionary<string, List<IPackage>> _resolvedAssemblies;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryAssemblyResolver"/> class.
        /// </summary>
        /// <param name="assemblies">The assemblies to look for.</param>
        /// <param name="packageSource">The package sources to search.</param>
        /// <param name="fileSystem">The file system to output any packages.config files.</param>
        /// <param name="console">The console to output to.</param>
        public RepositoryAssemblyResolver(List<string> assemblies, IQueryable<IPackage> packageSource, IFileSystem fileSystem, IConsole console)
        {
            _packageSource = packageSource;
            _fileSystem = fileSystem;
            _console = console;

            foreach (var assembly in assemblies.Where(assembly => !_assemblies.Add(assembly)))
            {
                console.WriteWarning("Same assembly resolution will be used for both assembly references to {0}", assembly);
            }
            _resolvedAssemblies = _assemblies.ToDictionary(a => a, _ => new List<IPackage>());
        }
        
        /// <summary>
        /// Resolves a list of packages that contain the assemblies requested.
        /// </summary>
        /// <param name="exhaustive">if set to <c>true</c> [exhaustive].</param>
        /// <returns></returns>
        public AssemblyToPackageMapping GetAssemblyToPackageMapping(Boolean exhaustive)
        {
            IPackage currentPackage = null;
            int current = 0;
            int max = _packageSource.Count();

            foreach (var filenamePackage in GetFilenamePackagePairs())
            {
                if (currentPackage != filenamePackage.Value) _console.WriteLine("Checking package {0} of {1}", current++, max);

                var currentFilename = filenamePackage.Key;
                currentPackage = filenamePackage.Value;

                if (_assemblies.Contains(currentFilename))
                {
                    _resolvedAssemblies[currentFilename].Add(currentPackage);
                    //HACK Exhaustive not easy with multiple assemblies, so default to only one currently....
                    if (!exhaustive && _assemblies.Count == 1) break;
                }
            }
            return new AssemblyToPackageMapping(_console, _fileSystem, _resolvedAssemblies);
        }

        private IEnumerable<KeyValuePair<string, IPackage>> GetFilenamePackagePairs()
        {
            foreach (var package in _packageSource)
            {
                var files = package.GetFiles();
                foreach (var file in files) yield return new KeyValuePair<string, IPackage>(new FileInfo(file.Path).Name, package);
            }
        }
    }
}
