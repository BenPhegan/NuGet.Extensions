using System;
using System.Collections.Generic;
using System.IO;
using NuGet.Commands;
using NuGet.Extensions.ExtensionMethods;

namespace NuGet.Extensions.BaseClasses
{
    /// <summary>
    /// Provides base functionality for commands that handle two repositories.
    /// </summary>
    public abstract class TwoWayCommand : Command
    {
        private readonly IList<string> _packageList;

        /// <summary>
        /// The working directory.
        /// </summary>
        protected string WorkDirectory;
        private IPackageSourceProvider _destinationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwoWayCommand"/> class.
        /// </summary>
        /// <param name="repositoryFactory">The repository factory.</param>
        /// <param name="sourceProvider">The source provider.</param>
        protected TwoWayCommand(IPackageRepositoryFactory repositoryFactory, IPackageSourceProvider sourceProvider)
        {
            RepositoryFactory = repositoryFactory;
            SourceProvider = sourceProvider;
            Sources = new List<string>();
            Destinations = new List<string>();
            _packageList = new List<string>();
        }

        /// <summary>
        /// Gets the package list.
        /// </summary>
        protected IList<string> PackageList
        {
            get { return _packageList; }
        }

        /// <summary>
        /// Gets the destination provider.
        /// </summary>
        protected IPackageSourceProvider DestinationProvider
        {
            get { return _destinationProvider; }
        }

        /// <summary>
        /// Gets the source.
        /// </summary>
        /// <value>
        /// The sources.
        /// </value>
        [Option(typeof(TwoWayResources), "SourceDescription", AltName = "src")]
        public IList<string> Sources { get; set; }

        /// <summary>
        /// Gets the destination.
        /// </summary>
        /// <value>
        /// The destinations.
        /// </value>
        [Option(typeof(TwoWayResources), "DestinationDescription", AltName = "dest")]
        public IList<string> Destinations { get; set; }

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        /// <value>
        /// The API key.
        /// </value>
        [Option(typeof(TwoWayResources), "ApiKeyDescription", AltName = "api")]
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the working directory root.
        /// </summary>
        /// <value>
        /// The working directory root.
        /// </value>
        [Option(typeof(TwoWayResources), "WorkingDirectoryRootDescription", AltName = "workroot")]
        public string WorkingDirectoryRoot { get; set; }

        /// <summary>
        /// Prepares the sources.
        /// </summary>
        protected abstract void PrepareSources();

        /// <summary>
        /// Prepares the destinations.
        /// </summary>
        protected abstract void PrepareDestinations();

        /// <summary>
        /// Deletes the work directory.
        /// </summary>
        protected void DeleteWorkDirectory()
        {
            if (Directory.Exists(WorkDirectory))
            {
                Directory.Delete(WorkDirectory, true);
            }
        }

        /// <summary>
        /// Prepares the work directory.
        /// </summary>
        protected void PrepareWorkDirectory()
        {
            DeleteWorkDirectory();
            Directory.CreateDirectory(WorkDirectory);
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        public override void ExecuteCommand()
        {
            //Console.WriteLine(string.Format("Preparing work directory: {0}", WorkDirectory));
            WorkDirectory = Path.Combine(string.IsNullOrEmpty(WorkingDirectoryRoot) ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) : WorkingDirectoryRoot, "NugetExtensionWork");
            PrepareWorkDirectory();

            PrepareSources();
            PrepareDestinations();
            PreventApiKeyBeingSpecifiedWhenMultipleRemoteSources();

            SourceProvider = CreateSourceProvider(Sources);
            _destinationProvider = CreateSourceProvider(Destinations);

            // invoke the override in the actual command
            ExecuteSub();

            //Console.WriteLine(string.Format("Cleaning up work directory: {0}.", WorkDirectory));
            DeleteWorkDirectory();
        }

        /// <summary>
        /// Executes the sub command.
        /// </summary>
        protected abstract void ExecuteSub();

        /// <summary>
        /// Creates the source provider.
        /// </summary>
        /// <param name="sources">The sources.</param>
        /// <param name="useDefaultFeed">if set to <c>true</c> [use default feed].</param>
        /// <returns></returns>
        protected IPackageSourceProvider CreateSourceProvider(IEnumerable<string> sources, bool useDefaultFeed = false)
        {
            return new PackageSourceProvider(new BlankUserSettings(), sources.AsPackageSourceList(useDefaultFeed));
        }

        /// <summary>
        /// Prevents the API key being specified when multiple remote sources.
        /// </summary>
        protected void PreventApiKeyBeingSpecifiedWhenMultipleRemoteSources()
        {
            int remoteCount = 0;
            foreach (string dest in Destinations)
            {
                if (!IsDirectory(dest))
                {
                    remoteCount += 1;
                }
            }
            if (!string.IsNullOrWhiteSpace(ApiKey) && remoteCount > 1)
            {
                throw new ApplicationException("ApiKey cannot be set if you specify multiple remote destinations. Please consider using nuget 'setApiKey' command and then running this command without the ApiKey parameter set.");
            }
        }

        /// <summary>
        /// Determines whether the specified destination is directory.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <returns>
        ///   <c>true</c> if the specified destination is directory; otherwise, <c>false</c>.
        /// </returns>
        protected bool IsDirectory(string destination)
        {
            return string.IsNullOrWhiteSpace(destination) || destination.Contains(@"\") || destination == ".";
        }
    }
}