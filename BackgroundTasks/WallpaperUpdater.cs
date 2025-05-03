using CMDInjectorHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using Windows.ApplicationModel.Background;
using Windows.System.UserProfile;

namespace BackgroundTasks
{
    public sealed class WallpaperUpdater : IBackgroundTask
    {
        private BackgroundTaskDeferral _Deferral { get; set; }

        readonly uint[] colors = { 16711680, 65280, 255, 65535, 16711935, 16776960 };

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _Deferral = taskInstance.GetDeferral();

            Helper.Init();

            if (AppSettings.GlanceAutoColorEnabled)
            {
                RegEdit.SetRegValueEx(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "SOFTWARE\\OEM\\Nokia\\lpm",
                    "ClockAndIndicatorsCustomColor",
                    RegistryType.REG_DWORD,
                    colors[AppSettings.GlanceColorIndex].ToString()
                );
                AppSettings.GlanceColorIndex += (AppSettings.GlanceColorIndex < 5).ToInt();
            }

            if (AppSettings.StartWallSwitch)
            {
                if (AppSettings.StartWallTrigger.ToBoolean())
                {
                    int interval = AppSettings.StartWallInterval;

                    DateTime dateTime = DateTime.ParseExact(
                        AppSettings.StartWallTime, "dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture
                    );
                    DateTime currentTime = DateTime.Now;

                    // Hmm... I am not able to imagine how this really works
                    // FIXME.
                    if ((currentTime - dateTime).Minutes < interval)
                    {
                        goto Update;
                    }

                    AppSettings.StartWallTime = currentTime.ToString("dd/MM/yy HH:mm:ss");
                }

                var library = await TweakBoxHelper.GetWallpaperLibrary();
                var files = new List<Windows.Storage.StorageFile>( await library.GetFilesAsync() );

                if (AppSettings.StartWallImagePosition >= files.Count)
                {
                    AppSettings.StartWallImagePosition = 0;
                }

                // support more?
                // FIXME
                List<string> imageExts = new List<string>
                {
                    ".jpg", ".jpeg", ".png"
                };
                
                var targetFile =
                    files.Find(elm => imageExts.Contains(Path.GetExtension(elm.Name).ToLower()) &&
                                      AppSettings.StartWallImagePosition == files.IndexOf(elm));

                var newFile = await targetFile.CopyAsync(
                    Helper.localFolder,
                    Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(targetFile.Path)
                );
                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                var result = await profileSettings.TrySetWallpaperImageAsync(newFile);
                AppSettings.StartWallImagePosition += 1;
                await newFile.DeleteAsync();
            }
            
            // It is only used once.
            // FIXME
            Update:
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    var currentTime = DateTime.Now;
                    if (currentTime >= AppSettings.LoadSettings("UpdateNotifyTime", currentTime))
                    {
                        bool isAlreadyExist = Helper.NotificationHelper.IsToastAlreadyThere("CheckUpdateTag");
                        if (!isAlreadyExist)
                        {
                            var isAvailable = await AboutHelper.IsNewUpdateAvailable();
                            if (isAvailable != null)
                            {
                                Helper.NotificationHelper.PushNotification("A new version of CMD Injector is available.", "Update Available", "DownloadUpdate", "CheckUpdateTag", 0);
                                AppSettings.SaveSettings("UpdateNotifyTime", currentTime.AddHours(12));
                            }
                        }
                    }
                }

            _Deferral.Complete();
        }
    }
}
