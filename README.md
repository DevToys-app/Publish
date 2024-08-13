# Prepare the release

1. Update the changelogs and commit them. [DevToys/src/app/dev/DevToys.Blazor/Assets/changelog.md](https://github.com/DevToys-app/DevToys/blob/main/src/app/dev/DevToys.Blazor/Assets/changelog.md)
1. Build and commit CSS/JS in DevToys.Blazor.
1. Prepare a blog article (optional).

# How to create artifacts

[![publish](https://github.com/DevToys-app/Publish/actions/workflows/ci.yml/badge.svg)](https://github.com/DevToys-app/Publish/actions/workflows/ci.yml)

Set of pipeline and logic to apply when publishing

## Linux

1. Build DevToys and DevToys CLI by running `bash ./build.sh  --MajorVersion 2 --MinorVersion 0 --BuildOrPreviewNumber 0 --IsPreviewVersion true`.

### Install, run and uninstall DevToys

```bash
# install
sudo apt install ./artifact/{cpu}/devtoys_{version}.deb
# or
sudo dpkg -i ./artifact/{cpu}/devtoys_{version}.deb
# verify it's install:
ls -l /opt/devtoys/devtoys
ls -l /usr/bin/{devtoys,DevToys}*

# run
devtoys

# uninstall
sudo apt remove devtoys
# if you want to remove dependencies
sudo apt autoremove 
```

### Install, run and uninstall DevToys CLI

```bash
# install
sudo apt install ./artifact/{cpu}/devtoys.cli_{version}.deb
# or
sudo dpkg -i ./artifact/{cpu}/devtoys.cli_{version}.deb
# check installed folders and binaries
ls -l /opt/devtoys/devtoys.cli
ls -l /usr/bin/{devtoys.cli,DevToys.cli}*

# run
devtoys.cli --help

# uninstall
sudo apt remove devtoys.cli
```

## Windows

1. Build DevToys and DevToys CLI by running `./build.ps1 --MajorVersion 2 --MinorVersion 0 --BuildOrPreviewNumber 0 --IsPreviewVersion true` in a PowerShell command prompt.

## macOS

1. Build DevToys and DevToys CLI by running `sh ./build.cmd --MajorVersion 2 --MinorVersion 0 --BuildOrPreviewNumber 0 --IsPreviewVersion true --AppleId appleid --AppleAppPassword the-password --AppleTeamId the-id`.

### Installing

A good way to validate that the app got notarize:
1. Create a draft release on GitHub
1. Upload the ZIP archive of the Mac app to it.
1. Download the ZIP from the GitHub release
1. Unzip and run the App: Mac should allow us to run it.

# How to publish

## Homebrew (macOS)

1. Update this file:
https://github.com/Homebrew/homebrew-cask/blob/master/Casks/d/devtoys.rb

## Chocolatey

// TODO

## WinGet
1. Update these files:
https://github.com/microsoft/winget-pkgs/pull/156567/files

## MS Store
1. Publish the MSIX to Microsoft Partner Center

## Flatpak
// TODO

## NuGet

// TODO