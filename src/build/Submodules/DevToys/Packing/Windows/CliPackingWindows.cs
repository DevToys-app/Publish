using Nuke.Common.IO;
using Serilog;
using Submodules.DevToys.PublishBinariesBuilders;

namespace Submodules.DevToys.Packing.Windows;

internal static class CliPackingWindows
{
    internal static void Pack(AbsolutePath packDirectory, CliPublishBinariesBuilder cliPublishBinariesBuilder)
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
}
