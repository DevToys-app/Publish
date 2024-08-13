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

using Serilog;
using System.Text;
using System.Text.RegularExpressions;
using WindowsTooling.Exceptions;
using WindowsTooling.Helpers;
using WindowsTooling.Progress;

namespace WindowsTooling.Sdk;

public class MakeAppxWrapper : ExeWrapper
{
    public Task Pack(MakeAppxPackOptions options, IProgress<ProgressData>? progress = null, CancellationToken cancellationToken = default)
    {
        StringBuilder arguments = new("pack", 256);

        if (options.Source is FileInfo fileInfo)
        {
            arguments.Append(" /f ");
            arguments.Append(CommandLineHelper.EncodeParameterArgument(fileInfo.FullName));
        }
        else
        {
            arguments.Append(" /d ");
            arguments.Append(CommandLineHelper.EncodeParameterArgument(options.Source.FullName));
        }

        arguments.Append(" /p ");
        arguments.Append(CommandLineHelper.EncodeParameterArgument(options.Target.FullName));

        if (options.Verbose)
        {
            arguments.Append(" /v");
        }

        if (options.Overwrite)
        {
            arguments.Append(" /o");
        }

        if (!options.Compress)
        {
            arguments.Append(" /nc");
        }

        if (!options.Validate)
        {
            arguments.Append(" /nv");
        }

        if (options.PublisherBridge != null)
        {
            arguments.Append(" /pb ");
            arguments.Append(CommandLineHelper.EncodeParameterArgument(options.PublisherBridge));
        }

        PackUnPackProgressWrapper wrapper = new(progress);
        return RunMakeAppx(arguments.ToString(), wrapper.Callback, cancellationToken);
    }

    private async Task RunMakeAppx(string arguments, Action<string> callBack, CancellationToken cancellationToken = default)
    {
        string makeAppx = SdkPathHelper.GetSdkPath("makeappx.exe", BundleHelper.SdkPath);
        Log.Information("Executing {MakeAppx} {Arguments}", makeAppx, arguments);

        try
        {
            await RunAsync(makeAppx, arguments, 0, callBack, cancellationToken);
        }
        catch (ProcessWrapperException e)
        {
            Exception exception = GetExceptionFromMakeAppxOutput(e.StandardError, e.ExitCode) ??
                            GetExceptionFromMakeAppxOutput(e.StandardOutput, e.ExitCode);

            if (exception != null)
            {
                throw exception;
            }

            throw;
        }
    }

