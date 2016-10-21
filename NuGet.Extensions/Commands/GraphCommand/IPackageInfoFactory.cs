namespace NuGet.Extensions.Commands.GraphCommand
{
    public interface IPackageInfoFactory
    {
        IPackageInfo From(IPackage package);
        IPackageInfo From(PackageDependency dependency);
    }
}