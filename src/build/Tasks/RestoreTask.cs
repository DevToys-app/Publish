using System.Threading.Tasks;
using Core;
using Serilog;

namespace Tasks;

internal static class RestoreTask
{
    internal static async Task RunAsync(SubmoduleBase[] submodules)
    {
        try
        {
            foreach (SubmoduleBase submodule in submodules)
            {
                Log.Information("Restoring {Value} repository's dependencies.", submodule.Name);
                await submodule.RestoreAsync();
                Log.Information("Restoration completed.");
            }
        }
        catch (System.Exception exception)
        {
            Log.Error(exception, "An error occurred while restoring the repository's dependencies.");
            throw;
        }
    }
}
