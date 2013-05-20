namespace NuGet.Extensions.Caches
{
    /// <summary>
    /// Provides a management layer over the MachineCache cache location.
    /// </summary>
    public interface INuGetCacheManager
    {
        /// <summary>
        /// Resets the %NuGetCachePath% to the previous setting.
        /// </summary>
        void ResetPreviousCacheDirectory();

        /// <summary>
        /// Sets the cache subdirectory to the source string provided.  Saves any existing NuGetCachePath value so it can be reset later.
        /// </summary>
        /// <param name="source"></param>
        void SetFeedSpecificCacheDirectory(string source);

        /// <summary>
        /// Provides the location of the current feed specific cache location.
        /// </summary>
        /// <returns></returns>
        string GetCurrentFeedSpecificCache();
    }
}