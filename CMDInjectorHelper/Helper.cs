using Microsoft.Toolkit.Uwp.Notifications;
using ndtklib;
using Newtonsoft.Json;
using OEMSharedFolderAccessLib;
using SecureBootRuntimeComponent;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Foundation;
using Windows.Networking.BackgroundTransfer;
using Windows.Security.Credentials.UI;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using WinUniversalTool;

public static class BoolExtension
{
    public static int ToInt(this bool value)
    {
        return value ? 1 : 0;
    }
}

public static class StringExtension
{
    public static int ToInt32(this string value)
    {
        return Convert.ToInt32(value);
    }
}

namespace CMDInjectorHelper
{
    /// <summary>
    /// Variables that are widely used within the application.
    /// </summary>
    public static class Globals
    {
        public static bool splashScreenDisplayed = false;

        /// <summary>
        /// Confirms that the user is "logged in".
        /// </summary>
        public static bool userVerified = false;

        /// <summary>
        /// Windows build number.
        /// The latest, non-Insider build of Windows 10 Mobile is 1709 (15063)
        /// </summary>
        public static ulong build = (ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion) & 0x00000000FFFF0000L) >> 16;

        /// <summary>
        /// Pretty much CMD Injector version.
        /// </summary>
        public static int currentBatchVersion =
            string.Format("{0}{1}{2}{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor,
                          Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision).ToInt32();
        /// <summary>
        /// CMD Injector version, each part is separated by a dot.
        /// </summary>
        public static string currentVersion =
            string.Format("{0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor,
                          Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);

        public static string InjectedBatchVersion {
            get {
                return File.Exists(@"C:\Windows\System32\CMDInjectorVersion.dat") ? File.ReadAllText(@"C:\Windows\System32\CMDInjectorVersion.dat", Encoding.UTF8) : "0000";
            }
        }

        public static StorageFolder installedLocation = Package.Current.Installed­Location;
        public static StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        public static StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
        public static StorageFolder externalFolder = KnownFolders.RemovableDevices;

