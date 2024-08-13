using Serilog;

using System.Text;

using WindowsTooling.Exceptions;
using WindowsTooling.Helpers;
using WindowsTooling.Progress;

namespace WindowsTooling.Sdk;

public class MakePriWrapper : ExeWrapper
{
    public string? ResourcePriPath { get; private set; }

    public async Task Pack(string packageDirectory, IProgress<ProgressData>? progress = null, CancellationToken cancellationToken = default)
    {
        string tempProConfigXmlPath = Path.Combine(Path.GetTempPath(), "priconfig-" + Guid.NewGuid().ToString("N") + ".xml");
        ResourcePriPath = Path.Combine(Path.GetTempPath(), "resources-" + Guid.NewGuid().ToString("N") + ".pri");

        StringBuilder arguments = new("createconfig", 256);

        arguments.Append(" /cf "); // Configuration file output location
        arguments.Append(CommandLineHelper.EncodeParameterArgument(tempProConfigXmlPath));

        arguments.Append(" /dq "); // The default qualifiers to set in the configuration file. A language qualifier is required
        arguments.Append(" en-US ");

        arguments.Append(" /o "); // Overwrite an existing output file of the same name without prompting
        arguments.Append(" /pv "); // Platform version to use for generated configuration file
        arguments.Append(" 10.0.0");

        await RunMakePro(arguments.ToString(), cancellationToken);

        // See https://stackoverflow.com/questions/38506783/why-is-makepri-exe-creating-more-than-one-resources-pri-file
        // Let's remove
        // <packaging>
        //     <autoResourcePackage qualifier="Language"/>
        //     <autoResourcePackage qualifier="Scale"/>
        //     <autoResourcePackage qualifier="DXFeatureLevel"/>
        // </packaging>

        string xml = File.ReadAllText(tempProConfigXmlPath);
        xml = xml
            .Replace("<packaging>", string.Empty)
            .Replace("<autoResourcePackage qualifier=\"Language\"/>", string.Empty)
            .Replace("<autoResourcePackage qualifier=\"Scale\"/>", string.Empty)
            .Replace("<autoResourcePackage qualifier=\"DXFeatureLevel\"/>", string.Empty)
            .Replace("</packaging>", string.Empty);
        File.WriteAllText(tempProConfigXmlPath, xml);

        arguments = new("new", 256);

        arguments.Append(" /pr ");
        arguments.Append(CommandLineHelper.EncodeParameterArgument(packageDirectory));

        arguments.Append(" /cf ");
        arguments.Append(CommandLineHelper.EncodeParameterArgument(tempProConfigXmlPath));

        arguments.Append(" /of ");
        arguments.Append(CommandLineHelper.EncodeParameterArgument(ResourcePriPath));

        arguments.Append(" /o ");
        arguments.Append(" /rm ");
        arguments.Append(" /v ");

        await RunMakePro(arguments.ToString(), cancellationToken);
    }

    private async Task RunMakePro(string arguments, CancellationToken cancellationToken)
    {
        string makepri = SdkPathHelper.GetSdkPath("makepri.exe", BundleHelper.SdkPath);
        Log.Information("Executing {MakePri} {Arguments}", makepri, arguments);
        await RunAsync(makepri, arguments.ToString(), 0, callBack: null, cancellationToken);
    }
}
