namespace WindowsTooling.AppxManifest;

public class AppxManifestCreatorOptions
{
    public string[]? EntryPoints { get; set; }

    public AppxPackageArchitecture PackageArchitecture { get; set; }

    public string? PackageName { get; set; }

    public string? PackageDisplayName { get; set; }

    public string? PackageDescription { get; set; }

    public string? PublisherName { get; set; }

    public string? PublisherDisplayName { get; set; }

    public Version? Version { get; set; }

    public bool CreateLogo { get; set; }

    public string[]? StoreLogoPath { get; set; }

    public string[]? Square150x150LogoPath { get; set; }

    public string[]? Square44x44LogoPath { get; set; }

    public string[]? Wide310x150Logo { get; set; }

    public string[]? Square71x71Logo { get; set; }

    public string[]? Square310x310Logo { get; set; }

    public static AppxManifestCreatorOptions Default =>
        new()
        {
            PackageArchitecture = AppxPackageArchitecture.Neutral,
            Version = null,
            PackageName = "MyPackage",
            PackageDisplayName = "My package",
            PublisherName = "CN=Publisher",
            PublisherDisplayName = "Publisher",
            CreateLogo = true
        };
}