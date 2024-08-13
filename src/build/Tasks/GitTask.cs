using System.Threading.Tasks;
using Helper;
using Serilog;

namespace Tasks;

internal static class GitTask
{
    internal static async Task UpdateSubmodulesAsync()
    {
        try
        {
            Log.Information("Revert local changes in submodules.");
            await ShellHelper.RunCommandAsync("git submodule foreach --recursive git reset --hard");
            await ShellHelper.RunCommandAsync("git submodule foreach --recursive git clean -fd");

            Log.Information("Clone submodules.");
            await ShellHelper.RunCommandAsync("git submodule update --init --recursive");

            Log.Information("Update submodules to the latest commit.");
            await ShellHelper.RunCommandAsync("git submodule update --remote --merge");

            Log.Information("Submodules updated. You may need to push the changes.");
        }
        catch (System.Exception exception)
        {
            Log.Error(exception, "An error occurred while updating the submodules.");
            throw;
        }
    }
}
