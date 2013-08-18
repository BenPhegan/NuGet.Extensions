using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Extensions.BaseClasses;

namespace NuGet.Extensions.Commands
{
    [Command(typeof (CopyResources), "copy", "Description", MinArgs = 1, MaxArgs = 7, UsageSummaryResourceName = "UsageSummary", UsageDescriptionResourceName = "UsageDescription")]
    public class Copy : TwoWayCommand
    {
        [ImportingConstructor]
        public Copy() {}

        [DefaultValue(true)]
        [Option(typeof (CopyResources), "RecursiveDescription", AltName = "r")]
        public bool Recursive { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        [Option(typeof(CopyResources), "VersionDescription", AltName = "v")]
        public string Version { get; set; }

        /// <summary>
        /// Executes the command.
        /// </summary>
        protected override void ExecuteSub() {
            string packageId = Arguments[0];

            Console.WriteLine("Copying {0}{1} from {2} to {3}.",
                              string.IsNullOrEmpty(Version) ? packageId : packageId + " " + Version,
                              Recursive ? " and all of its dependent packages" : string.Empty,
                              Sources.Count == 0 ? "any source" : string.Join(";", Sources), string.Join(";", Destinations));

            if (Recursive)
                InstallPackageLocally(packageId, WorkDirectory);
            else {
                GetPackageLocally(packageId, Version, WorkDirectory);
            }

            foreach (string dest in Destinations) {
                PrepareApiKey(dest);
                IList<string> packagePaths = GetPackages(WorkDirectory, GetSearchFilter(Recursive, packageId, Version));
                PushToDestination(WorkDirectory, dest, packagePaths);
            }
        }

        private void GetPackageLocally(string packageId, string version, string workDirectory) {
            //Add the default source if there are none present
            var workingFileSystem = new PhysicalFileSystem(workDirectory);

            foreach (string source in Sources) {
                Uri uri;
                if (Uri.TryCreate(source, UriKind.Absolute, out uri)) {
                    AggregateRepository repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(PackageRepositoryFactory.Default, CreateSourceProvider(new[] {source}), new[] {source});

                    IPackage package;
                    if (repository.TryFindPackage(packageId, new SemanticVersion(version), out package)) {
                        Console.WriteLine("Attempting to download package {0}.{1} via {2}", packageId, version, uri.ToString());

                        try {
                            string filepath = Path.Combine(workDirectory, package.Id + "-" + package.Version + ".nupkg");
                            workingFileSystem.AddFile(filepath, package.GetStream());
                            break;
                        }
                        catch (Exception e) {
                            Console.WriteError(e);
                        }
                    }
                }
            }
        }

        private static string GetSearchFilter(bool recursive, string packageId, string version) {
            return recursive ? "*.nupkg" : string.Format("{0}-{1}.nupkg", packageId, version);
        }

        protected override void PrepareSources() {
            if (Sources.Count == 0) {
                Sources.Add(".");
            }

            for (int i = 0; i < Sources.Count; i++) {
                if (IsDirectory(Sources[i])) {
                    //destination is current directory
                    if (string.IsNullOrWhiteSpace(Sources[i]) || Sources[i] == ".") {
                        Sources[i] = Directory.GetCurrentDirectory();
                    }

                    // not a UNC Path
                    if (!Sources[i].StartsWith(@"\\")) {
                        Sources[i] = Path.GetFullPath(Sources[i]);
                    }
                }
            }
        }

        protected override void PrepareDestinations() {
            if (Destinations.Count == 0) {
                Destinations.Add(".");
            }

            for (int i = 0; i < Destinations.Count; i++) {
                if (IsDirectory(Destinations[i])) {
                    //destination is current directory
                    if (string.IsNullOrWhiteSpace(Destinations[i]) || Destinations[i] == ".") {
                        Destinations[i] = Directory.GetCurrentDirectory();
                    }

                    // not a UNC Path
                    if (!Destinations[i].StartsWith(@"\\")) {
                        Destinations[i] = Path.GetFullPath(Destinations[i]);
                    }
                }
            }
        }


        private void PrepareApiKey(string destination) {
            if (!IsDirectory(destination)) {
                if (string.IsNullOrEmpty(ApiKey)) {
                    ApiKey = GetApiKey(SourceProvider, Settings, destination, true);
                }
            }
        }

        private void InstallPackageLocally(string packageId, string workDirectory) {
            var install = new InstallCommand();
            install.Arguments.Add(packageId);
            install.OutputDirectory = workDirectory;
            install.Console = Console;
            foreach (string source in Sources) {
                install.Source.Add(source);
            }
            if (!string.IsNullOrEmpty(Version)) {
                install.Version = Version;
            }

            install.ExecuteCommand();
        }

        private void PushToDestination(string workDirectory, string destination, IEnumerable<string> packagePaths) {
            foreach (var packagePath in packagePaths) {
                if (IsDirectory(destination)) {
                    PushToDestinationDirectory(packagePath, destination);
                }
                else {
                    PushToDestinationRemote(packagePath, destination);
                }
            }
        }

        private IList<string> GetPackages(string workDirectory, string searchFilter) {
            return Directory.GetFiles(workDirectory, searchFilter, SearchOption.AllDirectories);
        }


        private void PushToDestinationDirectory(string packagePath, string destination) {
            File.Copy(Path.GetFullPath(packagePath), Path.Combine(destination, Path.GetFileName(packagePath)), true);
            Console.WriteLine("Completed copying '{0}' to '{1}'", Path.GetFileName(packagePath), destination);
        }

        private void PushToDestinationRemote(string packagePath, string destination) {
            try
            {
                PushPackage(Path.GetFullPath(packagePath), destination, ApiKey);
            }
            catch (Exception ex) {
                Console.WriteLine("Copy encountered an issue. Perhaps the package already exists? {0}{1}", Environment.NewLine, ex);
            }
        }

        #region Push Command Not Working

        private static readonly string ApiKeysSectionName = "apikeys";

        private static string GetApiKey(IPackageSourceProvider sourceProvider, ISettings settings, string source, bool throwIfNotFound) {
            string apiKey = settings.GetDecryptedValue(ApiKeysSectionName, source);
            //HACK no pretty source name, as they have made the call to  CommandLineUtility.GetSourceDisplayName(source) internal
            if (string.IsNullOrEmpty(apiKey) && throwIfNotFound)
            {
                throw new CommandLineException(
                    "No API Key was provided and no API Key could be found for {0}. To save an API Key for a source use the 'setApiKey' command.",
                    new object[] {source});
            }
            return apiKey;
        }

        private void PushPackage(string packagePath, string source, string apiKey) {
            var packageServer = new PackageServer(source, "NuGet Command Line");

            // Push the package to the server
            var package = new ZipPackage(packagePath);

            //HACK no pretty source name, as they have made the call to  CommandLineUtility.GetSourceDisplayName(source) internal
            Console.WriteLine("Pushing {0} to {1}", package.GetFullName(), source);

            try 
            {
                using (package.GetStream())
                {
                    packageServer.PushPackage(apiKey, package, 60000);
                }
            }
            catch 
            {
                Console.WriteLine();
                throw;
            }
        }

        #endregion
    }
}