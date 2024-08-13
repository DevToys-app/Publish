using System.Collections.Generic;
using System.IO;
using Core;
using Helper;
using Microsoft.Build.Evaluation;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace Submodules.DevToys.PublishBinariesBuilders;

internal sealed class GuiMacOSPublishBinariesBuilder : PublishBinariesBuilder
{
    private readonly AbsolutePath _projectPath;
    private readonly AbsolutePath _submodulePath;

    public GuiMacOSPublishBinariesBuilder(
        AbsolutePath submodulePath,
        TargetCpuArchitecture architecture)
        : base("DevToys GUI (MacOS)", architecture, selfContained: true)
    {
        _projectPath = submodulePath / "src" / "app" / "dev" / "platforms" / "desktop" / "DevToys.MacOS" / "DevToys.MacOS.csproj";
        _submodulePath = submodulePath;
    }

    internal override void Build(AbsolutePath publishDirectory, AbsolutePath assetsDirectory, Configuration configuration)
    {
        AbsolutePath outputPath = publishDirectory / Architecture.PlatformTarget / $"{_projectPath.NameWithoutExtension}-{Architecture.RuntimeIdentifier}";

        AbsolutePath appIconPath = (AbsolutePath)Path.GetDirectoryName(_projectPath)! / "Assets.xcassets" / "AppIcon.appiconset";
        appIconPath.GetFiles("*.png").ForEach(file => file.DeleteFile());
        if (VersionHelper.IsPreview)
        {
            File.Copy(_submodulePath / "assets" / "logo" / "MacOS" / "Preview" / "16-mac.png", appIconPath / "16-mac.png");
            File.Copy(_submodulePath / "assets" / "logo" / "MacOS" / "Preview" / "32-mac.png", appIconPath / "32-mac.png");
            File.Copy(_submodulePath / "assets" / "logo" / "MacOS" / "Preview" / "64-mac.png", appIconPath / "64-mac.png");
            File.Copy(_submodulePath / "assets" / "logo" / "MacOS" / "Preview" / "128-mac.png", appIconPath / "128-mac.png");
            File.Copy(_submodulePath / "assets" / "logo" / "MacOS" / "Preview" / "256-mac.png", appIconPath / "256-mac.png");
            File.Copy(_submodulePath / "assets" / "logo" / "MacOS" / "Preview" / "512-mac.png", appIconPath / "512-mac.png");
            File.Copy(_submodulePath / "assets" / "logo" / "MacOS" / "Preview" / "1024-mac.png", appIconPath / "1024-mac.png");
        }
        else
        {
            File.Copy(_submodulePath / "assets" / "logo" / "MacOS" / "Stable" / "16-mac.png", appIconPath / "16-mac.png");
            File.Copy(_submodulePath / "assets" / "logo" / "MacOS" / "Stable" / "32-mac.png", appIconPath / "32-mac.png");
            File.Copy(_submodulePath / "assets" / "logo" / "MacOS" / "Stable" / "64-mac.png", appIconPath / "64-mac.png");
            File.Copy(_submodulePath / "assets" / "logo" / "MacOS" / "Stable" / "128-mac.png", appIconPath / "128-mac.png");
            File.Copy(_submodulePath / "assets" / "logo" / "MacOS" / "Stable" / "256-mac.png", appIconPath / "256-mac.png");
            File.Copy(_submodulePath / "assets" / "logo" / "MacOS" / "Stable" / "512-mac.png", appIconPath / "512-mac.png");
            File.Copy(_submodulePath / "assets" / "logo" / "MacOS" / "Stable" / "1024-mac.png", appIconPath / "1024-mac.png");
        }

        AbsolutePath projectFolder = Path.GetDirectoryName(_projectPath);

        // Copy DevToys.Tools to the project folder
        NuGetHelper.UnpackNuGetPackage(
            NuGetHelper.FindDevToysToolsNuGetPackage(publishDirectory),
            projectFolder / "Plugins" / "DevToys.Tools");

        Microsoft.Build.Evaluation.Project project = ProjectModelTasks.ParseProject(_projectPath);

        // Mark DevToys.Tools as a resource to bundle (and sign).
        IEnumerable<AbsolutePath>? devToysToolFiles =
            (projectFolder / "Plugins" / "DevToys.Tools")
            .GetFiles(pattern: "*", depth: int.MaxValue);
        foreach (AbsolutePath file in devToysToolFiles)
        {
            if (file.Name == "DevToys.Tools.deps.json"
                || file.Name == "DevToys.Tools.runtimeconfig.json")
            {
                // Don't include it or we get an error "{file} would result in a file outside of the app bundle and cannot be used.".
                file.DeleteFile();
                continue;
            }
            project.AddItem("BundleResource", projectFolder.GetRelativePathTo(file));
        }

        project.Save();
        ProjectProperty targetFramework = project.GetProperty("TargetFramework");

        if (string.IsNullOrWhiteSpace(AppleCredentials.AppleCodesignKey))
        {
            Log.Error("The 'AppleCodesignKey' parameter is missing.");
        }

        DotNetBuild(
            s => s
            .SetProjectFile(_projectPath)
            .SetConfiguration(configuration)
            .SetFramework(targetFramework.EvaluatedValue)
            .SetRuntime(Architecture.RuntimeIdentifier)
            .SetPlatform(Architecture.PlatformTarget)
            .SetSelfContained(SelfContained)
            .SetPublishSingleFile(false)
            .SetPublishReadyToRun(false)
            .SetPublishTrimmed(true) // HACK: Required for MacOS. However, <LinkMode>None</LinkMode> in the CSPROJ disables trimming.
            .SetVerbosity(DotNetVerbosity.quiet)
            .SetProcessArgumentConfigurator(_ => _
                .Add($"-maxcpucount:1") // Disable parallel build.
                .Add($"/p:RuntimeIdentifierOverride=" + Architecture.RuntimeIdentifier)
                .Add($"/p:CreatePackage=False") /* Will NOT create an installable .pkg */
                .Add($"/p:CodesignKey=\"{AppleCredentials.AppleCodesignKey}\"")
                .Add($"/p:CodesignProvision=\"DevToys\"")
                .Add($"/p:CodesignEntitlements=\"{projectFolder / "Entitlements.plist"}\"")
                .Add($"/p:UseHardenedRuntime=true")
                .Add($"/p:EnableCodeSigning=true")
                .Add($"/p:MtouchLink=SdkOnly")
                .Add($"/bl:\"{outputPath}.binlog\""))
            .SetOutputDirectory(outputPath));

        OutputPath = outputPath;
    }
}
