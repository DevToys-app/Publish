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

using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using WindowsTooling.Helpers;
using WindowsTooling.Sdk;

namespace WindowsTooling.AppxManifest;

public class MsixHeroBrandingInjector
{
    public enum BrandingInjectorOverrideOption
    {
        Default, // will prefer existing with exception of MsixHero, makeappx.exe and signtool.exe which must be taken over from the current toolset
        PreferExisting, // will prefer existing values and never overwrite anything with exception of MsixHero
        PreferIncoming // will replace existing values with new ones
    }

    public async Task Inject(XDocument manifestContent, BrandingInjectorOverrideOption overwrite = BrandingInjectorOverrideOption.Default)
    {
        Dictionary<string, string> toWrite = [];
        Dictionary<string, string> toWriteOnlyIfMissing = [];

        string signToolVersion = GetVersion("signtool.exe");
        string makePriVersion = GetVersion("makepri.exe");
        string makeAppxVersion = GetVersion("makeappx.exe");
        string? operatingSystemVersion = NdDll.RtlGetVersion()?.ToString();

        switch (overwrite)
        {
            case BrandingInjectorOverrideOption.Default:
                // prefer existing, with exception of signtool and makeappx that are always re-created.
                toWriteOnlyIfMissing.Add("MakePri.exe", makePriVersion);
                toWrite.Add("SignTool.exe", signToolVersion);
                toWrite.Add("MakeAppx.exe", makeAppxVersion);
                toWriteOnlyIfMissing.Add("OperatingSystem", operatingSystemVersion ?? string.Empty);
                break;
            case BrandingInjectorOverrideOption.PreferExisting:
                // prefer all existing
                toWriteOnlyIfMissing.Add("MakePri.exe", makePriVersion);
                toWriteOnlyIfMissing.Add("SignTool.exe", signToolVersion);
                toWriteOnlyIfMissing.Add("MakeAppx.exe", makeAppxVersion);
                toWriteOnlyIfMissing.Add("OperatingSystem", operatingSystemVersion ?? string.Empty);
                break;
            case BrandingInjectorOverrideOption.PreferIncoming:
                // overwrite everything
                toWrite.Add("MakePri.exe", makePriVersion);
                toWrite.Add("SignTool.exe", signToolVersion);
                toWrite.Add("MakeAppx.exe", makeAppxVersion);
                toWrite.Add("OperatingSystem", operatingSystemVersion ?? string.Empty);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(overwrite), overwrite, null);
        }

        SetBuildMetaDataExecutor executor = new(manifestContent);

        if (toWrite.Any())
        {
            SetBuildMetaData action = new(toWrite)
            {
                OnlyCreateNew = false
            };

            await executor.Execute(action);
        }

        if (toWriteOnlyIfMissing.Any())
        {
            SetBuildMetaData action = new(toWriteOnlyIfMissing)
            {
                OnlyCreateNew = true
            };

            await executor.Execute(action);
        }
    }

    private static string GetVersion(string sdkFile)
    {
        string path = SdkPathHelper.GetSdkPath(sdkFile);
        return File.Exists(path) ? FileVersionInfo.GetVersionInfo(path).ProductVersion ?? string.Empty : string.Empty;
    }
}
