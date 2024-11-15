﻿// MSIX Hero
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

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace WindowsTooling.AppxManifest.FileReader;

public class FileInfoFileReaderAdapter : IAppxDiskFileReader
{
    private readonly FileInfo _appxManifestFile;

    public FileInfoFileReaderAdapter(FileInfo appxManifestFile)
    {
        if (!appxManifestFile.Exists)
        {
            throw new ArgumentException(string.Format("File {0} does not exist.", appxManifestFile.FullName), nameof(appxManifestFile));
        }

        RootDirectory = appxManifestFile.DirectoryName!;
        _appxManifestFile = appxManifestFile;
        FilePath = appxManifestFile.FullName;
    }

    public FileInfoFileReaderAdapter(string appxManifestFile)
    {
        if (string.IsNullOrEmpty(appxManifestFile))
        {
            throw new ArgumentNullException(nameof(appxManifestFile));
        }

        FilePath = appxManifestFile;
        FileInfo fileInfo = new(appxManifestFile);
        RootDirectory = fileInfo.DirectoryName!;
        _appxManifestFile = fileInfo;
    }

    public string RootDirectory { get; }

    public string FilePath { get; }

    public Stream GetFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        // ReSharper disable once PossibleNullReferenceException
        return File.OpenRead(Path.Combine(_appxManifestFile.Directory!.FullName, filePath));
    }

#pragma warning disable 1998
    public async IAsyncEnumerable<string> EnumerateDirectories(string? rootRelativePath = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore 1998
    {
        // ReSharper disable once PossibleNullReferenceException
        string baseDir = _appxManifestFile.Directory!.FullName;
        string fullDir = rootRelativePath == null ? baseDir : Path.Combine(baseDir, rootRelativePath);

        foreach (string d in Directory.EnumerateDirectories(fullDir, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return d;
        }
    }

#pragma warning disable 1998
    public async IAsyncEnumerable<AppxFileInfo> EnumerateFiles(string? rootRelativePath, string wildcard, SearchOption searchOption = SearchOption.TopDirectoryOnly, [EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore 1998
    {
        // ReSharper disable once PossibleNullReferenceException
        string baseDir = _appxManifestFile.Directory!.FullName;
        string fullDir = rootRelativePath == null ? baseDir : Path.Combine(baseDir, rootRelativePath);

        foreach (string f in Directory.EnumerateFiles(fullDir, wildcard, searchOption))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new AppxFileInfo(new FileInfo(f));
        }
    }

    public IAsyncEnumerable<AppxFileInfo> EnumerateFiles(string? rootRelativePath = null, CancellationToken cancellationToken = default)
    {
        return EnumerateFiles(rootRelativePath, "*", SearchOption.TopDirectoryOnly, cancellationToken);
    }

    public Stream? GetResource(string resourceFilePath)
    {
        if (string.IsNullOrEmpty(resourceFilePath))
        {
            return null;
        }

        if (FileExists(resourceFilePath))
        {
            return GetFile(resourceFilePath);
        }

        string fileName = Path.GetFileName(resourceFilePath);
        string extension = Path.GetExtension(resourceFilePath);
        string? resourceDir = Path.GetDirectoryName(resourceFilePath);

        Queue<string> dirsToTry = new();
        // ReSharper disable once AssignNullToNotNullAttribute
        dirsToTry.Enqueue(string.IsNullOrEmpty(resourceDir) ? _appxManifestFile.DirectoryName! : Path.Combine(_appxManifestFile.DirectoryName!, resourceDir));

        while (dirsToTry.Any())
        {
            string dequeued = dirsToTry.Dequeue();
            DirectoryInfo dirInfo = new(dequeued);
            if (!dirInfo.Exists)
            {
                continue;
            }

            IEnumerable<FileInfo> matchingFiles = dirInfo.EnumerateFiles(Path.GetFileNameWithoutExtension(fileName) + "*" + extension);
            foreach (FileInfo matchingFile in matchingFiles)
            {
                string name = Regex.Replace(matchingFile.Name, @"\.[^\.\-]+-[^\.\-]+", string.Empty);
                if (string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    return GetFile(Path.GetRelativePath(_appxManifestFile.DirectoryName!, matchingFile.FullName));
                }
            }

            IEnumerable<DirectoryInfo> matchingDirectories = dirInfo.EnumerateDirectories().Where(d => Regex.IsMatch(d.Name, @".[^\.\-]+-[^\.\-]+"));
            foreach (DirectoryInfo? matchingDirectory in matchingDirectories)
            {
                dirsToTry.Enqueue(matchingDirectory.FullName);
            }
        }

        return null;
    }

    public bool FileExists(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        if (_appxManifestFile?.Directory?.FullName == null)
        {
            return false;
        }

        // ReSharper disable once PossibleNullReferenceException
        return File.Exists(Path.Combine(_appxManifestFile.Directory.FullName, filePath));
    }

    public bool DirectoryExists(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath))
        {
            return true;
        }

        if (_appxManifestFile?.Directory?.FullName == null)
        {
            return false;
        }

        // ReSharper disable once PossibleNullReferenceException
        return Directory.Exists(Path.Combine(_appxManifestFile.Directory.FullName, directoryPath));
    }

    void IDisposable.Dispose()
    {
    }
}