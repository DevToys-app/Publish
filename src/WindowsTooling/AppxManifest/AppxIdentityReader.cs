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
using System.IO.Compression;
using System.Xml.Linq;
using WindowsTooling.AppxManifest.FileReader;

namespace WindowsTooling.AppxManifest;

public class AppxIdentityReader : IAppxIdentityReader
{
    public async Task<AppxIdentity> GetIdentity(string filePath, CancellationToken cancellationToken = default)
    {
        switch (Path.GetExtension(filePath).ToLowerInvariant())
        {
            case ".xml":
                if (string.Equals(FileConstants.AppxManifestFile, Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase))
                {
                    await using var manifestStream = File.OpenRead(filePath);
                    return await GetIdentityFromPackageManifest(manifestStream, cancellationToken);
                }
                else if (string.Equals(FileConstants.AppxBundleManifestFile, Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase))
                {
                    await using var manifestStream = File.OpenRead(filePath);
                    return await GetIdentityFromBundleManifest(manifestStream, cancellationToken);
                }
                else
                {
                    throw new ArgumentException($"File name {Path.GetFileName(filePath)} is not supported.");
                }

            case FileConstants.MsixExtension:
            case FileConstants.AppxExtension:
                return await GetIdentityFromPackage(filePath, cancellationToken);
            case FileConstants.AppxBundleExtension:
            case FileConstants.MsixBundleExtension:
                return await GetIdentityFromBundle(filePath, cancellationToken);
            default:
                throw new ArgumentException($"File extension {Path.GetExtension(filePath)} is not supported.");
        }
    }

    public async Task<AppxIdentity> GetIdentity(Stream file, CancellationToken cancellationToken = default)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        Log.Information("Reading identity from stream of type " + file.GetType().Name);

        if (file is FileStream fileStream)
        {
            Log.Debug("The input is a file stream, trying to evaluate its name…");
            switch (Path.GetExtension(fileStream.Name).ToLowerInvariant())
            {
                case FileConstants.AppxBundleExtension:
                case FileConstants.MsixBundleExtension:
                    {
                        Log.Information("The file seems to be a bundle package (compressed).");
                        try
                        {
                            using IAppxFileReader reader = new ZipArchiveFileReaderAdapter(fileStream);
                            return await GetIdentityFromBundleManifest(reader.GetFile(FileConstants.AppxBundleManifestFilePath), cancellationToken);
                        }
                        catch (FileNotFoundException e)
                        {
                            throw new ArgumentException("File  " + fileStream.Name + " is not an APPX/MSIX bundle, because it does not contain a manifest.", nameof(file), e);
                        }
                    }

                case FileConstants.AppxExtension:
                case FileConstants.MsixExtension:
                    {
                        Log.Information("The file seems to be a package (compressed).");
                        try
                        {
                            using IAppxFileReader reader = new ZipArchiveFileReaderAdapter(fileStream);
                            return await GetIdentityFromBundleManifest(reader.GetFile(FileConstants.AppxManifestFile), cancellationToken);
                        }
                        catch (FileNotFoundException e)
                        {
                            throw new ArgumentException("File " + fileStream.Name + " is not an APPX/MSIX package, because it does not contain a manifest.", nameof(file), e);
                        }
                    }
            }

            switch (Path.GetFileName(fileStream.Name).ToLowerInvariant())
            {
                case FileConstants.AppxManifestFile:
                    Log.Information("The file seems to be a package (manifest).");
                    return await GetIdentityFromPackageManifest(fileStream, cancellationToken);
                case FileConstants.AppxBundleManifestFile:
                    Log.Information("The file seems to be a bundle (manifest).");
                    return await GetIdentityFromBundleManifest(fileStream, cancellationToken);
            }
        }

        try
        {
            Log.Debug("Trying to interpret the input file as an XML manifest…");
            XDocument doc = await XDocument.LoadAsync(file, LoadOptions.None, cancellationToken);
            XElement? firstElement = doc.Elements().FirstOrDefault();
            if (firstElement != null)
            {
                if (firstElement.Name.LocalName == "Package")
                {
                    Log.Information("The file seems to be a package (manifest).");
                    return GetIdentityFromPackageManifest(doc);
                }

                if (firstElement.Name.LocalName == "Bundle")
                {
                    Log.Information("The file seems to be a bundle (manifest).");
                    return GetIdentityFromBundleManifest(doc);
                }

                // This is an XML file but neither a package manifest or a bundle manifest, so we can stop here.
                throw new ArgumentException("This XML file is neither package nor a bundle manifest (missing <Package /> or <Bundle /> root element).");
            }
        }
        catch
        {
            // this is ok, it seems that the file was not XML so we should continue to find out some other possibilities
            Log.Debug("The file was not in XML format (exception thrown during parsing).");
        }

        try
        {
            file.Seek(0, SeekOrigin.Begin);
            using ZipArchive zip = new(file, ZipArchiveMode.Read, true);
            using IAppxFileReader reader = new ZipArchiveFileReaderAdapter(zip);

            if (reader.FileExists(FileConstants.AppxManifestFile))
            {
                return await GetIdentityFromPackageManifest(reader.GetFile(FileConstants.AppxManifestFile), cancellationToken);
            }

            if (reader.FileExists(FileConstants.AppxBundleManifestFilePath))
            {
                return await GetIdentityFromBundleManifest(reader.GetFile(FileConstants.AppxBundleManifestFilePath), cancellationToken);
            }

            // This is a ZIP archive but neither a package or bundle, so we can stop here.
            throw new ArgumentException("This compressed file is neither an APPX/MSIX or bundle because it contains no manifest file.");
        }
        catch
        {
            // this is ok, it seems that the file was not ZIP format so we should continue to find out some other possibilities
            Log.Debug("The file was not in ZIP format (exception thrown during opening).");
        }

