using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using NuGet.Commands;
using NuGet.Extras;
using NuGet.Extras.Commands;
using NuGet.Extras.Comparers;
using NuGet.Extras.ExtensionMethods;

namespace NuGet.Extensions.Commands
{
    [Command(typeof(CloneResources), "clone", "Description", MinArgs = 0, MaxArgs = 6, UsageSummaryResourceName = "UsageSummary", UsageDescriptionResourceName = "UsageDescription")]
    public class Clone : TwoWayCommand
    {
        private IList<string> _tags = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Clone"/> class.
        /// </summary>
        /// <param name="repositoryFactory">The repository factory.</param>
        /// <param name="sourceProvider">The source provider.</param>
        [ImportingConstructor]
        public Clone(IPackageRepositoryFactory repositoryFactory, IPackageSourceProvider sourceProvider)
            : base(repositoryFactory, sourceProvider) { }

        /// <summary>
        /// Gets or sets a value indicating whether [all versions].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [all versions]; otherwise, <c>false</c>.
        /// </value>
        [DefaultValue(false)]
        [Option(typeof(CloneResources), "AllVersionsDescription", AltName = "a")]
        public bool AllVersions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we are just doing a [dry run].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [dry run]; otherwise, <c>false</c>.
        /// </value>
        [DefaultValue(false)]
        [Option(typeof(CloneResources), "DryRunDescription")]
        public bool DryRun { get; set; }

        /// <summary>
        /// Gets or sets the list of tags to download.
        /// </summary>
        /// <value>
        /// The tags used to select packages.
        /// </value>
        [Option(typeof(CloneResources), "TagsDescription")]
        public string Tags { get; set; }

        [Option("Gets a list of the Ids availabble on the destination, and only clones them")]
        public bool Refresh { get; set; }

        private IQueryable<string> _packageList;

        /// <summary>
        /// Prepares the sources.
        /// </summary>
        protected override void PrepareSources()
        {
            if (Sources.Count == 0)
            {
                Sources.Add(NuGetConstants.DefaultFeedUrl);
            }
        }

        /// <summary>
        /// Prepares the destinations.
        /// </summary>
        protected override void PrepareDestinations()
        {
            if (Destinations.Count == 0)
            {
                Destinations.Add(NuGetConstants.DefaultFeedUrl);
            }
        }

        /// <summary>
        /// Executes the sub command.
        /// </summary>
        protected override void ExecuteSub()
        {
            if (!string.IsNullOrEmpty(Tags))
            {
                _tags = Tags.Split(',').ToList();
            }

            Console.WriteLine(AllVersions ? "Cloning packages (full history)." : "Cloning packages (latest only).");
            if (DryRun)
                Console.WriteWarning("Dry run only! No packages will be downloaded, pushed or published.");

            //TODO: Move to base class?
            string packageId = base.Arguments.Count > 0 ? base.Arguments[0] : string.Empty;
            PopulateSourcePackageList(packageId);

            //Grab each package, get the full list of versions, and then call a Copy on each.
            //TODO Copy is currently using the default InstallCommand under the covers, which means this is a bit messy on the dependencies (ie it gets them all)
            foreach (string packageName in _packageList)
            {
                //Get the list of packages from both source and destination, and if we only have one destination then find the set difference
                IEnumerable<IPackage> sourcePackages = GetPackageList(AllVersions, packageName, SourceProvider);
                IEnumerable<IPackage> packagesToCopy = sourcePackages;

                Console.WriteLine("{0}", packageName);
                if (sourcePackages.Count() == 0)
                {
                    Console.WriteError("Package Id: \"{0}\" not found in any sources.  Skipping.", packageName);
                    Console.WriteLine();
                    continue;
                }
                OutputCountToConsole("Source:", sourcePackages.Count(), sourcePackages);

                //TODO we could probably do this down in the copy command...Rob, thoughts?
                if (Destinations.Count == 1)
                {
                    IEnumerable<IPackage> destinationPackages = GetPackageList(true, packageName, DestinationProvider);

                    packagesToCopy = sourcePackages.Except(destinationPackages, GetIPackageLambdaComparer());
                    OutputCountToConsole("Destination:", destinationPackages.Count(), destinationPackages);
                }

                OutputCountToConsole("Copying:", packagesToCopy.Count(), packagesToCopy);
                //Console.WriteLine(string.Format("Found: {0} versions of {1} to copy.{2}", packagesToCopy.Count(), packageName, packagesToCopy.Count() == 0 ? " All packages synced!" : string.Empty));
                Console.WriteLine();
                foreach (var package in packagesToCopy)
                {
                    if (!DryRun)
                    {
                        ExecuteCopyAction(SourceProvider, package);
                        Console.WriteLine();
                    }
                }
            }
        }

        private void PopulateSourcePackageList(string packageId)
        {
            if (!string.IsNullOrEmpty(packageId))
            {
                _packageList = new EnumerableQuery<string>(new List<string>(){packageId});
            }
            else
            {
                //or get the full list from the source, and go from there....
                //REVIEW: this is a potential bottleneck - maybe split out in to batch call
                var sourceList = GetPackageList(false, string.Empty, SourceProvider, _tags).Select(p => p.Id);
                _packageList = Refresh ? GetPackageList(false, string.Empty, DestinationProvider, _tags).Select(p => p.Id) : sourceList;
                //_packageList = Refresh ? GetPackageList(false, string.Empty, DestinationProvider, _tags).Select(p => p.Id) 
                //                       : GetPackageList(false, string.Empty, SourceProvider, _tags).Select(p => p.Id);
            }
        }

