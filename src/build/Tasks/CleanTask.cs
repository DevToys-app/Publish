using System;
using Core;
using Nuke.Common.IO;
using Serilog;

namespace Tasks;

internal static class CleanTask
{
    internal static void Run(AbsolutePath rootDirectory, SubmoduleBase[] submodules)
    {
        try
        {
            foreach (SubmoduleBase submodule in submodules)
            {
                Log.Information("Cleaning {Value} repository.", submodule.Name);

                foreach (AbsolutePath directory in submodule.GetDirectoriesToClean())
                {
                    directory.CreateOrCleanDirectory();
                    Log.Information("Deleted {Value} directory.", directory);
                }
            }

            foreach (AbsolutePath directory in rootDirectory.GlobDirectories("bin", "obj", "packages", "publish", "artifacts"))
            {
                directory.CreateOrCleanDirectory();
                Log.Information("Deleted {Value} directory.", directory);
            }
        }
        catch (Exception exception)
        {
            Log.Error(exception, "An error occurred while cleaning the repository.");
            throw;
        }
    }
}
