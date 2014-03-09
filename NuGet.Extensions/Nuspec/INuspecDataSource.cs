namespace NuGet.Extensions.Commands
{
    public interface INuspecDataSource
    {
        string ProjectUrl { get; set; }
        string LicenseUrl { get; set; }
        string IconUrl { get; set; }
        string Tags { get; set; }
        string ReleaseNotes { get; set; }
        string Description { get; set; }
        string Id { get; set; }
        string Title { get; set; }
        string Author { get; set; }
        bool RequireLicenseAcceptance { get; set; }
        string Copyright { get; set; }
        string Owners { get; set; }
    }
}