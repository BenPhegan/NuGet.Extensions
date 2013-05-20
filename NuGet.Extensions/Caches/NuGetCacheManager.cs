using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using NuGet.Common;

namespace NuGet.Extras.Caches
{
    /// <summary>
    /// Provides management capability around NuGetCachePath so feed specific caches can be set.
    /// </summary>
    public class NuGetCacheManager : INuGetCacheManager
    {
        private string _previousCacheDirectory;
        private readonly IConsole Console;
        private const string DefaultCache = @"NuGet\Cache";
        private const string CacheEnvironmentVariable = "NuGetCachePath";
        private string _currentFeedSpecificCache;



        /// <summary>
        /// Creates a NuGetCacheManager, provided an IConsole implementation
        /// </summary>
        /// <param name="console"></param>
        public NuGetCacheManager(IConsole console)
        {
            //HACK this throws on instantiation.....?????
            Contract.Assert(console != null);
            Console = console;
        }

        /// <summary>
        /// Gets the LocalAppData path, firstly from the environment variable, falling back to SpecialFolder
        /// </summary>
        /// <returns></returns>
        private static string GetLocalAppDataPath()
        {
            var localAppDataEnvironment = Environment.GetEnvironmentVariable("LocalAppData");
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var basePath = !String.IsNullOrEmpty(localAppDataEnvironment)
                               ? localAppDataEnvironment
                               : localAppDataPath;
            return basePath;
        }

        /// <summary>
        /// Resets the %NuGetCachePath% to the previous setting.
        /// </summary>
        public void ResetPreviousCacheDirectory()
        {
            if (!String.IsNullOrEmpty(_previousCacheDirectory))
            {
                Environment.SetEnvironmentVariable(CacheEnvironmentVariable, _previousCacheDirectory);
            }
        }

        /// <summary>
        /// Sets the cache subdirectory to the source string provided.  Saves any existing NuGetCachePath value so it can be reset later.
        /// </summary>
        /// <param name="source"></param>
        public void SetFeedSpecificCacheDirectory(string source)
        {
            if (!string.IsNullOrEmpty(source))
            {
                var basePath = GetLocalAppDataPath();
                if (String.IsNullOrEmpty(basePath))
                {
                    Console.WriteWarning("Could not find your LocalAppData...caching isn't going to work too well...");
                    Console.WriteWarning("Try setting NuGetCachePath environment variable explicitly");
                    return;
                }
                var currentSetting = Environment.GetEnvironmentVariable(CacheEnvironmentVariable);

                //If we have it set, we should override and replace when we exit....
                if (!String.IsNullOrEmpty(currentSetting))
                {
                    _previousCacheDirectory = currentSetting;
                }

                basePath = Path.Combine(basePath, DefaultCache);
                var cacheSubDir = ResolveCacheSubDirectoryName(basePath, source);
                var fullCacheDirectory = Path.Combine(basePath, cacheSubDir);
                Environment.SetEnvironmentVariable(CacheEnvironmentVariable, fullCacheDirectory);
                _currentFeedSpecificCache = fullCacheDirectory;
                Console.WriteLine("Using a feed specific cache subdirectory : {0}", fullCacheDirectory);
            }
        }

        /// <summary>
        /// Provides the location of the current feed specific cache location.
        /// </summary>
        /// <returns></returns>
        public string GetCurrentFeedSpecificCache()
        {
            return _currentFeedSpecificCache ?? Environment.GetEnvironmentVariable(CacheEnvironmentVariable) ?? string.Empty;
        }

        private string ResolveCacheSubDirectoryName(string basePath, string source)
        {
            //Check to ensure that the source can be used as part of a path...
            try
            {
                Path.GetFullPath(Path.Combine(basePath, source));
                return source;
            }
            catch
            {
            }

            //If not, try and get host part of the URL
            try
            {
                Uri sourceUri;
                if (Uri.TryCreate(source, UriKind.RelativeOrAbsolute, out sourceUri))
                {
                    return sourceUri.Host;
                }
            }
            catch
            {
            }

            //If not, use its hash!
            return source.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }
    }
}