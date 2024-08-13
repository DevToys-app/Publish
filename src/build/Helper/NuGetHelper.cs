using System.IO;
using System.Linq;
using Nuke.Common.IO;
using Nuke.Common.Utilities.Collections;

namespace Helper;

internal class NuGetHelper
{
    internal static void UnpackNuGetPackage(AbsolutePath packagePath, AbsolutePath targetDirectory)
    {
        if (!packagePath.FileExists())
        {
            throw new FileNotFoundException(packagePath + " does not exist.");
        }

        packagePath
            .UnZipTo(targetDirectory);

        // Deleting this file is necessary to avoid MakeAPPX.exe error.
        targetDirectory
            .GetFiles(".rels", depth: 100)
            .ForEach(x => x
                .DeleteFile());
    }

    internal static AbsolutePath FindDevToysToolsNuGetPackage(AbsolutePath publishFolder)
    {
        return (publishFolder / "DevToys.Tools").GetFiles("*.nupkg").Single();
    }
}
