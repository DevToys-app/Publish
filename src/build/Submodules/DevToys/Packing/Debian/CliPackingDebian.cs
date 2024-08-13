using System.IO;
using System.Threading.Tasks;
using Core;
using Helper;
using Nuke.Common.IO;
using Nuke.Common.Utilities.Collections;
using Serilog;
using Submodules.DevToys.PublishBinariesBuilders;

namespace Submodules.DevToys.Packing.Debian;

internal static class CliPackingDebian
{
    internal static async Task PackAsync(AbsolutePath rootDirectory, AbsolutePath packDirectory, CliPublishBinariesBuilder cliPublishBinariesBuilder)
    {
        // Adjusting permission of dll files. See https://github.com/DevToys-app/DevToys/issues/1189#issuecomment-2174302607
        await ShellHelper.RunCommandAsync($"find \"{cliPublishBinariesBuilder.OutputPath}\" -type f -name \"*.dll\" -exec chmod -x {{}} \\;");

        PackZip(packDirectory, cliPublishBinariesBuilder);
        await PackDebAsync(rootDirectory, packDirectory, cliPublishBinariesBuilder);
    }

    private static void PackZip(AbsolutePath packDirectory, CliPublishBinariesBuilder cliPublishBinariesBuilder)
    {
        Log.Information("Zipping DevToys CLI {architecutre} (self-contained: {portable})...", cliPublishBinariesBuilder.Architecture.RuntimeIdentifier, cliPublishBinariesBuilder.SelfContained);

        string portable = string.Empty;
        if (cliPublishBinariesBuilder.SelfContained)
        {
            portable = "_portable";
        }

        AbsolutePath archiveFile = packDirectory / $"devtoys.cli_{cliPublishBinariesBuilder.Architecture.RuntimeIdentifierForFileName}{portable}.zip";

        if (cliPublishBinariesBuilder.OutputPath.DirectoryExists())
        {
            cliPublishBinariesBuilder.OutputPath.ZipTo(
                archiveFile,
                filter: null,
                compressionLevel: System.IO.Compression.CompressionLevel.SmallestSize,
                fileMode: System.IO.FileMode.Create);
        }

        Log.Information(string.Empty);
    }

    private static async Task PackDebAsync(AbsolutePath rootDirectory, AbsolutePath packDirectory, CliPublishBinariesBuilder cliPublishBinariesBuilder)
    {
        if (!cliPublishBinariesBuilder.SelfContained)
        {
            return;
        }

        Log.Information("Creating a DEB package for DevToys CLI {architecutre}...", cliPublishBinariesBuilder.Architecture.RuntimeIdentifier);

        string platform = cliPublishBinariesBuilder.Architecture.PlatformTarget;
        AbsolutePath debianBuildConfigFolder = rootDirectory / "assets" / "packing" / "debian" / $"devtoys.cli-{platform}";
        AbsolutePath tempFolder = debianBuildConfigFolder.Parent / "temp";
        tempFolder.DeleteDirectory();

        if (!debianBuildConfigFolder.DirectoryExists())
        {
            throw new DirectoryNotFoundException($"Unable to find '{debianBuildConfigFolder}' directory");
        }

        IOHelper.CopyDirectory(debianBuildConfigFolder, tempFolder, recursive: true);

        // Update changelog.
        DebianBuildConfigHelper.UpdateChangelog(tempFolder);

        // BuildOrPreview the .deb file
        if (cliPublishBinariesBuilder.Architecture == TargetCpuArchitecture.Linux_Arm)
        {
            await ShellHelper.RunCommandAsync("dpkg-buildpackage -aarm64 -Pcross -b -uc -us -D", tempFolder);
        }
        else
        {
            await ShellHelper.RunCommandAsync("dpkg-buildpackage -b -uc -us -D", tempFolder);
        }

        tempFolder.DeleteDirectory();

        AbsolutePath debianFolder = rootDirectory / "assets" / "packing" / "debian";
        debianFolder
            .GetFiles("*.deb")
            .ForEach(file => file.Move(packDirectory / $"devtoys.cli_{cliPublishBinariesBuilder.Architecture.RuntimeIdentifierForFileName}.deb"));
        debianFolder
            .GetFiles("*.changes")
            .ForEach(f => f.DeleteFile());
        debianFolder
            .GetFiles("*.buildinfo")
            .ForEach(f => f.DeleteFile());

        Log.Information(string.Empty);
    }
}
