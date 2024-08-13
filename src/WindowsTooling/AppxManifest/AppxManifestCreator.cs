using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WindowsTooling.Helpers;

namespace WindowsTooling.AppxManifest;

public class AppxManifestCreator
{
    public async IAsyncEnumerable<CreatedItem> CreateManifestForDirectory(
        DirectoryInfo sourceDirectory,
        AppxManifestCreatorOptions? options = default,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= AppxManifestCreatorOptions.Default;

        // Add logo to assets if there is nothing
        if (options.CreateLogo)
        {
            CreatedItem logo = default;
            foreach (string entryPoint in options.EntryPoints ?? Enumerable.Empty<string>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    FileInfo entryPointPath = new(Path.Combine(sourceDirectory.FullName, entryPoint));
                    if (entryPointPath.Exists)
                    {
                        logo = await CreateLogo(entryPointPath);
                    }
                }
                catch
                {
                }

                if (!default(CreatedItem).Equals(logo))
                {
                    break;
                }
            }

            if (default(CreatedItem).Equals(logo))
            {
                logo = await CreateDefaultLogo(cancellationToken);
            }

            yield return logo;
        }
        else
        {
            if (options.StoreLogoPath is null)
            {
                throw new InvalidOperationException("Store logo path is required when CreateLogo is set to false.");
            }
            if (options.Square150x150LogoPath is null)
            {
                throw new InvalidOperationException("Square150x150Logo path is required when CreateLogo is set to false.");
            }
            if (options.Square44x44LogoPath is null)
            {
                throw new InvalidOperationException("Square44x44Logo path is required when CreateLogo is set to false.");
            }
            if (options.Wide310x150Logo is null)
            {
                throw new InvalidOperationException("Square44x44Logo path is required when CreateLogo is set to false.");
            }
            if (options.Square71x71Logo is null)
            {
                throw new InvalidOperationException("Square44x44Logo path is required when CreateLogo is set to false.");
            }
            if (options.Square310x310Logo is null)
            {
                throw new InvalidOperationException("Square44x44Logo path is required when CreateLogo is set to false.");
            }

            for (int i = 0; i < options.StoreLogoPath.Length; i++)
            {
                yield return CreatedItem.CreateAsset(options.StoreLogoPath[i], "Assets/" + Path.GetFileName(options.StoreLogoPath[i]));
            }

            for (int i = 0; i < options.Square150x150LogoPath.Length; i++)
            {
                yield return CreatedItem.CreateAsset(options.Square150x150LogoPath[i], "Assets/" + Path.GetFileName(options.Square150x150LogoPath[i]));
            }

            for (int i = 0; i < options.Square44x44LogoPath.Length; i++)
            {
                yield return CreatedItem.CreateAsset(options.Square44x44LogoPath[i], "Assets/" + Path.GetFileName(options.Square44x44LogoPath[i]));
            }

            for (int i = 0; i < options.Wide310x150Logo.Length; i++)
            {
                yield return CreatedItem.CreateAsset(options.Wide310x150Logo[i], "Assets/" + Path.GetFileName(options.Wide310x150Logo[i]));
            }

            for (int i = 0; i < options.Square71x71Logo.Length; i++)
            {
                yield return CreatedItem.CreateAsset(options.Square71x71Logo[i], "Assets/" + Path.GetFileName(options.Square71x71Logo[i]));
            }

            for (int i = 0; i < options.Square310x310Logo.Length; i++)
            {
                yield return CreatedItem.CreateAsset(options.Square310x310Logo[i], "Assets/" + Path.GetFileName(options.Square310x310Logo[i]));
            }
        }

        // The actual part - create the manifest
        string modPackageTemplate = GetBundledResourcePath("ModificationPackage.AppxManifest.xml");
        await using var openTemplate = File.OpenRead(modPackageTemplate);
        var xml = await XDocument.LoadAsync(openTemplate, LoadOptions.None, cancellationToken);
        string[]? entryPoints = options.EntryPoints;

        if (entryPoints?.Any() != true)
        {
            IList<string> getExeFiles = await GetEntryPointCandidates(sourceDirectory, cancellationToken);
            entryPoints = getExeFiles.ToArray();
        }

