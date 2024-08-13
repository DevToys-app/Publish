using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core;
using Helper;
using Nuke.Common.IO;
using Serilog;

namespace Tasks;

internal static class UpdateVersionTask
{
    internal static async Task RunAsync(
        int majorVersion,
        int minorVersion,
        int buildOrPreviewNumber,
        bool isPreview,
        SubmoduleBase[] submodules)
    {
        Log.Information("Updating version to {MajorVersion}.{MinorVersion}.{PreviewNumber} (IsPreview = {IsPreview}).", majorVersion, minorVersion, buildOrPreviewNumber, isPreview);

        VersionHelper.Major = majorVersion;
        VersionHelper.Minor = minorVersion;
        VersionHelper.BuildOrPreview = buildOrPreviewNumber;
        VersionHelper.IsPreview = isPreview;

        foreach (SubmoduleBase submodule in submodules)
        {
            IEnumerable<AbsolutePath> projectFiles = submodule.RepositoryDirectory.GetFiles("*.csproj", depth: int.MaxValue);
            IEnumerable<AbsolutePath> plistFiles = submodule.RepositoryDirectory.GetFiles("*.plist", depth: int.MaxValue);
            IEnumerable<AbsolutePath> nuspecFiles = submodule.RepositoryDirectory.GetFiles("*.nuspec", depth: int.MaxValue);
            IEnumerable<AbsolutePath> sharedAssemblyVersionFiles = submodule.RepositoryDirectory.GetFiles("*AssemblyVersion.cs", depth: int.MaxValue);

            await UpdateProjectFilesAsync(projectFiles);
            await UpdatePListFilesAsync(plistFiles);
            await UpdateNuspecFilesAsync(nuspecFiles);
            await UpdateSharedAssemblyVersionFilesAsync(sharedAssemblyVersionFiles);
        }

        Log.Information(string.Empty);
    }

    private static async Task UpdatePListFilesAsync(IEnumerable<AbsolutePath>? plistFiles)
    {
        if (plistFiles is null)
        {
            return;
        }

        foreach (AbsolutePath plistFile in plistFiles)
        {
            string plistFileContent = await File.ReadAllTextAsync(plistFile);

            if (plistFileContent.Contains("CFBundleVersion"))
            {
                string bundleVersion = VersionHelper.GetVersionString(allowPreviewSyntax: false);

                string[] lines = plistFileContent.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("<key>CFBundleShortVersionString</key>"))
                    {
                        lines[i + 1] = $"\t<string>{bundleVersion}</string>";
                    }
                    else if (lines[i].Contains("<key>CFBundleVersion</key>"))
                    {
                        lines[i + 1] = $"\t<string>{bundleVersion}</string>";
                    }
                }

                string newPlistFileContent = string.Join('\n', lines);

                await File.WriteAllTextAsync(plistFile, newPlistFileContent);

                if (plistFileContent != newPlistFileContent)
                {
                    Log.Information("Updated {PlistFile}", plistFile);
                }
                else
                {
                    Log.Error("Failed to update version number in {PlistFile}", plistFile);
                    throw new IOException("Failed to update version number in " + plistFile);
                }
            }
        }
    }

    private static async Task UpdateNuspecFilesAsync(IEnumerable<AbsolutePath>? nuspecFiles)
    {
        if (nuspecFiles is null)
        {
            return;
        }

        foreach (AbsolutePath nuspecFile in nuspecFiles)
        {
            string nuspecFileContent = await File.ReadAllTextAsync(nuspecFile);

            string version = VersionHelper.GetVersionString(allowPreviewSyntax: true);

            string newNuspecFileContent
                = nuspecFileContent.Replace(
                    "<version>0.0.0-preview.0</version>",
                    $"<version>{version}</version>");

            await File.WriteAllTextAsync(nuspecFile, newNuspecFileContent);

            if (nuspecFileContent != newNuspecFileContent)
            {
                Log.Information("Updated {nuspecFile}", nuspecFile);
            }
            else
            {
                Log.Error("Failed to update version number in {nuspecFile}", nuspecFile);
                throw new IOException("Failed to update version number in " + nuspecFile);
            }
        }
    }

    private static async Task UpdateProjectFilesAsync(IEnumerable<AbsolutePath>? projectFiles)
    {
        if (projectFiles is null)
        {
            return;
        }

        foreach (AbsolutePath projectFile in projectFiles)
        {
            string projectFileContent = await File.ReadAllTextAsync(projectFile);

            if (projectFileContent.Contains("<PackageProjectUrl>"))
            {
                string version = VersionHelper.GetVersionString(allowPreviewSyntax: true);

                string newProjectFileContent
                     = projectFileContent.Replace(
                         "<Version>0.0.0-preview.0</Version>",
                         $"<Version>{version}</Version>");

                await File.WriteAllTextAsync(projectFile, newProjectFileContent);

                if (projectFileContent != newProjectFileContent)
                {
                    Log.Information("Updated {projectFile}", projectFile);
                }
                else
                {
                    Log.Warning("Failed to update version number in {projectFile}", projectFile);
                    throw new IOException("Failed to update version number in " + projectFile);
                }
            }
        }
    }

    private static async Task UpdateSharedAssemblyVersionFilesAsync(IEnumerable<AbsolutePath>? sharedAssemblyVersionFiles)
    {
        if (sharedAssemblyVersionFiles is null)
        {
            return;
        }

        foreach (AbsolutePath sharedAssemblyVersionFile in sharedAssemblyVersionFiles)
        {
            string sharedAssemblyVersionFileContent = await File.ReadAllTextAsync(sharedAssemblyVersionFile);

            string assemblyVersion = VersionHelper.GetVersionString(allowPreviewSyntax: false);
            string assemblyInformationalVersion = VersionHelper.GetVersionString(allowPreviewSyntax: true, allowDisplayedSyntax: true);

            string newSharedAssemblyVersionFileContent
                = sharedAssemblyVersionFileContent.Replace(
                    $"[assembly: AssemblyVersion(\"0.0.0.0\")]",
                    $"[assembly: AssemblyVersion(\"{assemblyVersion}\")]");
            newSharedAssemblyVersionFileContent
                = newSharedAssemblyVersionFileContent.Replace(
                    $"[assembly: AssemblyFileVersion(\"0.0.0.0\")]",
                    $"[assembly: AssemblyFileVersion(\"{assemblyVersion}\")]");
            newSharedAssemblyVersionFileContent
                = newSharedAssemblyVersionFileContent.Replace(
                    $"[assembly: AssemblyInformationalVersion(\"0.0.0-preview.0\")]",
                    $"[assembly: AssemblyInformationalVersion(\"{assemblyInformationalVersion}\")]");

            await File.WriteAllTextAsync(sharedAssemblyVersionFile, newSharedAssemblyVersionFileContent);

            if (sharedAssemblyVersionFileContent != newSharedAssemblyVersionFileContent)
            {
                Log.Information("Updated {sharedAssemblyVersionFile}", sharedAssemblyVersionFile);
            }
            else
            {
                Log.Error("Failed to update version number in {sharedAssemblyVersionFile}", sharedAssemblyVersionFile);
                throw new IOException("Failed to update version number in " + sharedAssemblyVersionFile);
            }
        }
    }
}
