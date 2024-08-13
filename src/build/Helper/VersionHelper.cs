namespace Helper;

internal static class VersionHelper
{
    internal static int Major { get; set; }

    internal static int Minor { get; set; }

    internal static int BuildOrPreview { get; set; }

    internal static bool IsPreview { get; set; }

    internal static string GetVersionString(bool allowPreviewSyntax, bool allowDisplayedSyntax = false)
    {
        if (allowPreviewSyntax)
        {
            if (allowDisplayedSyntax)
            {
                // For version displayed in-app.
                return $"{Major}.{Minor}-preview.{BuildOrPreview}";
            }

            // generally for NuGet packages
            return $"{Major}.{Minor}.{BuildOrPreview}-preview";
        }

        // Regular writing, for Apple bundle and MSIX.
        return $"{Major}.{Minor}.{BuildOrPreview}";
    }
}
