// MSIX Hero
// Copyright (C) 2022 Marcin Otorowski
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// Full notice:
// https://github.com/marcinotorowski/msix-hero/blob/develop/LICENSE.md

using WindowsTooling.AppxManifest;

namespace WindowsTooling;

public enum PackageTypeDisplay
{
    Long,
    Normal,
    Short
}

public static class PackageTypeConverter
{
    public static string GetPackageTypeStringFrom(MsixPackageType packageType, PackageTypeDisplay displayType = PackageTypeDisplay.Normal)
    {
        bool isUwp = (packageType & MsixPackageType.Uwp) == MsixPackageType.Uwp;
        if (isUwp)
        {
            return displayType switch
            {
                PackageTypeDisplay.Long => "Universal Windows Platform (UWP) app",
                _ => "UWP",
            };
        }

        bool isBridge = (packageType & MsixPackageType.Win32) == MsixPackageType.Win32;
        if (isBridge)
        {
            return displayType switch
            {
                PackageTypeDisplay.Long => "Classic Win32 app",
                _ => "Win32",
            };
        }

        bool isPsf = (packageType & MsixPackageType.Win32Psf) == MsixPackageType.Win32Psf;
        if (isPsf)
        {
            return displayType switch
            {
                PackageTypeDisplay.Long => "Classic Win32 app enhanced by Package Support Framework (PSF)",
                PackageTypeDisplay.Short => "PSF",
                _ => "Win32 + PSF",
            };
        }

        bool isAiStub = (packageType & MsixPackageType.Win32AiStub) == MsixPackageType.Win32AiStub;
        if (isAiStub)
        {
            return displayType switch
            {
                PackageTypeDisplay.Long => "Classic Win32 app enhanced by Advanced Installer launcher",
                PackageTypeDisplay.Short => "AI",
                _ => "Win32 + AI",
            };
        }

        bool isWeb = (packageType & MsixPackageType.Web) == MsixPackageType.Web ||
                    (packageType & MsixPackageType.ProgressiveWebApp) == MsixPackageType.ProgressiveWebApp;
        if (isWeb)
        {
            return displayType switch
            {
                PackageTypeDisplay.Long => "Progressive Web Application",
                _ => "PWA",
            };
        }

        bool isFramework = (packageType & MsixPackageType.Framework) == MsixPackageType.Framework;
        if (isFramework)
        {
            return displayType switch
            {
                PackageTypeDisplay.Short => "Framework",
                _ => "Framework",
            };
        }

        return displayType switch
        {
            PackageTypeDisplay.Short => "App",
            _ => "Unknown",
        };
    }

    public static string GetPackageTypeStringFrom(string entryPoint, string executable, string startPage, bool isFramework, PackageTypeDisplay displayType = PackageTypeDisplay.Normal)
    {
        return GetPackageTypeStringFrom(GetPackageTypeFrom(entryPoint, executable, startPage, isFramework), displayType);
    }

    public static string GetPackageTypeStringFrom(string entryPoint, string executable, string startPage, bool isFramework, string? hostId = null, PackageTypeDisplay displayType = PackageTypeDisplay.Normal)
    {
        return GetPackageTypeStringFrom(GetPackageTypeFrom(entryPoint, executable, startPage, isFramework, hostId), displayType);
    }

    public static MsixPackageType GetPackageTypeFrom(string? entryPoint, string? executable, string? startPage, bool isFramework, string? hostId = null)
    {
        if (hostId == "PWA")
        {
            return MsixPackageType.ProgressiveWebApp;
        }

        if (isFramework)
        {
            return MsixPackageType.Framework;
        }

        if (!string.IsNullOrEmpty(entryPoint))
        {
            switch (entryPoint)
            {
                case "Windows.FullTrustApplication":

                    if (!string.IsNullOrEmpty(executable) && string.Equals(".exe", Path.GetExtension(executable).ToLowerInvariant()))
                    {
                        executable = "\\" + executable; // to make sure we have a backslash for checking

                        if (
                            executable.IndexOf("\\psflauncher", StringComparison.OrdinalIgnoreCase) != -1 ||
                            executable.IndexOf("\\psfrundll", StringComparison.OrdinalIgnoreCase) != -1 ||
                            executable.IndexOf("\\psfmonitor", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            return MsixPackageType.Win32Psf;
                        }

                        if (
                            executable.IndexOf("\\ai_stubs", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            return MsixPackageType.Win32AiStub;
                        }

                        return MsixPackageType.Win32;
                    }

                    return 0;
            }

            if (string.IsNullOrEmpty(startPage))
            {
                return MsixPackageType.Uwp;
            }

            return 0;
        }

        if (executable?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true)
        {
            // workaround for MS Edge…
            return MsixPackageType.Win32;
        }

        return string.IsNullOrEmpty(startPage) ? 0 : MsixPackageType.Web;
    }
}
