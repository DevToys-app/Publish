using System.Threading.Tasks;
using Core;
using Nuke.Common.IO;
using Serilog;

namespace Tasks;

internal static class CompilePublishBinariesTask
{
    internal static async ValueTask RunAsync(AbsolutePath rootDirectory, AbsolutePath assetsDirectory, SubmoduleBase[] submodules, Configuration configuration)
    {
        try
        {
            AbsolutePath publishDirectory = rootDirectory / "publish";
            publishDirectory.CreateOrCleanDirectory();

            foreach (SubmoduleBase submodule in submodules)
            {
                await submodule.BuildPublishBinariesAsync(publishDirectory, assetsDirectory, configuration);
            }
        }
        catch (System.Exception exception)
        {
            Log.Error(exception, "An error occurred while compiling the publishing binaries.");
            throw;
        }
    }
}
