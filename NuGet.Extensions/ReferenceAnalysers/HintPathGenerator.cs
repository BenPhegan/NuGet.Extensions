using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace NuGet.Extensions.ReferenceAnalysers
{
    public class HintPathGenerator : IHintPathGenerator
    {
        public HintPathGenerator()
        {}

        public string ForAssembly(DirectoryInfo solutionDir, DirectoryInfo projectDir, IPackage package, string assemblyFilename)
        {
            var fileLocation = GetFileLocationFromPackage(package, assemblyFilename);
            //TODO make version available, currently only works for non versioned package directories...
            var newHintPathFull = Path.Combine(solutionDir.FullName, "packages", package.Id, fileLocation);
            var newHintPathRelative = GetRelativePath(projectDir.FullName, newHintPathFull);
            return newHintPathRelative;
        }

        private static String GetRelativePath(string root, string child)
        {
            // Validate paths.
            Contract.Assert(!String.IsNullOrEmpty(root));
            Contract.Assert(!String.IsNullOrEmpty(child));

            // Create Uris
            var rootUri = new Uri(root);
            var childUri = new Uri(child);

            // Get relative path.
            var relativeUri = rootUri.MakeRelativeUri(childUri);

            // Clean path and return.
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private string GetFileLocationFromPackage(IPackage package, string key)
        {
            return (from fileLocation in package.GetFiles()
                where fileLocation.Path.ToLowerInvariant().EndsWith(key, StringComparison.OrdinalIgnoreCase)
                select fileLocation.Path).FirstOrDefault();
        }
    }
}