        if (options.EntryPoints?.Any() == true)
        {
            foreach (string entryPoint in options.EntryPoints)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ExceptionGuard.Guard(() =>
                {
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(Path.Combine(sourceDirectory.FullName, entryPoint));

                    if (options.Version == null)
                    {
                        options.Version = Version.TryParse(fvi.ProductVersion ?? fvi.FileVersion ?? "#", out Version? v) ? v : null;
                    }

                    if (string.IsNullOrEmpty(options.PackageName) && !string.IsNullOrEmpty(Sanitize(fvi.ProductName)))
                    {
                        options.PackageName = Sanitize(fvi.ProductName, "ProductName")?.Trim();
                    }

                    if (string.IsNullOrEmpty(options.PackageDisplayName) && !string.IsNullOrEmpty(fvi.ProductName))
                    {
                        options.PackageDisplayName = fvi.ProductName?.Trim();
                    }

                    if (string.IsNullOrEmpty(options.PublisherName) && !string.IsNullOrEmpty(Sanitize(fvi.CompanyName)))
                    {
                        options.PublisherName = "CN=" + Sanitize(fvi.CompanyName, "CompanyName");
                    }

                    if (string.IsNullOrEmpty(options.PublisherDisplayName) && !string.IsNullOrEmpty(Sanitize(fvi.CompanyName)))
                    {
                        options.PublisherDisplayName = fvi.CompanyName?.Trim();
                    }
                });

                if (options.Version != null &&
                    !string.IsNullOrEmpty(options.PublisherName) &&
                    !string.IsNullOrEmpty(options.PublisherDisplayName) &&
                    !string.IsNullOrEmpty(options.PackageName) &&
                    !string.IsNullOrEmpty(options.PackageDisplayName))
                {
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(options.PackageDisplayName))
        {
            options.PackageDisplayName = "MyPackage";
        }

        if (string.IsNullOrEmpty(options.PackageName))
        {
            options.PackageName = "MyPackage";
        }

        if (string.IsNullOrEmpty(options.PublisherName))
        {
            options.PublisherName = "CN=Publisher";
        }

        if (string.IsNullOrEmpty(options.PublisherDisplayName))
        {
            options.PublisherDisplayName = "Publisher";
        }

        if (options.Version == null)
        {
            options.Version = new Version(1, 0, 0);
        }

        await this.AdjustManifest(xml, options, sourceDirectory, entryPoints);
        var manifestContent = xml.ToString(SaveOptions.OmitDuplicateNamespaces);

        string manifestFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(FileConstants.AppxManifestFile) + "-" + Guid.NewGuid().ToString("N").Substring(0, 10) + ".xml");
        await File.WriteAllTextAsync(manifestFilePath, manifestContent, Encoding.UTF8, cancellationToken);

