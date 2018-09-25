namespace NuGet.Extensions.Commands.GraphCommand
{
    public class SimplePackageInfoFactory : IPackageInfoFactory
    {
        public IPackageInfo From(IPackage package)
        {
            return new SimplePackageInfo(package);
        }

        public IPackageInfo From(PackageDependency dependency)
        {
            return new SimplePackageInfo(dependency);
        }
    }
}