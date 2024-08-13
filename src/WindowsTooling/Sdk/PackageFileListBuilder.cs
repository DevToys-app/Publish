using System.Text;

namespace WindowsTooling.Sdk;

public class PackageFileListBuilder
{
    private readonly IDictionary<string, string> _sourceFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly IList<SourceDirectory> _sourceDirectories = new List<SourceDirectory>();

    public void AddFile(string sourceFile, string targetRelativeFilePath)
    {
        _sourceFiles[targetRelativeFilePath] = sourceFile.Replace("/", "\\");
    }

    public void AddDirectory(string sourceDirectory, string wildcard, bool recursive, string targetRelativeDirectory)
    {
        _sourceDirectories.Add(new SourceDirectory(sourceDirectory, targetRelativeDirectory, wildcard, recursive));
    }

    public void AddDirectory(string sourceDirectory, string wildcard, string targetRelativeDirectory)
    {
        AddDirectory(sourceDirectory, wildcard, false, targetRelativeDirectory);
    }

    public void AddDirectory(string sourceDirectory, bool recursive, string targetRelativeDirectory)
    {
        AddDirectory(sourceDirectory, "*", recursive, targetRelativeDirectory);
    }

    public void AddDirectory(string sourceDirectory, string targetRelativeDirectory)
    {
        AddDirectory(sourceDirectory, "*", false, targetRelativeDirectory);
    }

    public void AddManifest(string sourceManifestFilePath)
    {
        _sourceFiles["AppxManifest.xml"] = sourceManifestFilePath;
    }

    /// <summary>
    /// Returns source path of the manifest file.
    /// </summary>
    /// <returns>The full source path to a manifest file.</returns>
    public string GetManifestSourcePath()
    {
        if (_sourceFiles.TryGetValue("appxmanifest.xml", out string? manifestPath))
        {
            return manifestPath;
        }

        foreach (SourceDirectory item in _sourceDirectories.Where(sd => string.IsNullOrEmpty(sd.TargetRelativePath)))
        {
            string? findManifestFiles = Directory.EnumerateFiles(item.SourcePath, "appxmanifest.xml", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (findManifestFiles != null)
            {
                return findManifestFiles;
            }
        }

        return null;
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append("[Files]");
        stringBuilder.AppendLine();

        HashSet<string> targetRelativeFilePaths = new(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, string> filesEnumeratedFromSourceDirectories = new(StringComparer.OrdinalIgnoreCase);
        foreach (SourceDirectory directory in _sourceDirectories)
        {
            foreach (string foundFile in Directory.EnumerateFiles(directory.SourcePath, directory.Wildcard, directory.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                if (!foundFile.StartsWith(directory.SourcePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string targetRelativePath = foundFile.Substring(directory.SourcePath.Length).TrimStart('\\');
                if (!string.IsNullOrEmpty(directory.TargetRelativePath))
                {
                    targetRelativePath = Path.Combine(directory.TargetRelativePath, targetRelativePath);
                }

                if (_sourceFiles.ContainsKey(targetRelativePath))
                {
                    continue;
                }

                filesEnumeratedFromSourceDirectories.Add(targetRelativePath, foundFile);
            }
        }

        foreach (KeyValuePair<string, string> item in filesEnumeratedFromSourceDirectories.Concat(_sourceFiles))
        {
            string source = item.Value;
            string target = item.Key;

            switch (target.ToLowerInvariant())
            {
                case "appxblockmap.xml":
                    break;
                case "appxsignature.p7x":
                    break;
                default:

                    if (!targetRelativeFilePaths.Add(target))
                    {
                        // File already added
                        continue;
                    }

                    stringBuilder.AppendLine($"\"{source}\"\t\"{target}\"");
                    break;
            }
        }

        return stringBuilder.ToString();
    }

    private struct SourceDirectory
    {
        public SourceDirectory(string sourcePath, string targetRelativePath, string wildcard, bool recursive)
        {
            SourcePath = sourcePath;
            TargetRelativePath = targetRelativePath;
            Wildcard = wildcard;
            Recursive = recursive;
        }

        public string SourcePath;
        public string TargetRelativePath;
        public string Wildcard;
        public bool Recursive;
    }
}