    private static Exception GetExceptionFromMakeAppxOutput(IList<string> outputLines, int exitCode)
    {
        string? findSimilar = outputLines.FirstOrDefault(item => item.StartsWith("MakeAppx : error: Error info: error ", StringComparison.OrdinalIgnoreCase));
        if (findSimilar != null)
        {
            findSimilar = findSimilar.Substring("MakeAppx : error: Error info: error ".Length);

            Match error = Regex.Match(findSimilar, "([0-9a-zA-Z]+): ");
            if (error.Success)
            {
                findSimilar = findSimilar.Substring(error.Length).Trim();
                return new SdkException(string.Format("MakeAppx.exe returned exit code {0} due to error {1}.", exitCode, error.Groups[1].Value) + " " + findSimilar, exitCode);
            }

            return new SdkException(string.Format("MakeAppx.exe returned exit code {0}.", exitCode) + " " + findSimilar, exitCode);
        }

        findSimilar = outputLines.FirstOrDefault(item => item.StartsWith("MakeAppx : error: 0x", StringComparison.OrdinalIgnoreCase));
        if (findSimilar != null)
        {
            string? manifestError = outputLines.FirstOrDefault(item => item.StartsWith("MakeAppx : error: Manifest validation error: "));
            manifestError = manifestError?.Substring("MakeAppx : error: Manifest validation error: ".Length);

            findSimilar = findSimilar.Substring("MakeAppx : error: ".Length);

            Match error = Regex.Match(findSimilar, "([0-9a-zA-Z]+) \\- ");
            if (error.Success)
            {
                if (!string.IsNullOrEmpty(manifestError))
                {
                    findSimilar = manifestError;
                }
                else
                {
                    findSimilar = findSimilar.Substring(error.Length).Trim();
                }

                int parsedExitCode;
                if (int.TryParse(error.Groups[1].Value, out parsedExitCode) && parsedExitCode > 0)
                {
                    return new SdkException(string.Format("MakeAppx.exe returned exit code {0} due to error {1}.", parsedExitCode, error.Groups[1].Value) + " " + findSimilar, exitCode);
                }

                if (error.Groups[1].Value.StartsWith("0x", StringComparison.Ordinal))
                {
                    parsedExitCode = Convert.ToInt32(error.Groups[1].Value, 16);
                    if (parsedExitCode != 0)
                    {
                        return new SdkException(string.Format("MakeAppx.exe returned exit code {0} due to error {1}.", parsedExitCode, error.Groups[1].Value) + " " + findSimilar, exitCode);
                    }
                }

                return new SdkException(string.Format("MakeAppx.exe returned exit code {0} due to error {1}.", exitCode, error.Groups[1].Value) + " " + findSimilar, exitCode);
            }

            if (!string.IsNullOrEmpty(manifestError))
            {
                findSimilar = manifestError;
            }

            if (int.TryParse(error.Groups[1].Value, out exitCode) && exitCode > 0)
            {
                return new SdkException(string.Format("MakeAppx.exe returned exit code {0}.", exitCode) + " " + findSimilar, exitCode);
            }

            if (error.Groups[1].Value.StartsWith("0x", StringComparison.Ordinal))
            {
                exitCode = Convert.ToInt32(error.Groups[1].Value, 16);
                if (exitCode != 0)
                {
                    return new SdkException(string.Format("MakeAppx.exe returned exit code {0}.", exitCode) + " " + findSimilar, exitCode);
                }
            }

            return new SdkException(string.Format("MakeAppx.exe returned exit code {0}.", exitCode) + " " + findSimilar, exitCode);
        }

        return null;
    }

    private class PackUnPackProgressWrapper
    {
        private readonly IProgress<ProgressData> _progressReporter;

        private int? _fileCounter;

        private int _alreadyProcessed;

        public PackUnPackProgressWrapper(IProgress<ProgressData> progressReporter)
        {
            _progressReporter = progressReporter;
        }

        public void SetFilesCount(int expectedFilesCount)
        {
            _fileCounter = expectedFilesCount;
        }

        public Action<string> Callback => OnProgress;

        private void OnProgress(string data)
        {
            if (string.IsNullOrEmpty(data) || _progressReporter == null)
            {
                return;
            }

            if (!_fileCounter.HasValue)
            {
                Match match = Regex.Match(data, @"^Packing (\d+) files?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                if (match.Success)
                {
                    _fileCounter = int.Parse(match.Groups[1].Value);
                }
            }

            Match regexFile = Regex.Match(data, "^Processing \"([^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            if (regexFile.Success)
            {
                _alreadyProcessed++;
                int currentProgress;
                if (_fileCounter.HasValue && _fileCounter.Value > 0)
                {
                    currentProgress = (int)(100.0 * _alreadyProcessed / _fileCounter.Value);
                }
                else
                {
                    currentProgress = 0;
                }

                string fileName = regexFile.Groups[1].Value;
                if (string.IsNullOrEmpty(fileName))
                {
                    return;
                }

                fileName = Path.GetFileName(fileName);
                _progressReporter.Report(new ProgressData(currentProgress, string.Format("Compressing {0}…", fileName)));
            }
            else
            {
                regexFile = Regex.Match(data, "^Extracting file ([^ ]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                if (regexFile.Success)
                {
                    _alreadyProcessed++;
                    int currentProgress;
                    if (_fileCounter.HasValue && _fileCounter.Value > 0)
                    {
                        currentProgress = (int)(100.0 * _alreadyProcessed / _fileCounter.Value);
                    }
                    else
                    {
                        currentProgress = 0;
                    }

                    string fileName = regexFile.Groups[1].Value;
                    if (string.IsNullOrEmpty(fileName))
                    {
                        return;
                    }

                    fileName = Path.GetFileName(fileName);
                    _progressReporter.Report(new ProgressData(currentProgress, string.Format("Extracting file {0}…", fileName)));
                }
            }
        }
    }
}