        public static Color color = Colors.Black;
        public static EventHandler pageNavigation;
        public static Rect rect;
    }

    public static class Helper
    {
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static TelnetClient tClient = new TelnetClient(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);

        public static bool splashScreenDisplayed = false;
        public static bool userVerified = false;

        /// <summary>
        /// Windows build number.
        /// The latest, non-Insider build of Windows 10 Mobile is 1709 (15063)
        /// </summary>
        public static ulong build = (ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion) & 0x00000000FFFF0000L) >> 16;

        /// <summary>
        /// Pretty much CMD Injector version.
        /// </summary>
        public static int currentBatchVersion = Convert.ToInt32(string.Format("{0}{1}{2}{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision));

        /// <summary>
        /// CMD Injector version, each part is separated by a dot.
        /// </summary>
        public static string currentVersion = string.Format("{0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);

        public static string InjectedBatchVersion { get { return File.Exists(@"C:\Windows\System32\CMDInjectorVersion.dat") ? File.ReadAllText(@"C:\Windows\System32\CMDInjectorVersion.dat", Encoding.UTF8) : "0000"; } }

        public static StorageFolder installedLocation = Package.Current.Installed­Location;
        public static StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        public static StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
        public static StorageFolder externalFolder = KnownFolders.RemovableDevices;

        public static Color color = Colors.Black;
        public static EventHandler pageNavigation;
        public static Rect rect;

        private static COEMSharedFolder oem = new COEMSharedFolder();
        private static NRPC rpc = new NRPC();
        private static PolicyProvisionNative sbp = new PolicyProvisionNative();

        private static async void Connect()
        {
            await tClient.Connect();

            long i = 0;
            while (tClient.IsConnected == false && i < 150)
            {
                await Task.Delay(100);
                i++;
            }

            if (i >= 150)
                ThrowException(new TimeoutException("Timed out waiting for the Telnet client to be ready"));
        }

        public static void Init()
        {
            oem.RPC_Init();
            rpc.Initialize();
            Connect();
        }

        public static async Task WaitAppLaunch()
        {
            if (build > 10572)
            {
                while (LocalSettingsHelper.LoadSettings("SplashScreen", true))
                {
                    await Task.Delay(200);
                    if (Globals.splashScreenDisplayed)
                    {
                        break;
                    }
                }

                while (LocalSettingsHelper.LoadSettings("LoginTogReg", true) &&
                       (await UserConsentVerifier.CheckAvailabilityAsync()) == UserConsentVerifierAvailability.Available)
                {
                    await Task.Delay(200);
                    if (Globals.userVerified)
                    {
                        break;
                    }
                }
            }
        }

        #region Permission and capabilities
        public static bool oemPublicDir_Allowed;
        public static bool extendedBackgroundTaskTime_Allowed;
        public static bool extendedExecutionUnconstrained_Allowed;
        public static bool fullPublicFolderCap;
        public static bool chamberPfpDataReadWrite;
        public static bool chamberPfpCodeReadWrite;

        /// <summary>
        /// Check if CMD Injector is allowed to do stuff.
        /// </summary>
        /// <returns>A boolean.</returns>
        public static async Task<bool> IsCapabilitiesAllowed()
        {
            var manifest = await installedLocation.GetFileAsync("AppxManifest.xml");
            var manifestText = await FileIO.ReadTextAsync(manifest);
            var reqCaps = RegistryHelper.GetRegValue(
                RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE,
                "Software\\Microsoft\\SecurityManager\\Applications\\CMDINJECTOR_KQYNG60ENG17C",
                "RequiredCapabilities",
                RegistryHelper.RegistryType.REG_MULTI_SZ
            );

            oemPublicDir_Allowed = manifestText.Contains("oemPublicDirectory");
            extendedBackgroundTaskTime_Allowed = manifestText.Contains("extendedBackgroundTaskTime");
            extendedExecutionUnconstrained_Allowed = manifestText.Contains("extendedExecutionUnconstrained");

            fullPublicFolderCap = manifestText.Contains("id_cap_public_folder_full") && reqCaps.Contains("id_cap_public_folder_full");
            chamberPfpCodeReadWrite = manifestText.Contains("id_cap_chamber_profile_code_rw") && reqCaps.Contains("id_cap_chamber_profile_code_rw");
            chamberPfpDataReadWrite = manifestText.Contains("id_cap_chamber_profile_data_rw") && reqCaps.Contains("id_cap_chamber_profile_data_rw");

            return oemPublicDir_Allowed && extendedExecutionUnconstrained_Allowed && extendedBackgroundTaskTime_Allowed &&
                   fullPublicFolderCap && chamberPfpDataReadWrite && chamberPfpCodeReadWrite;

        }

        public static async Task<bool> AskCapabilitiesPermission()
        {
            var question =
                "Allow App to access the following capabilities to use certain features?" + Environment.NewLine +
                "• extendedBackgroundTaskTime" + Environment.NewLine +
                "• extendedExecutionUnconstrained" + Environment.NewLine +
                "• oemPublicDirectory" + Environment.NewLine +
                "• id_cap_chamber_profile_code_rw" + Environment.NewLine +
                "• id_cap_chamber_profile_data_rw" + Environment.NewLine +
                "• id_cap_public_folder_full" + Environment.NewLine + Environment.NewLine +
                "The App will close or relaunch itself on allow.";

            if (build < 15063)
            {
                var result = await MessageBox(question, SoundHelper.Sound.Alert, "Permission", "Deny", true, "Allow");
                if (result != 0)
                {
                    return false;
                }
            }
            else
            {
                ContentDialog contentDialog = new ContentDialog
                {
                    Title = "Permission",
                    Content = question,
                    PrimaryButtonText = "Allow",
                    CloseButtonText = "Deny",
                };
                SoundHelper.PlaySound(SoundHelper.Sound.Alert);
                var result = await contentDialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                {
                    return false;
                }
            }

            var manifest = await installedLocation.GetFileAsync("AppxManifest.xml");
            var duplicateManifest = await manifest.CopyAsync(cacheFolder, "AppxManifest.xml", NameCollisionOption.ReplaceExisting);
            var manifestText = await FileIO.ReadTextAsync(manifest);

            using (var stream = await duplicateManifest.OpenStreamForWriteAsync())
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(stream);

                var nsUri = xml.DocumentElement.GetNamespaceOfPrefix("rescap");
                var caps = xml.GetElementsByTagName("Capabilities");

                if (caps.Count > 0)
                {
                    // One way is to create an array of capabilities, iterate that array
                    // The code will be much cleaner. But as we've already stored the
                    // check results we don't do that. At least for now.

                    if (!extendedBackgroundTaskTime_Allowed)
                    {
                        var newNode = xml.CreateElement("rescap", "Capability", nsUri);
                        newNode.SetAttribute("Name", "extendedBackgroundTaskTime");
                        caps[0].AppendChild(newNode);
                    }

                    if (!extendedExecutionUnconstrained_Allowed)
                    {
                        var newNode = xml.CreateElement("rescap", "Capability", nsUri);
                        newNode.SetAttribute("Name", "extendedExecutionUnconstrained");
                        caps[0].AppendChild(newNode);
                    }

                    if (!oemPublicDir_Allowed)
                    {
                        var newNode = xml.CreateElement("rescap", "Capability", nsUri);
                        newNode.SetAttribute("Name", "oemPublicDirectory");
                        caps[0].AppendChild(newNode);
                    }
                    if (!chamberPfpCodeReadWrite)
                    {
                        var newNode = xml.CreateElement("rescap", "Capability", nsUri);
                        newNode.SetAttribute("Name", "id_cap_chamber_profile_code_rw");
                        caps[0].AppendChild(newNode);
                    }

                    if (!chamberPfpDataReadWrite)
                    {
                        var newNode = xml.CreateElement("rescap", "Capability", nsUri);
                        newNode.SetAttribute("Name", "id_cap_chamber_profile_data_rw");
                        caps[0].AppendChild(newNode);
                    }

                    if (!fullPublicFolderCap)
                    {
                        var newNode = xml.CreateElement("rescap", "Capability", nsUri);
                        newNode.SetAttribute("Name", "id_cap_public_folder_full");
                        caps[0].AppendChild(newNode);
                    }

                    stream.SetLength(0);
                    xml.Save(stream);
                    await duplicateManifest.CopyAndReplaceAsync(manifest);
                }
                stream.Flush();
            }

            File.Delete($"{cacheFolder.Path}\\UnlockEnd.txt");
            File.Delete($"{cacheFolder.Path}\\UnlockResult.txt");

            RegistryHelper.SetRegValue(RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\SecurityManager\\CapabilityClasses", "ID_CAP_CHAMBER_PROFILE_CODE_RW", RegistryHelper.RegistryType.REG_MULTI_SZ, "CAPABILITY_CLASS_FIRST_PARTY_APPLICATIONS CAPABILITY_CLASS_THIRD_PARTY_APPLICATIONS ");
            RegistryHelper.SetRegValue(RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\SecurityManager\\CapabilityClasses", "ID_CAP_CHAMBER_PROFILE_DATA_RW", RegistryHelper.RegistryType.REG_MULTI_SZ, "CAPABILITY_CLASS_FIRST_PARTY_APPLICATIONS CAPABILITY_CLASS_THIRD_PARTY_APPLICATIONS ");
            RegistryHelper.SetRegValue(RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\SecurityManager\\CapabilityClasses", "ID_CAP_PUBLIC_FOLDER_FULL", RegistryHelper.RegistryType.REG_MULTI_SZ, "CAPABILITY_CLASS_FIRST_PARTY_APPLICATIONS CAPABILITY_CLASS_THIRD_PARTY_APPLICATIONS ");

            if (tClient.IsConnected && HomeHelper.IsConnected())
            {
                // The task below was not awaited before. Why? Also should I?
                await tClient.Send($"MinDeployAppx.exe /Register /PackagePath:\"{installedLocation.Path}\\AppxManifest.xml\" /DeploymentOption:1 >{cacheFolder.Path}\\UnlockResult.txt 2>&1&echo. >{cacheFolder.Path}\\UnlockEnd.txt");
                //while (!File.Exists($"{cacheFolder.Path}\\UnlockEnd.txt"))
                //{
                //    await Task.Delay(200);
                //}
                var unlockResult = File.ReadAllText($"{cacheFolder.Path}\\UnlockResult.txt");
                if (!unlockResult.Contains("ReturnCode:[0x0]"))
                {
                    _ = MessageBox("Failed to access the capabilities, please try to allow again from the App settings.", SoundHelper.Sound.Error, "Error");
                    return false;
                }
            }
            else
            {
                _ = MessageBox(HomeHelper.GetTelnetTroubleshoot(), SoundHelper.Sound.Error, "Error");
                return false;
            }
            return true;
        }
        #endregion

        public static async Task<int?> MessageBox(string content, SoundHelper.Sound sound = SoundHelper.Sound.Alert, string title = "", string buttonText = "Close", bool seconadaryButton = false, string seconadaryButtonText = "Ok")
        {
            try
            {
                var messageDialog = new MessageDialog(content, title);
                if (seconadaryButton)
                {
                    messageDialog.Commands.Add(new UICommand(seconadaryButtonText) { Id = 0 });
                    messageDialog.Commands.Add(new UICommand(buttonText) { Id = 1 });
                }
                else
                {
                    messageDialog.Commands.Add(new UICommand(buttonText) { Id = 0 });
                }

                IUICommand result = null;
                SoundHelper.PlaySound(sound);
                result = await messageDialog.ShowAsync();
                return (int)result.Id;
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }

        public static void ThrowException(Exception ex)
        {
            _ = MessageBox(ex.Message + "\n" + ex.StackTrace, SoundHelper.Sound.Error);
            throw ex; // Hmm...
        }

        public static bool IsStrAGraterThanStrB(string strA, string strB, char separator)
        {
            var result = false;
            var stringA = strA.Trim().Split(separator);
            var stringB = strB.Trim().Split(separator);

            int length = Math.Max(stringA.Length, stringB.Length);

            for (int i = 0; i < length; i++)
            {
                var partOfA = int.Parse(stringA[i]);
                var partOfB = int.Parse(stringB[i]);

                if (partOfA != partOfB)
                {
                    result = partOfA > partOfB;
                    break;
                }
            }
            return result;
        }

        public static uint CopyFile(string source, string destination)
        {
            return rpc.FileCopy(source, destination, 0);
        }
        
        public static uint CopyFromAppRoot(string path, string destination)
        {
            return CopyFile($"{installedLocation.Path}\\{path}", destination);
        }

        public static uint RebootSystem()
        {
            return rpc.SystemReboot();
        }

        public static bool IsSecureBootPolicyInstalled()
        {
            try
            {
                // CHECK: Is $"{{{Guid.Empty}}}" the same as { + Guid.Empty + } ?
                return sbp.GetSecureBootPolicyPublisher() != $"{{{Guid.Empty}}}" && !string.IsNullOrEmpty(sbp.GetSecureBootPolicyPublisher());
            }
            catch
            {
                return false;
            }
        }

        public static async Task DownloadFile(Uri downloadURL, StorageFile file, IProgress<int> progression)
        {
            DownloadOperation downloadOperation;
            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            BackgroundDownloader backgroundDownloader = new BackgroundDownloader();

            //Set url and file
            downloadOperation = backgroundDownloader.CreateDownload(downloadURL, file);

            Progress<DownloadOperation> progress = new Progress<DownloadOperation>((dOperation) =>
            {
                var _total = (long)dOperation.Progress.TotalBytesToReceive;
                var _received = (long)dOperation.Progress.BytesReceived;
                int _progress = (int)(100 * (_received / (double)_total));
                switch (dOperation.Progress.Status)
                {
                    case BackgroundTransferStatus.Running:
                        //Update your progress here
                        progression.Report(_progress);
                        break;
                    case BackgroundTransferStatus.Error:
                        //An error occured while downloading
                        progression.Report(-100);
                        break;
                    case BackgroundTransferStatus.PausedNoNetwork:
                        //No network detected
                        progression.Report(-50);
                        break;
                    case BackgroundTransferStatus.PausedCostedNetwork:
                        //Download paused because of metered connection
                        break;
                    case BackgroundTransferStatus.PausedByApplication:
                        //Download paused
                        break;
                    case BackgroundTransferStatus.Canceled:
                        //Download canceled
                        progression.Report(-100);
                        break;
                }
                if (_progress >= 100)
                {
                    //Download Done
                    dOperation = null;
                }
            });

            await downloadOperation.StartAsync().AsTask(cancellationToken.Token, progress);
        }

        public static class LocalSettingsHelper
        {
            public static void SaveSettings(string key, bool updateValue, string fileName = null)
            {
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(key, updateValue, fileName);
            }

            public static void SaveSettings(string key, DateTime updateValue, string fileName = null)
            {
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(key, updateValue, fileName);
            }

            public static void SaveSettings(string key, decimal updateValue, string fileName = null)
            {
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(key, updateValue, fileName);
            }

            public static void SaveSettings(string key, double updateValue, string fileName = null)
            {
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(key, updateValue, fileName);
            }

            public static void SaveSettings(string key, float updateValue, string fileName = null)
            {
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(key, updateValue, fileName);
            }

            public static void SaveSettings(string key, int updateValue, string fileName = null)
            {
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(key, updateValue, fileName);
            }

            public static void SaveSettings(string key, long updateValue, string fileName = null)
            {
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(key, updateValue, fileName);
            }

            public static void SaveSettings(string key, string updateValue, string fileName = null)
            {
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue(key, updateValue, fileName);
            }

            public static bool LoadSettings(string key, bool defaultValue, string fileName = null)
            {
                return Plugin.Settings.CrossSettings.Current.GetValueOrDefault(key, defaultValue, fileName);
            }

            public static DateTime LoadSettings(string key, DateTime defaultValue, string fileName = null)
            {
                return Plugin.Settings.CrossSettings.Current.GetValueOrDefault(key, defaultValue, fileName);
            }

            public static decimal LoadSettings(string key, decimal defaultValue, string fileName = null)
            {
                return Plugin.Settings.CrossSettings.Current.GetValueOrDefault(key, defaultValue, fileName);
            }

            public static double LoadSettings(string key, double defaultValue, string fileName = null)
            {
                return Plugin.Settings.CrossSettings.Current.GetValueOrDefault(key, defaultValue, fileName);
            }

            public static float LoadSettings(string key, float defaultValue, string fileName = null)
            {
                return Plugin.Settings.CrossSettings.Current.GetValueOrDefault(key, defaultValue, fileName);
            }

            public static int LoadSettings(string key, int defaultValue, string fileName = null)
            {
                return Plugin.Settings.CrossSettings.Current.GetValueOrDefault(key, defaultValue, fileName);
            }

            public static long LoadSettings(string key, long defaultValue, string fileName = null)
            {
                return Plugin.Settings.CrossSettings.Current.GetValueOrDefault(key, defaultValue, fileName);
            }

            public static string LoadSettings(string key, string defaultValue, string fileName = null)
            {
                return Plugin.Settings.CrossSettings.Current.GetValueOrDefault(key, defaultValue, fileName);
            }
        }

        public static class RegistryHelper
        {
            public enum RegistryHive
            {
                HKEY_CLASSES_ROOT = 0,
                HKEY_LOCAL_MACHINE = 1,
                HKEY_CURRENT_USER = 2,
                HKEY_CURRENT_CONFIG = 3,
                HKEY_USERS = 4,
                HKEY_PERFORMANCE_DATA = 5,
                HKEY_DYN_DATA = 6,
                HKEY_CURRENT_USER_LOCAL_SETTINGS = 7
            }

            public enum RegistryType
            {
                REG_SZ = 1,
                REG_EXPAND_SZ = 2,
                REG_BINARY = 3,
                REG_DWORD = 4,
                REG_DWORD_BIG_ENDIAN = 5,
                REG_LINK = 6,
                REG_MULTI_SZ = 7,
                REG_RESOURCE_LIST = 8,
                REG_FULL_RESOURCE_DESCRIPTOR = 9,
                REG_RESOURCE_REQUIREMENTS_LIST = 10,
                REG_QWORD = 11,
            }

            public static string GetRegValue(RegistryHive hKey, string subKey, string value, RegistryType type)
            {
                return oem.rget((uint)hKey, subKey, value, (uint)type);
            }

            public static void SetRegValue(RegistryHive hKey, string subKey, string value, RegistryType type, string buffer)
            {
                oem.rset((uint)hKey, subKey, value, (uint)type, buffer, 0);
            }

            public static uint SetRegValueEx(RegistryHive hKey, string subKey, string value, RegistryType type, string buffer)
            {
                byte[] byteArr = null;
                switch (type)
                {
                    case RegistryType.REG_SZ:
                        byteArr = Encoding.Unicode.GetBytes(buffer + '\0');
                        break;
                    case RegistryType.REG_EXPAND_SZ:
                        byteArr = Encoding.Unicode.GetBytes(buffer + '\0');
                        break;
                    case RegistryType.REG_BINARY:
                        byteArr = StringToByteArrayFastest(buffer);
                        break;
                    case RegistryType.REG_DWORD:
                        byteArr = BitConverter.GetBytes(uint.Parse(buffer));
                        break;
                    case RegistryType.REG_MULTI_SZ:
                        byteArr = Encoding.Unicode.GetBytes(buffer + '\0');
                        break;
                }
                return rpc.RegSetValue((uint)hKey, subKey, value, (uint)type, byteArr);
            }

            private static int GetHexVal(char hex)
            {
                var val = hex;
                //For uppercase A-F letters:
                //return val - (val < 58 ? 48 : 55);
                //For lowercase a-f letters:
                //return val - (val < 58 ? 48 : 87);
                //Or the two combined, but a bit slower:
                return val - (val < 58 ? 48 : val < 97 ? 55 : 87);
            }

            private static byte[] StringToByteArrayFastest(string hex)
            {
                try
                {
                    if (hex.Length % 2 == 1)
                    {
                        throw new Exception("The binary key cannot have an odd number of digits");
                    }
                    var arr = new byte[hex.Length >> 1];
                    for (int i = 0; i < hex.Length >> 1; ++i)
                    {
                        arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));
                    }
                    return arr;
                }
                catch (Exception ex)
                {
                    ThrowException(ex);
                    return null;
                }
            }
        }

        public static class NotificationHelper
        {
            public static void PushNotification(
                string content, string title, string argument = "", string tag = null, int expireTime = 30,
                string buttonContent = null, string buttonArgumentKey = null, string buttonArgumentValue = null)
            {
                var builder = new ToastContentBuilder()
                    .AddArgument(argument)
                    .AddText(title)
                    .AddText(content);

                if (buttonContent != null)
                {
                    builder = builder.AddButton(
                        new ToastButton()
                            .SetContent(buttonContent)
                            .AddArgument(buttonArgumentKey, buttonArgumentValue)
                    );
                }

                builder.Show(toast =>
                {
                    if (expireTime != 0)
                    {
                        toast.ExpirationTime = DateTime.Now.AddSeconds(expireTime);
                    }

                    if (tag != null)
                    {
                        toast.Tag = tag;
                    }
                });
            }

            public static bool IsToastAlreadyThere(string tag)
            {
                var toastsHistory = new List<ToastNotification>(
                    ToastNotificationManager.History.GetHistory()
                ).Find(o => o.Tag == tag);

                return toastsHistory != null;
            }
        }

        public static class SoundHelper
        {
            private static MediaElement Element { get; set; }
            private static StorageFolder Sounds { get; set; }

            public enum Sound
            {
                Alert,
                Capture,
                Error,
                Lock,
                StartRecord,
                StopRecord,
                Tick
            }

            public async static void Init()
            {
                Element = new MediaElement();
                var assets = await installedLocation.GetFolderAsync("Assets");
                Sounds = await assets.GetFolderAsync("Sounds");
            }

            public async static void PlaySound(Sound sound)
            {
                var file = await Sounds.GetFileAsync($"{sound.ToString()}.wav");
                var stream = await file.OpenAsync(FileAccessMode.Read);
                Element.SetSource(stream, "");
                Element.Play();
            }
        }

        public static class Archive
        {
            public static void CreateZip(string sourceFolder, string destinationFile, CompressionLevel level, bool includeBaseFolder = false, bool overWrite = false)
            {
                if (overWrite && File.Exists(destinationFile))
                {
                    File.Delete(destinationFile);
                }

                ZipFile.CreateFromDirectory(sourceFolder, destinationFile, level, includeBaseFolder);
            }

            public static void ExtractZip(string zipFile, string destinationFolder)
            {
                ZipFile.ExtractToDirectory(zipFile, destinationFolder);
            }

            public static async Task<StorageFolder> ExtractZipWithProgress(StorageFile zipFile, StorageFolder destinationFolder, bool treeStructure = true, bool baseFolder = true, IProgress<double> progress = null)
            {
                try
                {
                    if (baseFolder)
                    {
                        destinationFolder = await destinationFolder.CreateFolderAsync(zipFile.DisplayName, CreationCollisionOption.ReplaceExisting);
                    }

                    using (Stream fileStream = await zipFile.OpenStreamForReadAsync())
                    using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                    {
                        int totalEntries = archive.Entries.Count;
                        int currentEntry = 0;

                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            currentEntry++;

                            if (progress != null)
                            {
                                progress.Report((double)currentEntry / totalEntries * 100);
                            }

                            string entryPath = entry.FullName;
                            string[] directories = entryPath.Split('/');

                            StorageFolder currentFolder = destinationFolder;

                            if (treeStructure)
                            {
                                for (int i = 0; i < directories.Length - 1; i++)
                                {
                                    string directoryName = directories[i];
                                    currentFolder = await currentFolder.CreateFolderAsync(directoryName, CreationCollisionOption.OpenIfExists);
                                }
                            }

                            string fileName = directories[directories.Length - 1];
                            StorageFile extractedFile = await currentFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

                            using (Stream entryStream = entry.Open())
                            using (IRandomAccessStream extractedFileStream = await extractedFile.OpenAsync(FileAccessMode.ReadWrite))
                            using (Stream outputStream = extractedFileStream.AsStreamForWrite())
                            {
                                await entryStream.CopyToAsync(outputStream);
                            }
                        }
                    }

                    return destinationFolder;
                }
                catch (Exception ex)
                {
                    //ThrowException(ex);
                    return null;
                }
            }

            public static bool CheckFileExist(string zipFilePath, string fileName)
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
                {
                    var entries = new List<ZipArchiveEntry>(archive.Entries);
                    return entries.Find(obj => obj.FullName == fileName) != null;
                }
            }

            public static async Task<string> ReadTextFromZip(string zipFilePath, string fileName)
            {
                using (var zipArchive = ZipFile.OpenRead(zipFilePath))
                {
                    var jsonEntry = zipArchive.GetEntry(fileName);

                    using (var stream = jsonEntry.Open())
                    using (var reader = new StreamReader(stream))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
            }
        }

        public static class Json
        {
            public static void ObjectToJsonFile(object obj, string destinationFile)
            {
                File.WriteAllText(destinationFile, JsonConvert.SerializeObject(obj, Formatting.Indented));
            }
        }

        public class BackgroundTaskHelper
        {
            private static ExtendedExecutionSession session = null;
            private static int taskCount = 0;
            static bool IsSessionStarted = false;
            static bool SupportBackground = false;
            static readonly TypedEventHandler<object, ExtendedExecutionRevokedEventArgs> revoked = SessionRevoked;

            public static async Task RequestSessionAsync(String description)
            {
                try
                {
                    // The previous Extended Execution must be closed before a new one can be requested.       
                    ClearSession();
                    IsSessionStarted = true;
                    var newSession = new ExtendedExecutionSession
                    {
                        Reason = ExtendedExecutionReason.Unspecified,
                        Description = description
                    };
                    newSession.Revoked += SessionRevoked;

                    // Add a revoked handler provided by the app in order to clean up an operation that had to be halted prematurely
                    if (revoked != null)
                    {
                        newSession.Revoked += revoked;
                    }

                    ExtendedExecutionResult result = await newSession.RequestExtensionAsync();

                    switch (result)
                    {
                        case ExtendedExecutionResult.Allowed:
                            session = newSession;
                            SupportBackground = true;
                            break;
                        default:
                        case ExtendedExecutionResult.Denied:
                            newSession.Dispose();
                            SupportBackground = false;
                            break;
                    }
                    //return result;
                }
                catch (Exception ex)
                {
                    //return new ExtendedExecutionResult();
                }
            }

            public static void ClearSession()
            {
                try
                {
                    if (session != null)
                    {
                        session.Dispose();
                        session = null;
                    }

                    taskCount = 0;

                }
                catch (Exception ex)
                {

                }
                IsSessionStarted = false;
            }

            private static async void SessionRevoked(object sender, ExtendedExecutionRevokedEventArgs args)
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    switch (args.Reason)
                    {
                        case ExtendedExecutionRevokedReason.Resumed:
                            //Helpers.ShowToastNotification("Background Session", "Extended execution revoked due to returning to foreground.");
                            break;

                        case ExtendedExecutionRevokedReason.SystemPolicy:
                            //Helpers.ShowToastNotification("Background Session", "Extended execution revoked due to system policy.");
                            break;
                    }
                });
                
                //The session has been prematurely revoked due to system constraints, ensure the session is disposed
                if (session != null)
                {
                    session.Dispose();
                    session = null;
                }

                taskCount = 0;

                IsSessionStarted = false;
            }

            private const string ThemeTaskName = "ThemeUpdater";

            private const string ThemeTaskEntryPoint = "BackgroundTasks.ThemeUpdater";

            public static bool IsThemeTaskActivated()
            {
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == ThemeTaskName)
                    {
                        return true;
                    }
                }
                return false;
            }

            public static async Task<bool> RegisterThemeTask(uint? interval = null, SystemTrigger trigger = null)
            {
                return await RegisterBackgroundTask(
                    ThemeTaskName,
                    ThemeTaskEntryPoint,
                    interval,
                    trigger
                );
            }

            public static void UnregisterThemeTask()
            {
                UnregisterBackgroundTask(ThemeTaskName);
            }

            private const string WallpaperTaskName = "WallpaperUpdater";

            private const string WallpaperTaskEntryPoint = "BackgroundTasks.WallpaperUpdater";

            public static bool IsWallpaperTaskActivated()
            {
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == WallpaperTaskName)
                    {
                        return true;
                    }
                }
                return false;
            }

            public static async Task<bool> RegisterWallpaperTask(uint? interval = null, SystemTrigger trigger = null)
            {
                return await RegisterBackgroundTask(
                    WallpaperTaskName,
                    WallpaperTaskEntryPoint,
                    interval,
                    trigger
                );
            }

            public static void UnregisterWallpaperTask()
            {
                UnregisterBackgroundTask(WallpaperTaskName);
            }

            public static async Task<bool> RegisterBackgroundTask(string taskName, string entryPoint, uint? interval = null, SystemTrigger trigger = null, SystemCondition condition = null)
            {
                try
                {
                    foreach (var task in BackgroundTaskRegistration.AllTasks)
                    {
                        if (task.Value.Name == taskName)
                        {
                            return false;
                        }
                    }

                    BackgroundExecutionManager.RemoveAccess();

                    BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
                    if (status == BackgroundAccessStatus.DeniedByUser || status == BackgroundAccessStatus.DeniedBySystemPolicy)
                    {
                        return false;
                    }

                    var taskBuilder = new BackgroundTaskBuilder()
                    {
                        Name = taskName,
                        TaskEntryPoint = entryPoint
                    };

                    if (interval != null)
                    {
                        var timeTrigger = new TimeTrigger(interval.Value, false);
                        taskBuilder.SetTrigger(timeTrigger);
                    }

                    if (trigger != null)
                    {
                        taskBuilder.SetTrigger(trigger);
                    }

                    if (condition != null)
                    {
                        taskBuilder.AddCondition(condition);
                    }

                    taskBuilder.Register();
                }
                catch (Exception ex)
                {
                    ThrowException(ex);
                    return false;
                }
                return true;
            }

            public static bool UnregisterBackgroundTask(string taskName)
            {
                try
                {
                    foreach (var task in BackgroundTaskRegistration.AllTasks)
                    {
                        if (task.Value.Name == taskName)
                        {
                            BackgroundExecutionManager.RemoveAccess();
                            task.Value.Unregister(false);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ThrowException(ex);
                }
                return false;
            }
        }
    }
}
