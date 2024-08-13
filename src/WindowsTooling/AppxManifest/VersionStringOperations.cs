// MSIX Hero
// Copyright (C) 2022 Marcin Otorowski
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// Full notice:
// https://github.com/marcinotorowski/msix-hero/blob/develop/LICENSE.md


// MSIX Hero
// Copyright (C) 2022 Marcin Otorowski
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// Full notice:
// https://github.com/marcinotorowski/msix-hero/blob/develop/LICENSE.md

namespace WindowsTooling.AppxManifest;

public static class VersionStringOperations
{
    public static string ResolveMaskedVersion(string newValueWithMask, string currentValue)
    {
        if (string.IsNullOrEmpty(currentValue))
        {
            return ResolveMaskedVersion(newValueWithMask);
        }

        if (Version.TryParse(currentValue, out Version? parsed))
        {
            return ResolveMaskedVersion(newValueWithMask, parsed);
        }

        throw new ArgumentException(string.Format("Changing attribute 'ResourceId' from '{0}' to '{1}'…", currentValue));
    }

    public static string ResolveMaskedVersion(string newValueWithMask, Version? currentValue = null)
    {
        if (newValueWithMask == null)
        {
            return currentValue?.ToString() ?? "1.0.0.0";
        }

        if (Version.TryParse(newValueWithMask, out Version? parsedValue))
        {
            // this means the user passed something which looks like a version
            // but let's not trust him too much and make sure it's a four-unit string:
            return $"{Math.Max(parsedValue.Major, 0)}.{Math.Max(parsedValue.Minor, 0)}.{Math.Max(parsedValue.Build, 0)}.{Math.Max(parsedValue.Revision, 0)}";
        }

        // otherwise if the user set the value to 'auto' we change his input to:
        // *.*.*.+
        if (string.Equals(newValueWithMask, "auto", StringComparison.OrdinalIgnoreCase))
        {
            if (currentValue == null)
            {
                return "1.0.0.0";
            }

            newValueWithMask = "*.*.*.+";
        }

        // now we apply some special logic, where:
        // *, x or empty value means take the current unit
        // + or ^ means increase the version by on1

        string[] split = newValueWithMask.Split('.');
        if (split.Length > 4)
        {
            throw new ArgumentException(string.Format("Changing attribute 'ResourceId' from '{0}' to '{1}'…", newValueWithMask));
        }

        if (currentValue == null)
        {
            currentValue = new Version(1, 0, 0, 0);
        }

        string versionString = string.Empty;
        for (int i = 0; i < 4; i++)
        {
            if (i > 0)
            {
                versionString += ".";
            }

            var currentUnit = i switch
            {
                0 => Math.Max(0, currentValue.Major),
                1 => Math.Max(0, currentValue.Minor),
                2 => Math.Max(0, currentValue.Build),
                3 => Math.Max(0, currentValue.Revision),
                _ => throw new InvalidOperationException(),
            };
            if (i < split.Length)
            {
                switch (split[i])
                {
                    case "+":
                    case "^":
                        versionString += (currentUnit + 1).ToString();
                        break;
                    case "x":
                    case "*":
                    case "":
                        versionString += currentUnit.ToString();
                        break;
                    default:
                        if (!int.TryParse(split[i], out _))
                        {
                            throw new ArgumentException(string.Format("The value '{0}' is not a valid version string.", newValueWithMask));
                        }

                        versionString += split[i];
                        break;
                }
            }
            else
            {
                versionString += "0";
            }
        }

        if (!Version.TryParse(versionString, out Version? _))
        {
            throw new ArgumentException(string.Format("The value '{0}' is not a valid version string.", versionString));
        }

        return versionString;
    }
}