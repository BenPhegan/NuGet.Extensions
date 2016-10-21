namespace NuGet.Extensions.Commands.GraphCommand
{
    public interface IPackageInfo
    {
        bool Is(PackageDependency dependency);
        bool Is(IPackage package);

        string TargetInfo { get; }
        string Details { get; set; }
    }
}