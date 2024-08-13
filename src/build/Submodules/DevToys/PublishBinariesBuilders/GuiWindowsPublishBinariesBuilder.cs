using Core;
using Helper;
using Microsoft.Build.Evaluation;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace Submodules.DevToys.PublishBinariesBuilders;

internal sealed class GuiWindowsPublishBinariesBuilder : PublishBinariesBuilder
{
    private readonly AbsolutePath _projectPath;

    public GuiWindowsPublishBinariesBuilder(
        AbsolutePath submodulePath,
        TargetCpuArchitecture architecture)
        : base("DevToys GUI (Windows)", architecture, selfContained: true)
    {
        _projectPath = submodulePath / "src" / "app" / "dev" / "platforms" / "desktop" / "DevToys.Windows" / "DevToys.Windows.csproj";
    }

    internal override void Build(AbsolutePath publishDirectory, AbsolutePath assetsDirectory, Configuration configuration)
    {
        AbsolutePath outputPath = publishDirectory / Architecture.PlatformTarget / $"{_projectPath.NameWithoutExtension}-{Architecture.RuntimeIdentifier}";

        Microsoft.Build.Evaluation.Project project = ProjectModelTasks.ParseProject(_projectPath);

        if (VersionHelper.IsPreview)
        {
            project.SetProperty("ApplicationIcon", "$(RepoRoot)\\assets\\logo\\Windows-Linux\\Preview\\Icon-Windows-Preview.ico");
        }
        else
        {
            project.SetProperty("ApplicationIcon", "$(RepoRoot)\\assets\\logo\\Windows-Linux\\Stable\\Icon-Windows.ico");
        }

        project.Save(_projectPath);

        ProjectProperty targetFramework = project.GetProperty("TargetFramework");

        DotNetPublish(
            s => s
            .SetProject(_projectPath)
            .SetConfiguration(configuration)
            .SetFramework(targetFramework.EvaluatedValue)
            .SetRuntime(Architecture.RuntimeIdentifier)
            .SetPlatform(Architecture.PlatformTarget)
            .SetSelfContained(SelfContained)
            .SetPublishSingleFile(false)
            .SetPublishReadyToRun(false)
            .SetPublishTrimmed(false)
            .SetVerbosity(DotNetVerbosity.quiet)
            .SetProcessArgumentConfigurator(_ => _
                .Add("/p:RuntimeIdentifierOverride=" + Architecture.RuntimeIdentifier)
                .Add("/p:Unpackaged=" + SelfContained)
                .Add($"/bl:\"{outputPath}.binlog\""))
            .SetOutput(outputPath));

        AbsolutePath blazorContentFolder = outputPath / "wwwroot" / "_content";
        blazorContentFolder.DeleteDirectory();

        AbsolutePath licenseFile = assetsDirectory / "LICENSE.md";
        FileSystemTasks.CopyFile(licenseFile, outputPath / "LICENSE.md", FileExistsPolicy.OverwriteIfNewer);

        NuGetHelper.UnpackNuGetPackage(
            NuGetHelper.FindDevToysToolsNuGetPackage(publishDirectory),
            outputPath / "Plugins" / "DevToys.Tools");

        OutputPath = outputPath;
    }
}
