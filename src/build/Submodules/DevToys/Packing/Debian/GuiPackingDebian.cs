using System.IO;
using System.Threading.Tasks;
using Core;
using Helper;
using Nuke.Common.IO;
using Nuke.Common.Utilities.Collections;
using Serilog;
using Submodules.DevToys.PublishBinariesBuilders;

namespace Submodules.DevToys.Packing.Debian;

public static class GuiPackingDebian
{
    internal static async Task PackAsync(
        AbsolutePath rootDirectory,
        AbsolutePath packDirectory,
        GuiDebianPublishBinariesBuilder guiDebianPublishBinariesBuilder)
    {
        // Adjusting permission of dll files. See https://github.com/DevToys-app/DevToys/issues/1189#issuecomment-2174302607
        await ShellHelper.RunCommandAsync($"find \"{guiDebianPublishBinariesBuilder.OutputPath}\" -type f -name \"*.dll\" -exec chmod -x {{}} \\;");
        
        PackZip(packDirectory, guiDebianPublishBinariesBuilder);
        await PackDebAsync(rootDirectory, packDirectory, guiDebianPublishBinariesBuilder);
    }

    private static void PackZip(AbsolutePath packDirectory, GuiDebianPublishBinariesBuilder guiDebianPublishBinariesBuilder)
    {
        Log.Information("Zipping DevToys {architecutre} (self-contained: {portable})...", guiDebianPublishBinariesBuilder.Architecture.RuntimeIdentifier, guiDebianPublishBinariesBuilder.SelfContained);

        AbsolutePath archiveFile = packDirectory / $"devtoys_{guiDebianPublishBinariesBuilder.Architecture.RuntimeIdentifierForFileName}_portable.zip";

        if (guiDebianPublishBinariesBuilder.OutputPath.DirectoryExists())
        {
            guiDebianPublishBinariesBuilder.OutputPath.ZipTo(
                archiveFile,
                filter: null,
                compressionLevel: System.IO.Compression.CompressionLevel.SmallestSize,
                fileMode: System.IO.FileMode.Create);
        }

        Log.Information(string.Empty);
    }

    private static async Task PackDebAsync(AbsolutePath rootDirectory, AbsolutePath packDirectory, GuiDebianPublishBinariesBuilder guiDebianPublishBinariesBuilder)
    {
        Log.Information("Creating a DEB package for DevToys {architecutre}...", guiDebianPublishBinariesBuilder.Architecture.RuntimeIdentifier);

        string platform = guiDebianPublishBinariesBuilder.Architecture.PlatformTarget;
        AbsolutePath debianBuildConfigFolder = rootDirectory / "assets" / "packing" / "debian" / $"devtoys.gui-{platform}";
        AbsolutePath tempFolder = debianBuildConfigFolder.Parent / "temp";
        tempFolder.DeleteDirectory();

        if (!debianBuildConfigFolder.DirectoryExists())
        {
            throw new DirectoryNotFoundException($"Unable to find '{debianBuildConfigFolder}' directory");
        }

        IOHelper.CopyDirectory(debianBuildConfigFolder, tempFolder, recursive: true);

        // Update changelog & icon.
        DebianBuildConfigHelper.UpdateChangelog(tempFolder);
        DebianBuildConfigHelper.UpdateIcon(tempFolder);

        // BuildOrPreview the .deb file
        if (guiDebianPublishBinariesBuilder.Architecture == TargetCpuArchitecture.Linux_Arm)
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
            .ForEach(file => file.Move(packDirectory / $"devtoys_{guiDebianPublishBinariesBuilder.Architecture.RuntimeIdentifierForFileName}.deb"));
        debianFolder
            .GetFiles("*.changes")
            .ForEach(f => f.DeleteFile());
        debianFolder
            .GetFiles("*.buildinfo")
            .ForEach(f => f.DeleteFile());

        Log.Information(string.Empty);
    }
}
