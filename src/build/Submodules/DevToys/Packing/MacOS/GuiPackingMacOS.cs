using System.Threading.Tasks;
using Core;
using Helper;
using Nuke.Common.IO;
using Serilog;
using Submodules.DevToys.PublishBinariesBuilders;

namespace Submodules.DevToys.Packing.MacOS;

internal static class GuiPackingMacOS
{
    internal static async Task PackAsync(
        AbsolutePath packDirectory,
        AbsolutePath devToysRepositoryDirectory,
        GuiMacOSPublishBinariesBuilder guiMacOsPublishBinariesBuilder,
        Configuration configuration)
    {
        Log.Information("Zipping .app file of DevToys {architecutre}...",
            guiMacOsPublishBinariesBuilder.Architecture.RuntimeIdentifier);

        AbsolutePath appFileFolder = guiMacOsPublishBinariesBuilder.OutputPath / "appFile";
        appFileFolder.CreateOrCleanDirectory();

        AbsolutePath appFile = guiMacOsPublishBinariesBuilder.OutputPath / "DevToys.app";
        appFile.Move(appFileFolder / "DevToys.app");

        AbsolutePath archiveFile = packDirectory /
                                   $"devtoys_{guiMacOsPublishBinariesBuilder.Architecture.RuntimeIdentifierForFileName}.zip";

        appFileFolder.ZipTo(
            archiveFile,
            filter: null,
            compressionLevel: System.IO.Compression.CompressionLevel.SmallestSize,
            fileMode: System.IO.FileMode.Create);

        Log.Information("Notarizing .app file of DevToys {architecutre}...",
            guiMacOsPublishBinariesBuilder.Architecture.RuntimeIdentifier);

        if (string.IsNullOrWhiteSpace(AppleCredentials.AppleId))
        {
            Log.Error("The 'AppleId' parameter is missing.");
        }

        if (string.IsNullOrWhiteSpace(AppleCredentials.AppleAppPassword))
        {
            Log.Error("The 'AppleAppPassword' parameter is missing.");
        }

        if (string.IsNullOrWhiteSpace(AppleCredentials.AppleTeamId))
        {
            Log.Error("The 'AppleTeamId' parameter is missing.");
        }

        await ShellHelper.RunCommandAsync($"xcrun notarytool submit {archiveFile} --wait --apple-id {AppleCredentials.AppleId} --team-id {AppleCredentials.AppleTeamId} --password {AppleCredentials.AppleAppPassword}");

        Log.Information(string.Empty);
    }
}
