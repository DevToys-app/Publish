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
using System.Diagnostics;
using WindowsTooling.Exceptions;

namespace WindowsTooling.Sdk;

public abstract class ExeWrapper
{
    protected static Task RunAsync(
        string path,
        string arguments,
        IList<int> properExitCodes,
        Action<string>? callBack,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(path, arguments, null, properExitCodes, callBack, cancellationToken);
    }

    protected static Task RunAsync(
        string path,
        string arguments,
        Action<string>? callBack,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(path, arguments, null, (_, _, _) => null, callBack, cancellationToken);
    }

    protected static Task RunAsync(
        string path,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(path, arguments, null, (_, _, _) => null, default, cancellationToken);
    }

    protected static Task RunAsync(
        string path,
        string arguments,
        int properExitCode,
        Action<string>? callBack,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(path, arguments, null, new[] { properExitCode }, callBack, cancellationToken);
    }

    protected static Task RunAsync(
        string path,
        string arguments,
        int properExitCode,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(path, arguments, null, new[] { properExitCode }, default, cancellationToken);
    }

    protected static Task RunAsync(
        string path,
        string arguments,
        string workingDirectory,
        int properExitCode,
        Action<string>? callBack,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(path, arguments, workingDirectory, new[] { properExitCode }, callBack, cancellationToken);
    }

    protected static Task RunAsync(
        string path,
        string arguments,
        string? workingDirectory,
        IList<int> properExitCodes,
        Action<string>? callBack,
        CancellationToken cancellationToken = default)
    {
        GetErrorMessageFromProcess errorChecker = delegate (int code, IList<string> _, IList<string> _)
        {
            if (properExitCodes != null && properExitCodes.Any() && !properExitCodes.Contains(code))
            {
                return string.Format("Process exited with an improper exit code {0}.", code);
            }

            return null;
        };

        return RunAsync(path, arguments, workingDirectory, errorChecker, callBack, cancellationToken);
    }

    protected static async Task RunAsync(string path,
        string arguments,
        string? workingDirectory,
        GetErrorMessageFromProcess errorDelegate,
        Action<string>? callBack = default,
        CancellationToken cancellationToken = default)
    {
        Log.Debug(string.Format("Executing {0} {1}", path, arguments));
        ProcessStartInfo processStartInfo = new(path, arguments);

        List<string> standardOutput = [];
        List<string> standardError = [];

        // force some settings in the start info so we can capture the output
        processStartInfo.UseShellExecute = false;
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;
        processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        processStartInfo.CreateNoWindow = true;

        if (workingDirectory != null)
        {
            processStartInfo.WorkingDirectory = workingDirectory;
        }

        TaskCompletionSource<int> tcs = new();

        Process process = new()
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true
        };

        TaskCompletionSource<string[]> standardOutputResults = new();
        process.OutputDataReceived += (_, args) =>
        {
            callBack?.Invoke(args.Data ?? string.Empty);
            if (args.Data != null)
            {
                standardOutput.Add(args.Data);
            }
            else
            {
                standardOutputResults.SetResult(standardOutput.ToArray());
            }
        };

        TaskCompletionSource<string[]> standardErrorResults = new();
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                standardError.Add(args.Data);
            }
            else
            {
                standardErrorResults.SetResult(standardError.ToArray());
            }
        };

        process.Exited += async (_, _) =>
        {
            await standardOutputResults.Task;
            await standardErrorResults.Task;

            Log.Verbose("Standard error: " + string.Join(Environment.NewLine, standardError));
            Log.Verbose("Standard output: " + string.Join(Environment.NewLine, standardOutput));
            tcs.TrySetResult(process.ExitCode);
        };

        await using (cancellationToken.Register(
            () =>
            {
                tcs.TrySetCanceled();
                try
                {
                    if (!process.HasExited)
                    {
                        Log.Information("Killing the process PID={0}…", process.Id);
                        process.Kill();
                    }
                }
                catch (InvalidOperationException) { }
            }))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!process.Start())
            {
                tcs.TrySetException(new InvalidOperationException("Failed to start process."));
            }
            else
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            int result = await tcs.Task;

            if (standardOutput.Any())
            {
                Log.Debug("Process has finished and returned the following standard output:" + "\r\n" + string.Join(Environment.NewLine, standardOutput));
            }
            else
            {
                Log.Debug("Process has finished and did not return anything to standard output.");
            }

            if (standardError.Any())
            {
                Log.Debug("Process has finished and returned the following standard error:" + "\r\n" + string.Join(Environment.NewLine, standardError));
            }
            else
            {
                Log.Debug("Process has finished and did not return anything to standard error.");
            }

            string? error = errorDelegate(result, standardError, standardOutput);
            if (error == null)
            {
                return;
            }

            throw new ProcessWrapperException(error, result, standardError, standardOutput);
        }
    }

    protected static Task RunAsync(string path,
        string arguments,
        GetErrorMessageFromProcess errorDelegate,
        Action<string> callBack,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(path, arguments, null, errorDelegate, callBack, cancellationToken);
    }

    protected delegate string? GetErrorMessageFromProcess(
        int exitCode,
        IList<string> standardOutput,
        IList<string> standardError);
}