using System;
using System.IO;
using Nuke.Common.IO;

namespace Helper;

public static class DebianBuildConfigHelper
{
    public static void UpdateChangelog(AbsolutePath debianBuildConfigFolder)
    {
        AbsolutePath changelogFile = debianBuildConfigFolder / "debian" / "changelog";
        string changelogFileContent = File.ReadAllText(changelogFile);

        // Update date
        DateTime dateTime = DateTime.Now;
        string formattedDate = dateTime.ToString("ddd, dd MMM yyyy HH:mm:ss zzz");
        changelogFileContent = changelogFileContent.Replace("[DATE]", formattedDate);

        // Update version
        changelogFileContent
            = changelogFileContent.Replace(
                "(0.0.0)",
                "(" + VersionHelper.GetVersionString(allowPreviewSyntax: false) + ")");

        File.WriteAllText(changelogFile, changelogFileContent);
    }

    public static void UpdateIcon(AbsolutePath debianBuildConfigFolder)
    {
        UpdateRulesFile(debianBuildConfigFolder);
        UpdateDesktopFile(debianBuildConfigFolder);
    }

    private static void UpdateRulesFile(AbsolutePath debianBuildConfigFolder)
    {
        AbsolutePath rulesFile = debianBuildConfigFolder / "debian" / "rules";
        string rulesFileContent = File.ReadAllText(rulesFile);

        // Find the line starting with "ICON_FILE_SRC_PATH" and replace it with the right icon
        string[] lines = rulesFileContent.Split(Environment.NewLine);
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("ICON_FILE_SRC_PATH"))
            {
                if (VersionHelper.IsPreview)
                {
                    lines[i] = "ICON_FILE_SRC_PATH=../../../../submodules/DevToys/assets/logo/Windows-Linux/Preview/Icon-Windows-Linux-Preview.png";
                }
                else
                {
                    lines[i] = "ICON_FILE_SRC_PATH=../../../../submodules/DevToys/assets/logo/Windows-Linux/Stable/Icon-Windows-Linux.png";
                }
                break;
            }
        }

        rulesFileContent = string.Join(Environment.NewLine, lines);

        File.WriteAllText(rulesFile, rulesFileContent);
    }

    private static void UpdateDesktopFile(AbsolutePath debianBuildConfigFolder)
    {
        AbsolutePath desktopFile = debianBuildConfigFolder / "desktop-file" / "devtoys.desktop";
        string desktopFileContent = File.ReadAllText(desktopFile);

        // Find the line starting with "Icon" and replace it with the right icon
        string[] lines = desktopFileContent.Split(Environment.NewLine);
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("Icon"))
            {
                if (VersionHelper.IsPreview)
                {
                    lines[i] = "Icon=/opt/devtoys/devtoys/Icon-Windows-Linux-Preview.png";
                }
                else
                {
                    lines[i] = "Icon=/opt/devtoys/devtoys/Icon-Windows-Linux.png";
                }
                break;
            }
        }

        desktopFileContent = string.Join(Environment.NewLine, lines);

        File.WriteAllText(desktopFile, desktopFileContent);
    }
}
