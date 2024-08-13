using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Microsoft.Build.Evaluation;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace Submodules.DevToysTools;

internal sealed class DevToysToolsSubmodule : SubmoduleBase
{
    private AbsolutePath _outputPath;

    public DevToysToolsSubmodule(AbsolutePath repositoryDirectory)
        : base("DevToys.Tools", repositoryDirectory / "submodules" / "DevToys.Tools")
    {
    }

    internal override IEnumerable<AbsolutePath> GetSolutions()
    {
        yield return RepositoryDirectory / "src" / "DevToys.Tools.sln";
    }

    internal override ValueTask BuildPublishBinariesAsync(AbsolutePath publishDirectory, AbsolutePath assetsDirectory, Configuration configuration)
    {
        Log.Information("Building DevToys.Tools NuGet package");

        _outputPath = publishDirectory / $"DevToys.Tools";
        AbsolutePath projectPath = RepositoryDirectory / "src" / "DevToys.Tools" / "DevToys.Tools.csproj";

        Microsoft.Build.Evaluation.Project project = ProjectModelTasks.ParseProject(projectPath);
        ProjectProperty targetFramework = project.GetProperty("TargetFramework");

        DotNetPack(
            s => s
            .SetProject(projectPath)
            .SetConfiguration(configuration)
            .SetPublishSingleFile(false)
            .SetPublishReadyToRun(false)
            .SetPublishTrimmed(false)
            .SetVerbosity(DotNetVerbosity.quiet)
            .SetProcessArgumentConfigurator(_ => _
                .Add($"/bl:\"{_outputPath}.binlog\""))
            .SetOutputDirectory(_outputPath));

        Log.Information(string.Empty);

        return ValueTask.CompletedTask;
    }

    internal override ValueTask PackPublishBinariesAsync(AbsolutePath rootDirectory, AbsolutePath packDirectory, Configuration configuration)
    {
        Log.Information(messageTemplate: "Copying DevToys.Tools NuGet package to artifacts");

        _outputPath
            .GetFiles("*.nupkg")
            .ForEach(x => x
                .MoveToDirectory(packDirectory));

        Log.Information(string.Empty);

        return ValueTask.CompletedTask;
    }
}
