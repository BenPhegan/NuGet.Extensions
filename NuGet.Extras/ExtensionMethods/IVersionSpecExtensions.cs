using System;

namespace NuGet.Extras.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on IVersionSpec
    /// </summary>
    public static class IVersionExtensions
    {
        /// <summary>
        /// Stolen from NuGet codebase.
        /// </summary>
        /// <param name="versionInfo"></param>
        /// <returns></returns>
        public static Func<IPackage, bool> ToDelegate(this IVersionSpec versionInfo)
        {
            return versionInfo.ToDelegate<IPackage>(p => p.Version);
        }

        /// <summary>
        /// Stolen from NuGet codebase.
        /// </summary>
        /// <param name="versionInfo"></param>
        /// <param name="extractor"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Func<T, bool> ToDelegate<T>(this IVersionSpec versionInfo, Func<T, SemanticVersion> extractor)
        {
            return p =>
            {
                SemanticVersion version = extractor(p);
                bool condition = true;
                if (versionInfo.MinVersion != null)
                {
                    if (versionInfo.IsMinInclusive)
                    {
                        condition = condition && version >= versionInfo.MinVersion;
                    }
                    else
                    {
                        condition = condition && version > versionInfo.MinVersion;
                    }
                }

                if (versionInfo.MaxVersion != null)
                {
                    if (versionInfo.IsMaxInclusive)
                    {
                        condition = condition && version <= versionInfo.MaxVersion;
                    }
                    else
                    {
                        condition = condition && version < versionInfo.MaxVersion;
                    }
                }

                return condition;
            };
        }

        /// <summary>
        /// Determines if the specified version is within the version spec
        /// </summary>
        public static bool Satisfies(this IVersionSpec versionSpec, SemanticVersion version)
        {
            // The range is unbounded so return true
            if (versionSpec == null)
            {
                return true;
            }
            return versionSpec.ToDelegate<SemanticVersion>(v => v)(version);
        }
    }
}
