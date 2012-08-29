using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Extensions.FeedAudit
{
    public class PackageAuditEventArgs : EventArgs
    {
        public IPackage Package { get; set; }
    }
}
