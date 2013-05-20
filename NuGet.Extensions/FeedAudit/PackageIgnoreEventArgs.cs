using System;

namespace NuGet.Extensions.FeedAudit
{
    public class PackageIgnoreEventArgs : EventArgs
    {
        public IPackage IgnoredPackage { get; set; }
        public bool Wildcard { get; set; }
        public bool StringMatch { get; set; }
    }
}
