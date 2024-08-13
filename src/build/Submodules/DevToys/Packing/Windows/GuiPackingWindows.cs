using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Core;
using Helper;
using InnoSetup.ScriptBuilder;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.InnoSetup;
using Serilog;
using Submodules.DevToys.PublishBinariesBuilders;
using WindowsTooling.AppxManifest;
using WindowsTooling.Progress;
using WindowsTooling.Sdk;

namespace Submodules.DevToys.Packing.Windows;

internal static class GuiPackingWindows
{
    internal static async Task PackAsync(AbsolutePath packDirectory, AbsolutePath devToysRepositoryDirectory, GuiWindowsPublishBinariesBuilder guiWindowsPublishBinariesBuilder)
    {
        Zip(packDirectory, guiWindowsPublishBinariesBuilder);
        CreateSetup(packDirectory, devToysRepositoryDirectory, guiWindowsPublishBinariesBuilder);
        await CreateMSIXAsync(packDirectory, devToysRepositoryDirectory, guiWindowsPublishBinariesBuilder);
    }

    private static void Zip(AbsolutePath packDirectory, GuiWindowsPublishBinariesBuilder guiWindowsPublishBinariesBuilder)
    {
        Log.Information("Zipping DevToys {architecutre}...", guiWindowsPublishBinariesBuilder.Architecture.RuntimeIdentifier);

        AbsolutePath archiveFile = packDirectory / $"devtoys_win_{guiWindowsPublishBinariesBuilder.Architecture.PlatformTarget}_portable.zip";

        if (guiWindowsPublishBinariesBuilder.OutputPath.DirectoryExists())
        {
            guiWindowsPublishBinariesBuilder.OutputPath.ZipTo(
                archiveFile,
                filter: null,
                compressionLevel: System.IO.Compression.CompressionLevel.SmallestSize,
                fileMode: FileMode.Create);
        }

        Log.Information(string.Empty);
    }

    private static void CreateSetup(AbsolutePath packDirectory, AbsolutePath devToysRepositoryDirectory, GuiWindowsPublishBinariesBuilder guiWindowsPublishBinariesBuilder)
    {
        Log.Information("Creating installer for DevToys {architecutre}...", guiWindowsPublishBinariesBuilder.Architecture.RuntimeIdentifier);

        string version = VersionHelper.GetVersionString(allowPreviewSyntax: true, allowDisplayedSyntax: true);
        AbsolutePath innoSetupScriptFile
            = GenerateInnoSetupScript(
                version,
                VersionHelper.IsPreview,
                packDirectory,
                devToysRepositoryDirectory,
                guiWindowsPublishBinariesBuilder);

        AbsolutePath innoSetupCompiler = NuGetToolPathResolver.GetPackageExecutable("Tools.InnoSetup", "ISCC.exe");

        InnoSetupTasks.InnoSetup(config => config
            .SetProcessToolPath(innoSetupCompiler)
            .SetScriptFile(innoSetupScriptFile)
            .SetOutputDir(packDirectory));

        Log.Information(string.Empty);
    }

    private static async Task CreateMSIXAsync(
        AbsolutePath packDirectory,
        AbsolutePath devToysRepositoryDirectory,
        GuiWindowsPublishBinariesBuilder guiWindowsPublishBinariesBuilder)
    {
        Log.Information("Creating Microsoft Store package for DevToys {architecutre}...", guiWindowsPublishBinariesBuilder.Architecture.RuntimeIdentifier);

        AbsolutePath sourceMappingFile
            = await CreateAppxManifestAsync(
                VersionHelper.IsPreview,
                devToysRepositoryDirectory,
                guiWindowsPublishBinariesBuilder);

        AbsolutePath msixFile = packDirectory / $"devtoys_win_{guiWindowsPublishBinariesBuilder.Architecture.PlatformTarget}.msix";

        var progress = new Progress<ProgressData>(data =>
        {
            Log.Debug(data.Message);
        });

        var sdk = new MakeAppxWrapper();
        await sdk.Pack(
            MakeAppxPackOptions.CreateFromMapping(
                sourceMappingFile,
                msixFile,
                compress: true,
                validate: true),
            progress,
            CancellationToken.None);

        Log.Information(string.Empty);
    }

