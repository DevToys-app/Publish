namespace Core;

internal sealed class TargetCpuArchitecture
{
    private const string X86 = "x86";
    private const string X64 = "x64";
    private const string Arm = "arm";
    private const string Arm64 = "arm64";
    private const string AnyCPU = "AnyCPU";

    internal static TargetCpuArchitecture Linux_Arm = new() { PlatformTarget = Arm, RuntimeIdentifier = "linux-arm" };
    internal static TargetCpuArchitecture Linux_X64 = new() { PlatformTarget = X64, RuntimeIdentifier = "linux-x64" };
    internal static TargetCpuArchitecture MacOs_Arm64 = new() { PlatformTarget = Arm64, RuntimeIdentifier = "osx-arm64" };
    internal static TargetCpuArchitecture MacOs_X64 = new() { PlatformTarget = X64, RuntimeIdentifier = "osx-x64" };
    internal static TargetCpuArchitecture Windows_Arm64 = new() { PlatformTarget = Arm64, RuntimeIdentifier = "win-arm64" };
    internal static TargetCpuArchitecture Windows_X64 = new() { PlatformTarget = X64, RuntimeIdentifier = "win-x64" };
    internal static TargetCpuArchitecture Windows_X86 = new() { PlatformTarget = X86, RuntimeIdentifier = "win-x86" };
    internal static TargetCpuArchitecture Neutral = new() { PlatformTarget = AnyCPU, RuntimeIdentifier = string.Empty };

    internal string RuntimeIdentifier { get; private set; } = string.Empty;

    internal string RuntimeIdentifierForFileName => RuntimeIdentifier.Replace("-", "_");

    internal string PlatformTarget { get; private set; } = string.Empty;
}
