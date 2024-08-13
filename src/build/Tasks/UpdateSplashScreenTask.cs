using System;
using System.IO;
using Helper;
using Nuke.Common.IO;
using Serilog;

namespace Tasks;

internal static class UpdateSplashScreenTask
{
    internal static void Run(AbsolutePath repositoryDirectory)
    {
        Log.Information("Updating splash screen in Blazor.");

        AbsolutePath splashScreenPath = repositoryDirectory / "submodules" / "DevToys" / "src" / "app" / "dev" / "DevToys.Blazor" / "wwwroot" / "img" / "splash-screen.svg";
        if (!splashScreenPath.FileExists())
        {
            Log.Error("Splash screen file not found, maybe it moved? {Path}", splashScreenPath);
            return;
        }

        AbsolutePath iconSource;
        if (OperatingSystem.IsMacOS())
        {
            if (VersionHelper.IsPreview)
            {
                iconSource = repositoryDirectory / "submodules" / "DevToys" / "assets" / "logo" / "MacOS" / "Preview" / "Icon-MacOS-Preview.svg";
            }
            else
            {
                iconSource = repositoryDirectory / "submodules" / "DevToys" / "assets" / "logo" / "MacOS" / "Stable" / "Icon-MacOS.svg";
            }
        }
        else if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
        {
            if (VersionHelper.IsPreview)
            {
                iconSource = repositoryDirectory / "submodules" / "DevToys" / "assets" / "logo" / "Windows-Linux" / "Preview" / "Icon-Windows-Linux-Preview.svg";
            }
            else
            {
                iconSource = repositoryDirectory / "submodules" / "DevToys" / "assets" / "logo" / "Windows-Linux" / "Stable" / "Icon-Windows-Linux.svg";
            }
        }
        else
        {
            Log.Error("Unsupported operating system.");
            return;
        }

        File.Copy(iconSource, splashScreenPath, overwrite: true);

        Log.Information(string.Empty);
    }
}
