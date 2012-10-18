using System;

namespace NuGet.Extensions.FeedAudit
{
    [Flags]
    public enum AuditEventTypes
    {
        ResolvedAssemblyReferences = 1,
        UnloadablePackageFiles = 2,
        UnresolvedAssemblyReferences = 4,
        UnresolvedDependencies = 8,
        UnusedPackageDependencies = 16,
        UsedPackageDependencies = 32,
        FeedResolvableReferences = 64,
        FeedUnresolvableReferences = 128,
        GacResolvableReferences = 256
    }
}