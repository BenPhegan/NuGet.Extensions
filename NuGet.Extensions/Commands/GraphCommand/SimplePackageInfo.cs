namespace NuGet.Extensions.Commands.GraphCommand
{
    public class SimplePackageInfo : IPackageInfo
    {
        protected string Id { get; }

        public SimplePackageInfo(IPackage package)
        {
            Id = GetId(package);
            Details = Id;
        }

        public SimplePackageInfo(PackageDependency to)
        {
            Id = GetId(to);
            Details = Id;
        }

        public string TargetInfo { get; protected set; }
        public string Details { get; set; }

        public override string ToString()
        {
            return Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj) || obj == null)
            {
                return true;
            }

            SimplePackageInfo other = obj as SimplePackageInfo;
            return other != null && other.Id == Id;
        }

        public virtual bool Is(IPackage package)
        {
            return Id == GetId(package);
        }

        public virtual bool Is(PackageDependency dependency)
        {
            return Id == GetId(dependency);
        }

        private static string GetId(PackageDependency dependency)
        {
            return dependency.Id.ToLowerInvariant();
        }
        private static string GetId(IPackage package)
        {
            return package.Id.ToLowerInvariant();
        }

    }
}