using System.Collections.Generic;
using System.Reflection;

namespace NuGet.Extensions.FeedAudit
{
    public class AssemblyNameEqualityComparer : IEqualityComparer<AssemblyName>
    {
        public bool Equals(AssemblyName x, AssemblyName y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
            return x.FullName == y.FullName;
        }

        public int GetHashCode(AssemblyName obj)
        {
            if (ReferenceEquals(obj, null)) return 0;
            return obj.FullName.GetHashCode();
        }
    }
}