    private static AbsolutePath GenerateInnoSetupScript(
        string versionNumber,
        bool isPreview,
        AbsolutePath packDirectory,
        AbsolutePath devToysRepositoryDirectory,
        GuiWindowsPublishBinariesBuilder guiWindowsPublishBinariesBuilder)
    {
        string appName = "DevToys";
        if (isPreview)
            appName = "DevToys Preview";

        AbsolutePath binDirectory = guiWindowsPublishBinariesBuilder.OutputPath!;
        AbsolutePath exeFile = guiWindowsPublishBinariesBuilder.OutputPath / "DevToys.exe";
        AbsolutePath archiveFile = packDirectory / $"devtoys_setup_{guiWindowsPublishBinariesBuilder.Architecture.PlatformTarget}.exe";
        AbsolutePath iconFile = devToysRepositoryDirectory / "assets" / "logo" / "Windows-Linux" / "Stable" / "Icon-Windows.ico";
        if (VersionHelper.IsPreview)
        {
            iconFile = devToysRepositoryDirectory / "assets" / "logo" / "Windows-Linux" / "Preview" / "Icon-Windows-Preview.ico";
        }

        AbsolutePath innoSetupScriptFile = binDirectory.Parent / $"devtoys_setup_{guiWindowsPublishBinariesBuilder.Architecture.PlatformTarget}.iss";

        Architectures architectures;
        if (guiWindowsPublishBinariesBuilder.Architecture == TargetCpuArchitecture.Windows_Arm64)
        {
            architectures = Architectures.Arm64;
        }
        else if (guiWindowsPublishBinariesBuilder.Architecture == TargetCpuArchitecture.Windows_X64)
        {
            architectures = Architectures.X64;
        }
        else if (guiWindowsPublishBinariesBuilder.Architecture == TargetCpuArchitecture.Windows_X86)
        {
            architectures = Architectures.X86;
        }
        else
        {
            throw new NotSupportedException($"Unsupported platform target: {guiWindowsPublishBinariesBuilder.Architecture.PlatformTarget}");
        }

        BuilderUtils.Build(
            builder =>
            {
                builder.Setup
                    // Basic info
                    .Create(appName)
                    .AppPublisher("DevToys")
                    .DefaultGroupName(appName)
                    .AppPublisherURL("https://devtoys.app")
                    .AppSupportURL("https://github.com/DevToys-app/DevToys/issues")
                    .AppVersion(versionNumber)
                    // Paths
                    .OutputDir(packDirectory)
                    .OutputBaseFilename($"devtoys_win_{guiWindowsPublishBinariesBuilder.Architecture.PlatformTarget}")
                    .DefaultDirName(@$"{InnoConstants.Shell.UserProgramFiles}\{appName}") // userpf = C:\Users\{username}\AppData\Local\Programs
                    .PrivilegesRequired(PrivilegesRequired.Lowest)
                    .LicenseFile(binDirectory / "LICENSE.md")
                    // Compression
                    .Compression("lzma")
                    .SolidCompression(YesNo.Yes)
                    // Architecture
                    .ArchitecturesAllowed(architectures)
                    .ArchitecturesInstallIn64BitMode(ArchitecturesInstallIn64BitMode.X64 | ArchitecturesInstallIn64BitMode.Arm64)
                    // UX
                    .SetupIconFile(iconFile)
                    .UninstallDisplayIcon(iconFile)
                    .WizardStyle(WizardStyle.Modern)
                    .ShowLanguageDialog(YesNo.No)
                    .DisableDirPage(YesNo.Yes)
                    .DisableProgramGroupPage(YesNo.Yes);

                // Task to create desktop icon
                builder.Tasks
                    .CreateEntry("desktopicon", "{cm:CreateDesktopIcon}")
                    .GroupDescription("{cm:AdditionalIcons}")
                    .Flags(TaskFlags.Unchecked);

                // Files
                builder.Files
                    .CreateEntry(@$"{binDirectory}\*", InnoConstants.Directories.App)
                    .Flags(FileFlags.IgnoreVersion | FileFlags.RecurseSubdirs);

                builder.Icons
                    // Start menu icon
                    .CreateEntry(@$"{InnoConstants.Shell.UserPrograms}\{appName}", @$"{InnoConstants.Directories.App}\{exeFile.Name}")
                    // Desktop icon
                    .CreateEntry(@$"{InnoConstants.Shell.UserDesktop}\{appName}", @$"{InnoConstants.Directories.App}\{exeFile.Name}")
                    .Tasks("desktopicon");

                // Run app after installation
                builder.Run
                    .CreateEntry($@"{InnoConstants.Directories.App}\{exeFile.Name}")
                    .Flags(RunFlags.NoWait | RunFlags.PostInstall | RunFlags.SkipIfSilent);

            },
            path: innoSetupScriptFile);

        return innoSetupScriptFile;
    }