        throw new ArgumentException("The input stream is neither a valid manifest or package file.");
    }

    private static async Task<AppxIdentity> GetIdentityFromPackage(string packagePath, CancellationToken cancellationToken = default)
    {
        using IAppxFileReader reader = new ZipArchiveFileReaderAdapter(packagePath);
        try
        {
            return await GetIdentityFromPackageManifest(reader.GetFile(FileConstants.AppxManifestFile), cancellationToken);
        }
        catch (FileNotFoundException e)
        {
            throw new ArgumentException("File " + packagePath + " is not an APPX/MSIX package, because it does not contain a manifest.", nameof(packagePath), e);
        }
    }

    private static async Task<AppxIdentity> GetIdentityFromBundle(string bundlePath, CancellationToken cancellationToken = default)
    {
        try
        {
            using IAppxFileReader reader = new ZipArchiveFileReaderAdapter(bundlePath);
            return await GetIdentityFromBundleManifest(reader.GetFile(FileConstants.AppxBundleManifestFilePath), cancellationToken);
        }
        catch (FileNotFoundException e)
        {
            throw new ArgumentException("File " + bundlePath + " is not an APPX/MSIX bundle, because it does not contain a manifest.", nameof(bundlePath), e);
        }
    }

    private static async Task<AppxIdentity> GetIdentityFromPackageManifest(Stream packageManifestStream, CancellationToken cancellationToken = default)
    {
        XDocument doc = await XDocument.LoadAsync(packageManifestStream, LoadOptions.None, cancellationToken);
        return GetIdentityFromPackageManifest(doc);
    }

    internal static AppxIdentity GetIdentityFromPackageManifest(XDocument document, CancellationToken cancellationToken = default)
    {
        /*
         * <Package
         *    xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10">
              <Identity Name="MSIXHero" Version="2.0.46.0" Publisher="CN=abc" ProcessorArchitecture="neutral" />
         */

        XNamespace windows10Namespace = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
        XNamespace appxNamespace = "http://schemas.microsoft.com/appx/2010/manifest";

        XElement? root = (document.Element(windows10Namespace + "Package") ?? document.Element(appxNamespace + "Package")) ?? throw new InvalidDataException("Not a valid package manifest. Missing root element <Package />.");
        XElement? identity = (root.Element(windows10Namespace + "Identity") ?? root.Element(appxNamespace + "Identity")) ?? throw new InvalidDataException("Not a valid bundle manifest. Missing child element <Identity />.");
        XAttribute? version = identity.Attribute("Version");
        XAttribute? publisher = identity.Attribute("Publisher");
        XAttribute? name = identity.Attribute("Name");
        XAttribute? architecture = identity.Attribute("ProcessorArchitecture");

        AppxIdentity appxIdentity = new()
        {
            Name = name?.Value,
            Publisher = publisher?.Value,
            Version = version?.Value
        };

        if (architecture != null)
        {
            if (Enum.TryParse(architecture.Value, true, out AppxPackageArchitecture architectureValue))
            {
                appxIdentity.Architectures = new[] { architectureValue };
            }
            else
            {
                Log.Warning("Could not parse the value " + architecture.Value + " to one of known enums.");
            }
        }

        return appxIdentity;
    }

    private static async Task<AppxIdentity> GetIdentityFromBundleManifest(Stream bundleManifestStream, CancellationToken cancellationToken = default)
    {
        XDocument doc = await XDocument.LoadAsync(bundleManifestStream, LoadOptions.None, cancellationToken);
        return GetIdentityFromBundleManifest(doc);
    }

    internal static AppxIdentity GetIdentityFromBundleManifest(XDocument document, CancellationToken cancellationToken = default)
    {
        /*
         *<?xml version="1.0" encoding="UTF-8"?>
         * <Bundle xmlns:b4="http://schemas.microsoft.com/appx/2018/bundle" xmlns="http://schemas.microsoft.com/appx/2013/bundle" 
         */
        XNamespace bundleManifest = "http://schemas.microsoft.com/appx/2013/bundle";

        XElement? root = document.Element(bundleManifest + "Bundle") ?? throw new InvalidDataException("Not a valid bundle manifest. Missing root element <Bundle />.");
        XElement? identity = root.Element(bundleManifest + "Identity") ?? throw new InvalidDataException("Not a valid bundle manifest. Missing child element <Identity />.");
        XAttribute? version = identity.Attribute("Version");
        XAttribute? publisher = identity.Attribute("Publisher");
        XAttribute? name = identity.Attribute("Name");

        XElement? packagesNode = root.Element(bundleManifest + "Packages");
        IEnumerable<XElement>? packages = packagesNode?.Elements(bundleManifest + "Package");

        AppxIdentity appxIdentity = new()
        {
            Name = name?.Value,
            Publisher = publisher?.Value,
            Version = version?.Value
        };

        if (packages != null)
        {
            HashSet<AppxPackageArchitecture> architectures = [];

            foreach (XElement package in packages)
            {
                XAttribute? architecture = package.Attribute("Architecture");
                if (architecture == null)
                {
                    continue;
                }

                if (Enum.TryParse(architecture.Value, true, out AppxPackageArchitecture architectureValue))
                {
                    architectures.Add(architectureValue);
                }
                else
                {
                    Log.Warning("Could not parse the value " + architecture.Value + " to one of known enums.");
                }
            }

            appxIdentity.Architectures = architectures.ToArray();
        }

        return appxIdentity;
    }
}