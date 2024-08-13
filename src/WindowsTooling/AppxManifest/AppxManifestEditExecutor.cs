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

using System.Xml.Linq;

namespace WindowsTooling.AppxManifest;

public abstract class AppxManifestEditExecutor<T> : IAppxEditCommandExecutor<T> where T : IAppxEditCommand
{
    protected readonly XDocument Manifest;

    protected AppxManifestEditExecutor(XDocument manifest)
    {
        Manifest = manifest;
    }

    public abstract Task Execute(T command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures that a namespace exists and returns its prefix.
    /// </summary>
    /// <param name="namespaceUrl">The namespace URL to check.</param>
    /// <param name="preferredPrefix">The preferred prefix in case the namespace is to be created.</param>
    protected string EnsureNamespace(string namespaceUrl, string preferredPrefix)
    {
        return EnsureNamespace(XNamespace.Get(namespaceUrl), preferredPrefix);
    }

    /// <summary>
    /// Ensures that a namespace exists and returns its prefix.
    /// </summary>
    /// <param name="ns">The namespace to check.</param>
    /// <param name="preferredPrefix">The preferred prefix in case the namespace is to be created.</param>
    protected string EnsureNamespace(XNamespace ns, string preferredPrefix)
    {
        if (Manifest.Root == null)
        {
            throw new InvalidOperationException("Document root cannot be empty.");
        }

        if (Manifest.Root.GetDefaultNamespace().NamespaceName == ns.NamespaceName)
        {
            return string.Empty;
        }

        string? prefix = Manifest.Root.GetPrefixOfNamespace(ns);
        if (prefix == null)
        {
            prefix = preferredPrefix;
            Manifest.Root.Add(new XAttribute(XNamespace.Xmlns + preferredPrefix, ns.NamespaceName));
        }

        XAttribute? ignorable = Manifest.Root.Attribute("IgnorableNamespaces");
        if (ignorable == null)
        {
            ignorable = new XAttribute("IgnorableNamespaces", prefix);
            Manifest.Root.Add(ignorable);
        }
        else
        {
            string[] value = ignorable.Value.Split(' ');
            if (!value.Contains(prefix))
            {
                ignorable.SetValue(ignorable.Value + " " + prefix);
            }
        }

        return prefix;
    }

    /// <summary>
    /// Ensures than a given namespace with a given version exists, and returns a tuple with prefix to that namespace and the actual namespace object.
    /// </summary>
    /// <param name="ns">The type of the namespace to ensure exists in the document.</param>
    /// <param name="version">The version of the namespace.</param>
    /// <returns>A tuple with prefix to that namespace and the actual namespace object.</returns>
    protected (string, XNamespace) EnsureNamespace(Namespaces ns, int version)
    {
        if (Manifest.Root == null)
        {
            throw new InvalidOperationException("Document root cannot be empty.");
        }

        string namespaceUrl;
        string preferredPrefix;

        switch (ns)
        {
            case Namespaces.Root:
                return (string.Empty, Manifest.Root.GetDefaultNamespace());

            case Namespaces.Appx:
                namespaceUrl = "http://schemas.microsoft.com/appx/2010/manifest";
                preferredPrefix = "appx";
                break;

            case Namespaces.Uap:
                namespaceUrl = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
                preferredPrefix = "uap";
                break;

            case Namespaces.RestrictedCapabilities:
                namespaceUrl = "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities";
                preferredPrefix = "rescap";
                break;

            case Namespaces.Build:
                preferredPrefix = "build";
                namespaceUrl = "http://schemas.microsoft.com/developer/appx/2015/build";
                break;

            case Namespaces.Foundation:
                namespaceUrl = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
                preferredPrefix = "win";
                break;

            case Namespaces.Mobile:
                namespaceUrl = "http://schemas.microsoft.com/appx/manifest/mobile/windows10";
                preferredPrefix = "mobile";
                break;

            case Namespaces.Iot:
                namespaceUrl = "http://schemas.microsoft.com/appx/manifest/iot/windows10";
                preferredPrefix = "iot";
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(ns), ns, null);
        }

        if (version == 0)
        {
            XNamespace xns = XNamespace.Get(namespaceUrl);
            string actualPrefix = EnsureNamespace(xns, preferredPrefix);

            return (actualPrefix, xns);
        }
        else
        {
            XNamespace xns = XNamespace.Get($"{namespaceUrl}/{version}");
            string actualPrefix = EnsureNamespace(xns, preferredPrefix);

            return (actualPrefix, xns);
        }
    }

    /// <summary>
    /// Ensures than a given namespace type exists, and returns a tuple with prefix to that namespace and the actual namespace object.
    /// </summary>
    /// <param name="ns">The type of the namespace to ensure exists in the document.</param>
    /// <returns>A tuple with prefix to that namespace and the actual namespace object.</returns>
    protected (string, XNamespace) EnsureNamespace(Namespaces ns)
    {
        return EnsureNamespace(ns, 0);
    }

    /// <summary>
    /// Ensures than a given namespace type exists, and returns a tuple with prefix to that namespace and the actual namespace object.
    /// </summary>
    /// <returns>A tuple with prefix to that namespace and the actual namespace object.</returns>
    protected (string, XNamespace) EnsureNamespace()
    {
        if (Manifest.Root == null)
        {
            throw new InvalidOperationException("Document root cannot be empty.");
        }

        return (string.Empty, Manifest.Root.GetDefaultNamespace());
    }

    protected enum Namespaces
    {
        Root,

        Appx,

        Uap,

        RestrictedCapabilities,

        Build,

        Foundation,

        Iot,

        Mobile
    }
}