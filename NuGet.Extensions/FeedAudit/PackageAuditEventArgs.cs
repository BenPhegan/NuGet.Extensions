using System;

namespace NuGet.Extensions.FeedAudit
{
    public class PackageAuditEventArgs : EventArgs
    {
        public IPackage Package { get; set; }
    }
}
