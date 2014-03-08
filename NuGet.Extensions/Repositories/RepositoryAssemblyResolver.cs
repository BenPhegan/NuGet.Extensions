using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NuGet.Common;

namespace NuGet.Extensions.Repositories
{
    /// <summary>
    /// Provides the ability to search across IQueryable package sources for a set of packages that contain a particular assembly or set of assemblies.
    /// </summary>
    public class RepositoryAssemblyResolver
    {
        List<string> assemblies = new List<string>();
        IQueryable<IPackage> packageSource;
        Dictionary<string, List<IPackage>> resolvedAssemblies = new Dictionary<string, List<IPackage>>();
        IConsole Console;
        IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryAssemblyResolver"/> class.
        /// </summary>
        /// <param name="assemblies">The assemblies to look for.</param>
        /// <param name="packageSource">The package sources to search.</param>
        /// <param name="fileSystem">The file system to output any packages.config files.</param>
        /// <param name="console">The console to output to.</param>
        public RepositoryAssemblyResolver(List<string> assemblies, IQueryable<IPackage> packageSource, IFileSystem fileSystem, IConsole console)
        {
            this.assemblies = assemblies;
            this.packageSource = packageSource;
            this.Console = console;
            this.fileSystem = fileSystem;

            foreach (var a in assemblies)
            {
                resolvedAssemblies.Add(a, new List<IPackage>());
            }
        }

        /// <summary>
        /// Resolves a list of packages that contain the assemblies requested.
        /// </summary>
        /// <param name="exhaustive">if set to <c>true</c> [exhaustive].</param>
        /// <returns></returns>
        public Dictionary<string, List<IPackage>> ResolveAssemblies(Boolean exhaustive)
        {
            int current = 0;
            int max = packageSource.Count();

            foreach (var package in packageSource)
            {
                Console.WriteLine("Checking package {1} of {2}", package.Id, current++, max);
                var packageFiles = package.GetFiles();
                foreach (var f in packageFiles)
                {
                    FileInfo file = new FileInfo(f.Path);
                    foreach (var assembly in assemblies.Where(a => a.Equals(file.Name, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        resolvedAssemblies[assembly].Add(package);
                        //HACK Exhaustive not easy with multiple assemblies, so default to only one currently....
                        if (!exhaustive && assemblies.Count == 1)
                        {
                            return resolvedAssemblies;
                        }
                    }
                }
            }
            return resolvedAssemblies;
        }


        /// <summary>
        /// Outputs a package.config file reflecting the set of packages that provides the requested set of assemblies.
        /// </summary>
        public void OutputPackageConfigFile()
        {
            if (fileSystem.FileExists("packages.config"))
                fileSystem.DeleteFile("packages.config");

            if (!fileSystem.FileExists("packages.config"))
            {
                var prf = new PackageReferenceFile(fileSystem,".\\packages.config");
                foreach (var assemblyToPackageMapping in resolvedAssemblies)
                {
                    if (assemblyToPackageMapping.Value.Count() > 0)
                    {
                        IPackage smallestPackage;
                        if (assemblyToPackageMapping.Value.Count > 1)
                        {
                            smallestPackage = assemblyToPackageMapping.Value.OrderBy(l => l.GetFiles().Count()).FirstOrDefault();
                            Console.WriteLine(String.Format("{0} : Choosing {1} from {2} choices.", assemblyToPackageMapping.Key, smallestPackage.Id, assemblyToPackageMapping.Value.Count()));
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
            }
            else
            {
                Console.WriteError("Please move the existing packages.config file....");
            }
        }

    }


}