        private void OutputCountToConsole(string message, int count)
        {
            Console.WriteLine(string.Format(message.PadRight(14) + count).PadLeft(18));
        }

        private void OutputCountToConsole(string message, int count, IEnumerable<IPackage> list)
        {
            var packageVersionList =  list.Count() > 0 ? string.Format( "    (" +string.Join(", ", list.Select(x => x.Version)) + ")") : string.Empty;

            Console.WriteLine(string.Format(message.PadRight(14) + count).PadLeft(18) + packageVersionList);
        }

        private static LambdaComparer<IPackage> GetIPackageLambdaComparer()
        {
            return new LambdaComparer<IPackage>(
                                    (a, b) => a.Id == b.Id && a.Version.ToString() == b.Version.ToString(),
                                    (a) => a.Id.GetHashCode() + a.Version.ToString().GetHashCode());
        }

        private void ExecuteCopyAction(IPackageSourceProvider realSourceProvider, IPackage package)
        {
            var copyCommand = new Copy(RepositoryFactory, SourceProvider)
            {
                ApiKey = ApiKey,
                Destinations = Destinations,
                Sources = Sources,
                Version = package.Version.ToString(),
                Console = Console,
                Recursive = false,
                WorkingDirectoryRoot = WorkingDirectoryRoot
            };
            copyCommand.Arguments.Add(package.Id);
            copyCommand.Execute();

        }

        //HACK Does this need to be here?
        private IPackageSourceProvider CreateSourceProvider(IEnumerable<string> sources, bool useDefaultFeed = false)
        {
            return new PackageSourceProvider(new BlankUserSettings(), sources.AsPackageSourceList(useDefaultFeed));
        }

        public IEnumerable<IPackage> GetPackageList(bool allVersions, string id, IPackageSourceProvider sourceProvider)
        {
            return GetPackageList(allVersions, id, sourceProvider, null);
        }


        public IQueryable<IPackage> GetPackageList(bool allVersions, string id, IPackageSourceProvider sourceProvider, IEnumerable<string> tags)
        {
            bool singular = !string.IsNullOrEmpty(id);

            //HACK perhaps we should be passing in the tags as well here (additional to the id). This would narrow down the number of packages
            //returned....
            IEnumerable<IPackage> packages;

            //Check for tags
            if (tags != null && tags.Count() > 0)
            {
                packages = GetPackagesByTag(allVersions, id, sourceProvider, tags);
                LogPackageList(packages, tags);
            }
            else
            {
                packages = GetInitialPackageList(allVersions, new[] { id }.ToList(), sourceProvider);
            }

            //listcommand doesnt return just the matching packages, so filter here...
            if (singular)
                return packages.Where(p => p.Id.ToLowerInvariant() == id.ToLowerInvariant()).AsQueryable();
            else
                return packages.AsQueryable();
        }

        private void LogPackageList(IEnumerable<IPackage> packages, IEnumerable<string> tags)
        {
            Console.WriteLine("Using tags:".PadRight(15) + string.Join(" ", tags).PadLeft(15));
            Console.WriteLine();

            var maxIdLength = packages.Max(x => x.Id.Length);

            Console.WriteLine("Package Id".PadRight(maxIdLength + 4) + "Tags");
            Console.WriteLine("----------".PadRight(maxIdLength + 4) + "----");

            foreach (var p in packages.Distinct())
            {
                Console.WriteLine(p.Id.PadRight(maxIdLength + 3) + string.Join(" ", p.Tags));
            }

            Console.WriteLine();
        }

        private IEnumerable<IPackage> GetPackagesByTag(bool allVersions, string id, IPackageSourceProvider sourceProvider, IEnumerable<string> tags)
        {
            IEnumerable<IPackage> packages;
            //Where we have tags on a package that include one of the tags we are looking for, include it...
            tags.ToList().Add(id);
            packages = GetInitialPackageList(allVersions, tags.ToList(), sourceProvider);
            //Check them, as the list command adds packages regardless of where the search term occurs...
            packages = packages.Where(p => p.Tags != null && p.Tags.Count() > 0 ? Clone.ParseTags(p.Tags).Any(t => tags.Contains(t)) : false);
            return packages;
        }

        //REVIEW Just in case we want to get away from using their list command....
        private IEnumerable<IPackage> GetInitialPackageList(bool allVersions, List<string> ids, IPackageSourceProvider sourceProvider)
        {
            //var listCommand = new ListCommand(RepositoryFactory, sourceProvider)
            //{
            //    AllVersions = allVersions,
            //    Console = this.Console,
            //};

            //if (ids != null && ids.Count != 0)
            //    listCommand.Arguments.AddRange(ids);
            //var packages = listCommand.GetPackages();

            List<IPackage> packages = new List<IPackage>();

            var repo = sourceProvider.GetAggregate(RepositoryFactory);
            if (ids != null && ids.Count != 0 && !string.IsNullOrEmpty(ids.First()))
            {
                foreach (var id in ids)
                {
                    if (allVersions)
                    {
                        packages.AddRange(repo.FindPackagesById(id));
                    }
                    else
                    {
                        packages.Add(repo.FindLatestPackage(id));
                    }
                }
            }
            else
            {
                return repo.Search("", false).Where(p => p.IsLatestVersion).ToList();
            }

            return packages;
        }


        //HACK stolen straight from NuGet.PackageBuilder.ParseTags...cause they made it private and its useful.
        /// <summary>
        /// Tags come in this format. tag1 tag2 tag3 etc..
        /// </summary>
        private static IEnumerable<string> ParseTags(string tags)
        {
            Debug.Assert(tags != null);
            return from tag in tags.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                   select tag.Trim();
        }
    }
}