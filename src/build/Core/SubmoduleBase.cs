using System.Collections.Generic;
using System.Threading.Tasks;
using Nuke.Common.IO;

namespace Core;

internal abstract class SubmoduleBase
{
    protected SubmoduleBase(string name, AbsolutePath repositoryDirectory)
    {
        Name = name;
        RepositoryDirectory = repositoryDirectory;
    }

    internal AbsolutePath RepositoryDirectory { get; }

    internal string Name { get; }

    internal virtual IEnumerable<AbsolutePath> GetDirectoriesToClean()
    {
        return RepositoryDirectory.GlobDirectories("bin", "obj", "packages", "publish");
    }

    internal virtual ValueTask RestoreAsync()
    {
        return ValueTask.CompletedTask;
    }

    internal virtual IEnumerable<AbsolutePath> GetSolutions()
    {
        yield break;
    }

    internal virtual IEnumerable<AbsolutePath> GetTestProjects()
    {
        return RepositoryDirectory.GlobFiles("**/*Tests.csproj");
    }

    internal virtual ValueTask BuildPublishBinariesAsync(AbsolutePath publishDirectory, AbsolutePath assetsDirectory, Configuration configuration)
    {
        return ValueTask.CompletedTask;
    }

    internal virtual ValueTask PackPublishBinariesAsync(AbsolutePath rootDirectory, AbsolutePath packDirectory, Configuration configuration)
    {
        return ValueTask.CompletedTask;
    }
}
