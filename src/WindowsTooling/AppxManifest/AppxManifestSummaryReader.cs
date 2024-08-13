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

using Serilog;
using System.Xml.Linq;
using WindowsTooling.AppxManifest.FileReader;

namespace WindowsTooling.AppxManifest;

public static class AppxManifestSummaryReader
{
    public static async Task<AppxManifestSummary> FromMsix(string fullMsixFilePath, ReadMode mode = ReadMode.Minimal)
    {
        Log.Information("Reading application manifest from {0}…", fullMsixFilePath);

        if (!File.Exists(fullMsixFilePath))
        {
            throw new FileNotFoundException("MSIX file does not exist.", fullMsixFilePath);
        }

        using IAppxFileReader reader = FileReaderFactory.CreateFileReader(fullMsixFilePath);
        Stream package = reader.GetFile(FileConstants.AppxManifestFile);
        return await FromManifest(package, mode);
    }

    public static async Task<AppxManifestSummary> FromMsix(IAppxFileReader msixFileReader, ReadMode mode = ReadMode.Minimal)
    {
        Stream package = msixFileReader.GetFile(FileConstants.AppxManifestFile);
        return await FromManifest(package, mode);
    }

    public static Task<AppxManifestSummary> FromInstallLocation(string installLocation, ReadMode mode = ReadMode.Minimal)
    {
        Log.Debug("Reading application manifest from install location {0}…", installLocation);
        if (!Directory.Exists(installLocation))
        {
            throw new DirectoryNotFoundException("Install location " + installLocation + " not found.");
        }

        return FromManifest(Path.Join(installLocation, FileConstants.AppxManifestFile), mode);
    }

    public static async Task<AppxManifestSummary> FromManifest(string fullManifestPath, ReadMode mode = ReadMode.Minimal)
    {
        if (!File.Exists(fullManifestPath))
        {
            throw new FileNotFoundException("Manifest file does not exist.", fullManifestPath);
        }

        await using var fs = File.OpenRead(fullManifestPath);
        return await FromManifest(fs, mode);
    }

    private static async Task<AppxManifestSummary> FromManifest(Stream manifestStream, ReadMode mode)
    {
        AppxManifestSummary result = new();

        Log.Debug("Loading XML file…");
        XDocument xmlDocument = await XDocument.LoadAsync(manifestStream, LoadOptions.None, CancellationToken.None);

        AppxIdentity identity = AppxIdentityReader.GetIdentityFromPackageManifest(xmlDocument);
        result.Name = identity.Name;
        result.Version = identity.Version;
        result.Publisher = identity.Publisher;
        result.Architectures = identity.Architectures;

        XNamespace win10Namespace = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
        XNamespace appxNamespace = "http://schemas.microsoft.com/appx/2010/manifest";
        XNamespace uapNamespace = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
        XNamespace uap3Namespace = "http://schemas.microsoft.com/appx/manifest/uap/windows10/3";

        XElement? packageNode = (xmlDocument.Element(win10Namespace + "Package") ?? xmlDocument.Element(appxNamespace + "Package")) ?? throw new ArgumentException("The manifest file does not contain a valid root element (<Package />).");
        if ((mode & ReadMode.Properties) == ReadMode.Properties)
        {
            XElement? propertiesNode = packageNode.Element(win10Namespace + "Properties") ?? packageNode.Element(appxNamespace + "Properties");
            Log.Verbose("Executing XQuery /*[local-name()='Package']/*[local-name()='Properties'] for a single node…");

            if (propertiesNode != null)
            {
                foreach (XElement subNode in propertiesNode.Elements())
                {
                    switch (subNode.Name.LocalName)
                    {
                        case "DisplayName":
                            result.DisplayName = subNode.Value;
                            break;
                        case "PublisherDisplayName":
                            result.DisplayPublisher = subNode.Value;
                            break;
                        case "Description":
                            result.Description = subNode.Value;
                            break;
                        case "Logo":
                            result.Logo = subNode.Value;
                            break;
                        case "Framework":
                            result.IsFramework = bool.TryParse(subNode.Value, out bool parsed) && parsed;
                            break;
                    }
                }
            }

            XElement? applicationsNode = packageNode.Element(win10Namespace + "Applications") ?? packageNode.Element(appxNamespace + "Applications");
            if (applicationsNode != null)
            {
                IEnumerable<XElement> applicationNode = applicationsNode.Elements(win10Namespace + "Application").Concat(applicationsNode.Elements(appxNamespace + "Application"));
                IEnumerable<XElement> visualElementsNode = applicationNode
                    .SelectMany(e => e.Elements(win10Namespace + "VisualElements")
                    .Concat(e.Elements(appxNamespace + "VisualElements"))
                    .Concat(e.Elements(uap3Namespace + "VisualElements"))
                    .Concat(e.Elements(uapNamespace + "VisualElements")));

                XAttribute? background = visualElementsNode.Select(node => node.Attribute("BackgroundColor")).FirstOrDefault(a => a != null);
                result.AccentColor = background?.Value ?? "Transparent";
            }
            else
            {
                result.AccentColor = "Transparent";
            }
        }

        if ((mode & ReadMode.Applications) == ReadMode.Applications)
        {
            result.PackageType = result.IsFramework ? MsixPackageType.Framework : 0;

            XElement? applicationsNode = packageNode.Element(win10Namespace + "Applications") ?? packageNode.Element(appxNamespace + "Applications");
            if (applicationsNode != null)
            {
                IEnumerable<XElement> applicationNode = applicationsNode.Elements(win10Namespace + "Application").Concat(applicationsNode.Elements(appxNamespace + "Application"));

                foreach (XElement subNode in applicationNode)
                {
                    string? entryPoint = subNode.Attribute("EntryPoint")?.Value;
                    string? executable = subNode.Attribute("Executable")?.Value;
                    string? startPage = subNode.Attribute("StartPage")?.Value;
                    result.PackageType |= PackageTypeConverter.GetPackageTypeFrom(entryPoint, executable, startPage, result.IsFramework);
                }
            }
        }

        Log.Debug("Manifest information parsed.");
        return result;
    }

    [Flags]
    public enum ReadMode
    {
        Applications = 2 << 0,
        Properties = 2 << 1,
        Minimal = Applications | Properties
    }
}