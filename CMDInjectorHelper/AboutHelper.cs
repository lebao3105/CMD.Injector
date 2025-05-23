﻿using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

namespace CMDInjectorHelper
{
    public static class AboutHelper
    {
        private static async Task<Release> GetLatestVersion() => await Task.Run(() =>
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                GitHubClient client = new GitHubClient(new ProductHeaderValue("MyClient"));
                return client.Repository.Release.GetLatest("lebao3105", "CMD.Injector").Result;
            }
            return null;
        });

        public async static Task<Release> IsNewUpdateAvailable()
        {
            var latestRelease = await GetLatestVersion();
            var latestReleaseVersion = latestRelease.TagName;
            // Get current build
            PackageVersion version = Package.Current.Id.Version;

            string current = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);

            // Cleanup the target version: it follows v{Major}.{Minor}.{Build}.{Revision} format.
            latestReleaseVersion = latestReleaseVersion.Remove(0, 1);
            return latestReleaseVersion.IsGreaterThan(current, '.') ? latestRelease : null;
        }

        public async static Task<string> GetLatestReleaseNote()
        {
            return (await GetLatestVersion()).Body;
        }

        public static async Task<StorageFolder> GetDownloadsFolder()
        {
            if (StorageApplicationPermissions.FutureAccessList.ContainsItem("DownloadsFolder"))
            {
                try
                {
                    return await StorageApplicationPermissions.FutureAccessList.GetFolderAsync("DownloadsFolder");
                }
                catch (Exception ex)
                {
                    //Helper.ThrowException(ex);
                }
            }
            var result = await Helper.MessageBox("Please select the Downloads folder.", Helper.SoundHelper.Sound.Alert, "Select Location", "Browse");
            if (result == 0)
            {
                var folderPicker = new FolderPicker
                {
                    SuggestedStartLocation = PickerLocationId.Downloads
                };
                folderPicker.FileTypeFilter.Add("*");
                StorageFolder DownloadsFolderTest = await folderPicker.PickSingleFolderAsync();
                if (DownloadsFolderTest != null)
                {
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace("DownloadsFolder", DownloadsFolderTest);
                    return DownloadsFolderTest;
                }
            }
            return null;
        }
    }
}
