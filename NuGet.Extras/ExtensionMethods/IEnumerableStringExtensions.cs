using System.Collections.Generic;
using System.Linq;

namespace NuGet.Extras
{
    /// <summary>
    /// Mwahahahaaaahaaaaaaaaa
    /// </summary>
    public static class NuGetExtensions
    {
        /// <summary>
        /// Filthy extension method.  Converts the given list of package sources into a list of, well, PackageSource objects.
        /// </summary>
        /// <param name="sources">The sources.</param>
        /// <param name="useDefaultFeed">if set to <c>true</c> [use default feed].</param>
        /// <returns></returns>
        public static IEnumerable<PackageSource> AsPackageSourceList(this IEnumerable<string> sources, bool useDefaultFeed = false)
        {
            List<PackageSource> sourceList = sources.Select(d => new PackageSource(d)).ToList();
            if (useDefaultFeed) sourceList.Add(new PackageSource(NuGetConstants.DefaultFeedUrl));
            return sourceList;
        }
    }
}