    private static async Task<AbsolutePath> CreateAppxManifestAsync(
        bool isPreview,
        AbsolutePath devToysRepositoryDirectory,
        GuiWindowsPublishBinariesBuilder guiWindowsPublishBinariesBuilder)
    {
        string displayName = "DevToys";
        string packageName = "DevToys";
        AbsolutePath msixLogoPath = devToysRepositoryDirectory / "assets" / "logo" / "Windows-Linux" / "Stable" / "msix";

        if (isPreview)
        {
            displayName = "DevToys-Preview";
            packageName = "64360VelerSoftware.DevToys-Preview";
            msixLogoPath = devToysRepositoryDirectory / "assets" / "logo" / "Windows-Linux" / "Preview" / "msix";
        }

        var fileListBuilder = new PackageFileListBuilder();
        fileListBuilder.AddDirectory(guiWindowsPublishBinariesBuilder.OutputPath, recursive: true, targetRelativeDirectory: string.Empty);

        AppxPackageArchitecture architecture;
        if (guiWindowsPublishBinariesBuilder.Architecture == TargetCpuArchitecture.Windows_Arm64)
        {
            architecture = AppxPackageArchitecture.Arm64;
        }
        else if (guiWindowsPublishBinariesBuilder.Architecture == TargetCpuArchitecture.Windows_X64)
        {
            architecture = AppxPackageArchitecture.x64;
        }
        else if (guiWindowsPublishBinariesBuilder.Architecture == TargetCpuArchitecture.Windows_X86)
        {
            architecture = AppxPackageArchitecture.x86;
        }
        else
        {
            throw new NotSupportedException($"Unsupported platform target: {guiWindowsPublishBinariesBuilder.Architecture.PlatformTarget}");
        }

        var options = new AppxManifestCreatorOptions
        {
            CreateLogo = false,
            EntryPoints = ["DevToys.exe"],
            PackageArchitecture = architecture,
            PackageName = packageName,
            PackageDisplayName = displayName,
            PackageDescription = "A Swiss Army knife for developers.",
            PublisherName = "CN=3D87C8C2-C6F4-4875-B258-5FEC13B08F81",
            PublisherDisplayName = "etiennebaudoux",
            Version = new Version(VersionHelper.Major, VersionHelper.Minor, VersionHelper.BuildOrPreview, 0),
            StoreLogoPath = [
                msixLogoPath / "StoreLogo.scale-100.png",
                msixLogoPath / "StoreLogo.scale-125.png",
                msixLogoPath / "StoreLogo.scale-150.png",
                msixLogoPath / "StoreLogo.scale-200.png",
                msixLogoPath / "StoreLogo.scale-400.png",
            ],
            Square150x150LogoPath = [
                msixLogoPath / "Square150x150Logo.scale-100.png",
                msixLogoPath / "Square150x150Logo.scale-125.png",
                msixLogoPath / "Square150x150Logo.scale-150.png",
                msixLogoPath / "Square150x150Logo.scale-200.png",
                msixLogoPath / "Square150x150Logo.scale-400.png",
            ],
            Square44x44LogoPath = [
                msixLogoPath / "Square44x44Logo.scale-100.png",
                msixLogoPath / "Square44x44Logo.scale-125.png",
                msixLogoPath / "Square44x44Logo.scale-150.png",
                msixLogoPath / "Square44x44Logo.scale-200.png",
                msixLogoPath / "Square44x44Logo.scale-400.png",
                msixLogoPath / "Square44x44Logo.targetsize-16.png",
                msixLogoPath / "Square44x44Logo.targetsize-24.png",
                msixLogoPath / "Square44x44Logo.targetsize-24_altform-unplated.png",
                msixLogoPath / "Square44x44Logo.targetsize-32.png",
                msixLogoPath / "Square44x44Logo.targetsize-48.png",
                msixLogoPath / "Square44x44Logo.targetsize-256.png",
                msixLogoPath / "Square44x44Logo.altform-lightunplated_targetsize-16.png",
                msixLogoPath / "Square44x44Logo.altform-lightunplated_targetsize-24.png",
                msixLogoPath / "Square44x44Logo.altform-lightunplated_targetsize-32.png",
                msixLogoPath / "Square44x44Logo.altform-lightunplated_targetsize-48.png",
                msixLogoPath / "Square44x44Logo.altform-lightunplated_targetsize-256.png",
                msixLogoPath / "Square44x44Logo.altform-unplated_targetsize-16.png",
                msixLogoPath / "Square44x44Logo.altform-unplated_targetsize-32.png",
                msixLogoPath / "Square44x44Logo.altform-unplated_targetsize-48.png",
                msixLogoPath / "Square44x44Logo.altform-unplated_targetsize-256.png",
            ],
            Wide310x150Logo = [
                msixLogoPath / "Wide310x150Logo.scale-100.png",
                msixLogoPath / "Wide310x150Logo.scale-125.png",
                msixLogoPath / "Wide310x150Logo.scale-150.png",
                msixLogoPath / "Wide310x150Logo.scale-200.png",
                msixLogoPath / "Wide310x150Logo.scale-400.png",
            ],
            Square71x71Logo = [
                msixLogoPath / "SmallTile.scale-100.png",
                msixLogoPath / "SmallTile.scale-125.png",
                msixLogoPath / "SmallTile.scale-150.png",
                msixLogoPath / "SmallTile.scale-200.png",
                msixLogoPath / "SmallTile.scale-400.png",
            ],
            Square310x310Logo = [
                msixLogoPath / "LargeTile.scale-100.png",
                msixLogoPath / "LargeTile.scale-125.png",
                msixLogoPath / "LargeTile.scale-150.png",
                msixLogoPath / "LargeTile.scale-200.png",
                msixLogoPath / "LargeTile.scale-400.png",
            ]
        };

        var temporaryFiles = new List<string>();
        var manifestCreator = new AppxManifestCreator();
        await foreach (CreatedItem result in manifestCreator.CreateManifestForDirectory(new DirectoryInfo(guiWindowsPublishBinariesBuilder.OutputPath), options, CancellationToken.None))
        {
            temporaryFiles.Add(result.SourcePath);

            if (result.PackageRelativePath == null)
                continue;

            // Instead of adding the files to the list of files to include in the MSIX, we copy them to the folder.
            // This is needed otherwise the generation of `resources.pri` won't include the assets.
            var directory = Path.GetDirectoryName(Path.Combine(guiWindowsPublishBinariesBuilder.OutputPath, result.PackageRelativePath));
            Directory.CreateDirectory(directory);
            File.Copy(result.SourcePath, Path.Combine(guiWindowsPublishBinariesBuilder.OutputPath, result.PackageRelativePath), overwrite: true);
        }

        // Add remaining files.
        string tempFileList = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".list");
        temporaryFiles.Add(tempFileList);

