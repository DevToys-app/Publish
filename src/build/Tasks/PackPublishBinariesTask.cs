using System.Threading.Tasks;
using Core;
using Nuke.Common.IO;
using Serilog;

namespace Tasks;

internal static class PackPublishBinariesTask
{
    internal static async Task RunAsync(AbsolutePath rootDirectory, SubmoduleBase[] submodules, Configuration configuration)
    {
        try
        {
            AbsolutePath artifactDirectory = rootDirectory / "artifacts";
            artifactDirectory.CreateOrCleanDirectory();

            foreach (SubmoduleBase submodule in submodules)
            {
                await submodule.PackPublishBinariesAsync(rootDirectory, artifactDirectory, configuration);
            }
        }
        catch (System.Exception exception)
        {
            Log.Error(exception, "An error occurred while packing the publishing binaries.");
            throw;
        }
    }
}
