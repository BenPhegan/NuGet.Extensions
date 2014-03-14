using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace NuGet.Extensions.ReferenceAnalysers
{
    public class HintPathGenerator : IHintPathGenerator
    {
        private readonly bool _useVersionedPackageHintPath;

        public HintPathGenerator(bool useVersionedPackageHintPath = true)
        {
            _useVersionedPackageHintPath = useVersionedPackageHintPath;
        }

        public string ForAssembly(DirectoryInfo solutionDir, DirectoryInfo projectDir, IPackage package, string assemblyFilename)
        {
            var fileLocation = GetFileLocationFromPackage(package, assemblyFilename);
            var packageDirectory = _useVersionedPackageHintPath ? string.Format("{0}.{1}", package.Id, package.Version) : package.Id;
            var newHintPathFull = Path.Combine(solutionDir.FullName, "packages", packageDirectory, fileLocation);
            var newHintPathRelative = GetRelativePath(projectDir.FullName + Path.DirectorySeparatorChar, newHintPathFull);
            return newHintPathRelative;
        }

        private static String GetRelativePath(string rootWithTrailingSlash, string childWithTrailingSlash)
        {
            // Validate paths.
            Contract.Assert(!String.IsNullOrEmpty(rootWithTrailingSlash));
            Contract.Assert(!String.IsNullOrEmpty(childWithTrailingSlash));

            // Create Uris
            var rootUri = new Uri(rootWithTrailingSlash);
            var childUri = new Uri(childWithTrailingSlash);

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