        string tempManifestPath = Path.Combine(Path.GetTempPath(), "AppxManifest-" + Guid.NewGuid().ToString("N") + ".xml");
        temporaryFiles.Add(tempManifestPath);

        // Generate resources.pri
        var makePri = new MakePriWrapper();
        await makePri.Pack(guiWindowsPublishBinariesBuilder.OutputPath, null, CancellationToken.None);
        fileListBuilder.AddFile(makePri.ResourcePriPath, "resources.pri");

        string srcManifest = fileListBuilder.GetManifestSourcePath();
        if (srcManifest == null || !File.Exists(srcManifest))
            throw new InvalidOperationException("The selected folder cannot be packed because it has no manifest, and MSIX Hero was unable to create one. A manifest can be only created if the selected folder contains any executable file.");

        // Copy manifest to a temporary file
        var injector = new MsixHeroBrandingInjector();
        await using (FileStream manifestStream = File.OpenRead(fileListBuilder.GetManifestSourcePath()))
        {
            XDocument xml = await XDocument.LoadAsync(manifestStream, LoadOptions.None, CancellationToken.None);
            await injector.Inject(xml);
            await File.WriteAllTextAsync(tempManifestPath, xml.ToString(SaveOptions.None), CancellationToken.None);
            fileListBuilder.AddManifest(tempManifestPath);
        }

        await File.WriteAllTextAsync(tempFileList, fileListBuilder.ToString(), CancellationToken.None);

        return tempFileList;
    }
}