        yield return CreatedItem.CreateManifest(manifestFilePath);
    }

    private static string? Sanitize(string? input, string? defaultIfNull = null)
    {
        string result = Regex.Replace(input ?? string.Empty, @"[^a-zA-Z0-9\.]+", string.Empty).Trim();
        if (string.IsNullOrEmpty(result))
        {
            return defaultIfNull;
        }

        return result;
    }

    public async Task<AppxManifestCreatorAdviser> AnalyzeDirectory(DirectoryInfo directoryInfo, CancellationToken cancellationToken = default)
    {
        AppxManifestCreatorAdviser result = new();

        FileInfo manifest = new(Path.Combine(directoryInfo.FullName, FileConstants.AppxManifestFile));
        if (manifest.Exists)
        {
            result.Manifest = manifest;
        }

        result.Directory = directoryInfo;
        result.EntryPoints = (await GetEntryPointCandidates(directoryInfo, cancellationToken)).ToArray();

        if (manifest.Exists)
        {
            AppxManifestSummary manifestSummary = await AppxManifestSummaryReader.FromManifest(manifest.FullName, AppxManifestSummaryReader.ReadMode.Properties);
            if (manifestSummary.Logo != null)
            {
                result.Logo = new FileInfo(Path.Combine(directoryInfo.FullName, manifestSummary.Logo.Replace("/", "\\")));
                if (!result.Logo.Exists)
                {
                    result.Logo = null;
                }
            }
        }

        return result;
    }

    public async Task<IList<string>> GetEntryPointCandidates(DirectoryInfo directoryInfo, CancellationToken cancellationToken = default)
    {
        IList<string> candidates = await GetEntryPoints(directoryInfo, cancellationToken);
        if (!candidates.Any())
        {
            throw new InvalidOperationException("This folder contains no executable files.");
        }

        List<string> filteredList = candidates.Where(c =>
        {
            string fn = Path.GetFileNameWithoutExtension(c).ToLowerInvariant();
            return fn switch
            {
                "update" or "updater" => false,
                _ => !fn.StartsWith("unins", StringComparison.OrdinalIgnoreCase),
            };
        }).ToList();

        if (filteredList.Any())
        {
            return filteredList;
        }

        return candidates;
    }

    private async Task<CreatedItem> CreateDefaultLogo(CancellationToken cancellationToken = default)
    {
        string logoSourcePath = Path.Combine(Path.GetTempPath(), "Logo-" + Guid.NewGuid().ToString("N").Substring(0, 10) + ".png");
        string bundledLogoPath = GetBundledResourcePath("Logo.png");
        await using var sourceStream = File.OpenRead(bundledLogoPath);
        await using var targetStream = File.OpenWrite(logoSourcePath);
        await sourceStream.CopyToAsync(targetStream, cancellationToken);
        return CreatedItem.CreateAsset(logoSourcePath, "Assets/StoreLogo.png");
    }

    private Task<CreatedItem> CreateLogo(FileInfo logoSource)
    {
        string logoSourcePath = Path.Combine(Path.GetTempPath(), "Logo-" + Guid.NewGuid().ToString("N").Substring(0, 10) + ".png");

        ExceptionGuard.Guard(() =>
        {
            using Icon? icon = Icon.ExtractAssociatedIcon(logoSource.FullName);
            if (icon == null)
            {
                return;
            }

            using Bitmap bitmap = icon.ToBitmap();
            bitmap.Save(logoSourcePath, ImageFormat.Png);
        });

        if (File.Exists(logoSourcePath))
        {
            return Task.FromResult(CreatedItem.CreateAsset(logoSourcePath, "Assets/StoreLogo.png"));
        }

        return Task.FromResult(default(CreatedItem));
    }

    private Task<IList<string>> GetEntryPoints(DirectoryInfo directoryInfo, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            IList<string> allFiles = new List<string>();
            foreach (FileInfo file in directoryInfo.EnumerateFiles("*.exe", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                string fullName = file.FullName;
                if (fullName.StartsWith(directoryInfo.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    allFiles.Add(Path.GetRelativePath(directoryInfo.FullName, fullName));
                }
            }

            return (IList<string>)allFiles.OrderBy(fl => fl.Split('\\').Length).ThenBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        }, cancellationToken);
    }

    private async Task AdjustManifest(XDocument template, AppxManifestCreatorOptions config, DirectoryInfo baseDirectory, string[] entryPoints)
    {
        XNamespace nsUap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
        XNamespace nsUap4 = "http://schemas.microsoft.com/appx/manifest/uap/windows10/4";
        XNamespace nsUap6 = "http://schemas.microsoft.com/appx/manifest/uap/windows10/6";
        XNamespace defaultNamespace = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";

        XElement? root = template.Root;
        if (root == null)
        {
            root = new XElement(defaultNamespace + "Package");
            template.Add(root);
        }
        else
        {
            defaultNamespace = root.GetDefaultNamespace();
        }

        // Add capability
        AddCapability addCapability = new("runFullTrust");
        AddCapabilityExecutor capabilityExecutor = new(template);
        await capabilityExecutor.Execute(addCapability);

        // Set identity
        SetPackageIdentity setIdentity = new()
        {
            Name = config.PackageName,
            Publisher = config.PublisherName,
            ProcessorArchitecture = config.PackageArchitecture.ToString("G").ToLowerInvariant(),
        };

        int major = config.Version?.Major ?? 0;
        int minor = config.Version?.Minor ?? 0;
        int build = config.Version?.Build ?? 0;
        int revision = config.Version?.Revision ?? 0;

        if (major < 0)
        {
            throw new FormatException("Invalid version format, major version is required.");
        }

        if (minor < 0)
        {
            throw new FormatException("Invalid version format, major version is required.");
        }

        if (revision < 0)
        {
            revision = 0;
        }

        if (build < 0)
        {
            build = 0;
        }

        setIdentity.Version = new Version(major, minor, build, revision).ToString();
        SetPackageIdentityExecutor executor = new(template);
        await executor.Execute(setIdentity);

        // Add namespaces (legacy)
        if (root.GetPrefixOfNamespace(nsUap) == null)
        {
            root.Add(new XAttribute(XNamespace.Xmlns + "uap", nsUap.NamespaceName));
        }

        if (root.GetPrefixOfNamespace(nsUap4) == null)
        {
            root.Add(new XAttribute(XNamespace.Xmlns + "uap4", nsUap6.NamespaceName));
        }

        if (root.GetPrefixOfNamespace(nsUap6) == null)
        {
            root.Add(new XAttribute(XNamespace.Xmlns + "uap6", nsUap6.NamespaceName));
        }

        XElement package = GetOrCreateNode(template, "Package", defaultNamespace);

        XElement properties = GetOrCreateNode(package, "Properties", defaultNamespace);
        var displayName = GetNotEmptyValueFromList(config.PackageDisplayName, config.PackageName, "DisplayName");
        var description = GetNotEmptyValueFromList(config.PackageDescription, config.PackageName, "Description");
        GetOrCreateNode(properties, "DisplayName", defaultNamespace).Value = displayName;
        GetOrCreateNode(properties, "Description", defaultNamespace).Value = description;
        GetOrCreateNode(properties, "PublisherDisplayName", defaultNamespace).Value = GetNotEmptyValueFromList(config.PublisherDisplayName, config.PublisherName, "Publisher");
        GetOrCreateNode(properties, "Logo", defaultNamespace).Value = "Assets\\StoreLogo.png";

        XElement applicationsNode = GetOrCreateNode(package, "Applications", defaultNamespace);

        HashSet<string> usedNames = [];
        foreach (string item in entryPoints)
        {
            XElement applicationNode = CreateApplicationNodeFromExe(baseDirectory, item, displayName, description);
            applicationsNode.Add(applicationNode);

            string idCandidate = Regex.Replace(Path.GetFileNameWithoutExtension(item), "[^a-zA-z0-9_]+", string.Empty);
            if (!usedNames.Add(idCandidate))
            {
                int index = 1;
                string baseIdCandidate = idCandidate;
                while (!usedNames.Add(baseIdCandidate + "_" + index))
                {
                    index++;
                }

                idCandidate = baseIdCandidate + "_" + index;
            }

            applicationNode.SetAttributeValue("Id", idCandidate);
        }

        MsixHeroBrandingInjector branding = new();
        await branding.Inject(template, MsixHeroBrandingInjector.BrandingInjectorOverrideOption.PreferIncoming);
    }

    private static XElement CreateApplicationNodeFromExe(DirectoryInfo directoryInfo, string relativePath, string displayName, string description)
    {
        string fullFilePath = Path.Combine(directoryInfo.FullName, relativePath);
        if (!File.Exists(fullFilePath))
        {
            throw new FileNotFoundException($"File '{relativePath}' was not found in base directory {directoryInfo.FullName}.");
        }

        XNamespace defaultNamespace = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
        XNamespace nsUap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
        XNamespace nsDesktop4 = "http://schemas.microsoft.com/appx/manifest/desktop/windows10/4";
        XElement applicationNode = new(defaultNamespace + "Application");

        XElement visualElements = new(nsUap + "VisualElements");

        FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(fullFilePath);
        displayName = GetNotEmptyValueFromList(3, displayName, fileInfo.ProductName, fileInfo.InternalName, Path.GetFileNameWithoutExtension(fullFilePath), "Package display name");
        description = GetNotEmptyValueFromList(3, description, fileInfo.FileDescription, fileInfo.ProductName, Path.GetFileNameWithoutExtension(fullFilePath), "Package description");
        string appId = GetNotEmptyValueFromList(3, Regex.Replace(Path.GetFileNameWithoutExtension(relativePath), "[^a-zA-z0-9_]+", string.Empty), "App1");

        applicationNode.SetAttributeValue("Id", appId);
        applicationNode.SetAttributeValue("EntryPoint", "Windows.FullTrustApplication");
        applicationNode.SetAttributeValue("Executable", relativePath);
        applicationNode.SetAttributeValue(nsDesktop4 + "SupportsMultipleInstances", "true");

        visualElements.SetAttributeValue("DisplayName", displayName);
        visualElements.SetAttributeValue("Square150x150Logo", "Assets\\Square150x150Logo.png");
        visualElements.SetAttributeValue("Square44x44Logo", "Assets\\Square44x44Logo.png");
        visualElements.SetAttributeValue("BackgroundColor", "transparent");
        visualElements.SetAttributeValue("Description", description);

        applicationNode.Add(visualElements);

        XElement defaultTileElement = new(nsUap + "DefaultTile");
        defaultTileElement.SetAttributeValue("Wide310x150Logo", "Assets\\Wide310x150Logo.png");
        defaultTileElement.SetAttributeValue("Square71x71Logo", "Assets\\SmallTile.png");
        defaultTileElement.SetAttributeValue("Square310x310Logo", "Assets\\LargeTile.png");

        visualElements.Add(defaultTileElement);

        return applicationNode;
    }

    private static string GetNotEmptyValueFromList(int minimumLength, params string?[] values)
    {
        if (minimumLength <= 0)
        {
            return GetNotEmptyValueFromList(values);
        }

        return values.FirstOrDefault(v => !string.IsNullOrEmpty(v) && v.Length >= minimumLength) ?? string.Empty;
    }

    private static string GetNotEmptyValueFromList(params string?[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? string.Empty;
    }

    private static XElement GetOrCreateNode(XContainer xmlNode, string name, XNamespace? nameSpace = null)
    {
        XElement? node = nameSpace == null ? xmlNode.Descendants(name).FirstOrDefault() : xmlNode.Descendants(nameSpace + name).FirstOrDefault();
        if (node == null)
        {
            node = new XElement(nameSpace! + name);
            xmlNode.Add(node);
        }

        return node;
    }

    private static string GetBundledResourcePath(string localName)
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", localName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(string.Format("Could not locate resource {0}.", path), path);
        }

        return path;
    }
}
