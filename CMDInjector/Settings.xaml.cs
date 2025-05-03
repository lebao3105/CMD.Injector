using System;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.UI.Xaml.Shapes;
using System.Reflection;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using CMDInjectorHelper;
using WinUniversalTool;

namespace CMDInjector
{
    public sealed partial class Settings : Page
    {
        static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        TelnetClient tClient = new TelnetClient(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
        bool flag = false;

        private void Connect()
        {
            _ = tClient.Connect();
            long i = 0;
            while (Helper.IsTelnetConnected() == false && i < 1000000)
            {
                i++;
            }
        }

        private async void GetExternalAsync()
        {
            StorageFolder sdCard = (await Helper.externalFolder.GetFoldersAsync()).FirstOrDefault();
            if (sdCard == null)
            {
                StorageTog.IsEnabled = false;
                StorageTog.IsOn = false;
                AppSettings.SaveSettings("StorageSet", false);
            }
            else
            {
                StorageTog.IsOn = AppSettings.LoadSettings("StorageSet", false);
            }
        }

        public Settings()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            Connect();
            Initialize();
        }

        private async void Initialize()
        {
            if (Helper.build <= 14393)
            {
                if (Helper.build < 14393)
                {
                    ConKeyBtnTog.IsEnabled = false;
                    AppSettings.SaveSettings("ConKeyBtnSet", false);
                }
                BackupFoldBtn.IsEnabled = false;

                if (Helper.build < 10572)
                {
                    MenuTransitionTog.IsEnabled = false;
                }
                else
                {
                    MenuTransitionTog.IsOn = AppSettings.LoadSettings("MenuTransition", true);
                }

                if (Helper.build < 10586)
                {
                    LoginReqTog.IsEnabled = false;
                }
            }

            DefaultTog.IsOn = RegEdit.GetRegValue(
                RegistryHive.HKEY_LOCAL_MACHINE,
                "Software\\Microsoft\\DefaultApplications",
                ".xap", RegistryType.REG_SZ) == "CMDInjector_kqyng60eng17c";

            GetExternalAsync();
            SplashScrTog.IsOn = AppSettings.SplashScreen;
            SplashScrCombo.SelectedIndex = AppSettings.SplashAnimIndex;

            if (Helper.build >= 10586)
                LoginReqTog.IsOn = AppSettings.LoginTogReg;

            CommandsWrapToggle.IsOn = AppSettings.CommandsTextWrap.Equals(TextWrapping.Wrap);
            ConKeyBtnTog.IsOn = AppSettings.LoadSettings("ConKeyBtnSet", false);
            ConsoleFontSizeBox.SelectedIndex = AppSettings.LoadSettings("ConFontSizeSet", 3);
            ThemeToggle.IsOn = AppSettings.ThemeSettings;
            AccentToggle.IsOn = AppSettings.LoadSettings("AccentSettings", false);
            HamBurMenu.IsOn = AppSettings.LoadSettings("SplitView", false);
            SnapNotifTog.IsOn = AppSettings.LoadSettings("SnapperNotify", true);
            SnapSoundTog.IsOn = AppSettings.LoadSettings("SnapSoundTog", true);
            ArgConfirmTog.IsOn = AppSettings.LoadSettings("TerminalRunArg", true);

            if (await Helper.IsCapabilitiesAllowed())
            {
                ResCapTog.IsOn = true;
                ResCapTog.IsEnabled = false;
            }

            if (!AppSettings.LoadSettings("PMLogPath", false))
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PMLogPath", KnownFolders.DocumentsLibrary);
                AppSettings.SaveSettings("PMLogPath", true);
            }

            var PMLogPath = (await StorageApplicationPermissions.FutureAccessList.GetFolderAsync("PMLogPath")).Path;

            if (PMLogPath.ToUpper() == @"C:\Data\USERS\DefApps\APPDATA\ROAMING\MICROSOFT\WINDOWS\Libraries\Documents.library-ms".ToUpper())
            {
                LogPathBox.Text = "C:\\Data\\Users\\Public\\Documents\\PacMan_Installer.pmlog";
            }
            else
            {
                LogPathBox.Text = $"{PMLogPath}\\PacMan_Installer.pmlog";
            }

            if (ThemeToggle.IsOn)
            {
                CustomTheme.SelectedIndex = AppSettings.Theme;
            }

            RegEdit.GoToKey(RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet");

            UMCIToggle.IsOn = RegEdit.GetRegValue("Control\\CI", "UMCIAuditMode", RegistryType.REG_DWORD).DWORDFlagToBool();
            WifiServiceToggle.IsOn = RegEdit.GetRegValue("services\\KeepWiFiOnSvc", "Start", RegistryType.REG_DWORD) != "00000004";
            BootshToggle.IsOn = RegEdit.GetRegValue("Services\\Bootsh", "Start", RegistryType.REG_DWORD) != "00000004";

            foreach (var color in typeof(Colors).GetRuntimeProperties())
            {
                if (!Globals.CursedColors.Contains(color.Name))
                {
                    var cbi = new ComboBoxItem();
                    var selectColor = new Rectangle()
                    {
                        Width = 20,
                        Height = 20,
                        Margin = new Thickness(0, 0, 10, 0),
                        Fill = new SolidColorBrush((Color)color.GetValue(null))
                    };

                    var colorText = new TextBlock()
                    {
                        Text = color.Name,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    cbi.Content = new StackPanel()
                    {
                        Orientation = Orientation.Horizontal
                    }
                        .AddChildren(selectColor)
                        .AddChildren(colorText);

                    CustomAccent.Items.Add(cbi);
                    SolidColorBrush solidColor = (SolidColorBrush)selectColor.Fill;
                }
            }

            CustomAccent.SelectedIndex = AppSettings.LoadSettings("Accent", 11);

            flag = true;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (await PacManHelper.IsBackupFolderSelected()) BackupFoldBox.Text = (await PacManHelper.GetBackupFolder()).Path;
        }

        private void ThemeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            AppSettings.SaveSettings("ThemeSettings", ThemeToggle.IsOn);
            if (ThemeToggle.IsOn)
            {
                CustomTheme.Visible();
                CustomTheme.SelectedIndex = AppSettings.LoadSettings("Theme", 0);

                if (CustomTheme.SelectedIndex == 0)
                {
                    ((Frame)Window.Current.Content).RequestedTheme = ElementTheme.Dark;
                    Helper.color = Colors.Black;
                }
                else
                {
                    ((Frame)Window.Current.Content).RequestedTheme = ElementTheme.Light;
                    Helper.color = Colors.White;
                }
            }
            else
            {
                CustomTheme.Collapse();
                ((Frame)Window.Current.Content).RequestedTheme = ElementTheme.Default;
                Helper.color = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\Theme",
                    "CurrentTheme", RegistryType.REG_DWORD).IsDWORDStrFullZeros() ? Colors.White : Colors.Black;
            }
        }

        private void AccentToggle_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                AppSettings.SaveSettings("AccentSettings", AccentToggle.IsOn);
                if (AccentToggle.IsOn)
                {
                    CustomAccent.Visible();
                    AccentResources();
                }
                else
                {
                    CustomAccent.Collapse();
                    Color accentColor = new UISettings().GetColorValue(UIColorType.Accent);
                    (Application.Current.Resources["AppAccentColor"] as SolidColorBrush).Color = accentColor;
                    (Application.Current.Resources["ToggleSwitchFillOn"] as SolidColorBrush).Color = accentColor;
                    (Application.Current.Resources["TextControlBorderBrushFocused"] as SolidColorBrush).Color = accentColor;
                    (Application.Current.Resources["RadioButtonOuterEllipseCheckedStroke"] as SolidColorBrush).Color = accentColor;
                    (Application.Current.Resources["SliderTrackValueFill"] as SolidColorBrush).Color = accentColor;
                    (Application.Current.Resources["SliderThumbBackground"] as SolidColorBrush).Color = accentColor;
                    (Application.Current.Resources["SystemControlHighlightAccentBrush"] as SolidColorBrush).Color = accentColor;
                    (Application.Current.Resources["SystemControlHighlightListAccentLowBrush"] as SolidColorBrush).Color = Color.FromArgb(204, accentColor.R, accentColor.G, accentColor.B);
                    (Application.Current.Resources["SystemControlHighlightListAccentHighBrush"] as SolidColorBrush).Color = Color.FromArgb(242, accentColor.R, accentColor.G, accentColor.B);
                    if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                    {
                        var statusBar = StatusBar.GetForCurrentView();
                        if (statusBar != null)
                        {
                            statusBar.ForegroundColor = accentColor;
                        }
                    }
                }
            }
            catch (Exception ex) { Helper.ThrowException(ex); }
        }

        private void CustomTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppSettings.SaveSettings("Theme", CustomTheme.SelectedIndex);
            if (CustomTheme.SelectedIndex == 0)
            {
                ((Frame)Window.Current.Content).RequestedTheme = ElementTheme.Dark;
                Helper.color = Colors.Black;
            }
            else
            {
                ((Frame)Window.Current.Content).RequestedTheme = ElementTheme.Light;
                Helper.color = Colors.White;
            }
        }

        private void CustomAccent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AccentResources();
        }

        private void AccentResources()
        {
            try
            {
                if (flag)
                {
                    int count = 0;
                    foreach (var color in typeof(Colors).GetRuntimeProperties())
                    {
                        // TODO
                        if (color.Name != "AliceBlue" && color.Name != "AntiqueWhite" && color.Name != "Azure" && color.Name != "Beige" && color.Name != "Bisque" && color.Name != "Black" && color.Name != "BlanchedAlmond" && color.Name != "Cornsilk" && color.Name != "FloralWhite" && color.Name != "Gainsboro" && color.Name != "GhostWhite" && color.Name != "Honeydew" && color.Name != "Ivory" && color.Name != "Lavender" && color.Name != "LavenderBlush" && color.Name != "LemonChiffon"
                        && color.Name != "LightCyan" && color.Name != "LightGoldenrodYellow" && color.Name != "LightGray" && color.Name != "LightYellow" && color.Name != "Linen" && color.Name != "MintCream" && color.Name != "MistyRose" && color.Name != "Moccasin" && color.Name != "OldLace" && color.Name != "PapayaWhip" && color.Name != "SeaShell" && color.Name != "Snow" && color.Name != "Transparent" && color.Name != "White" && color.Name != "WhiteSmoke")
                        {
                            if (CustomAccent.SelectedIndex == count)
                            {
                                var accentColor = (Color)color.GetValue(null);
                                (Application.Current.Resources["AppAccentColor"] as SolidColorBrush).Color = accentColor;
                                (Application.Current.Resources["ToggleSwitchFillOn"] as SolidColorBrush).Color = accentColor;
                                (Application.Current.Resources["TextControlBorderBrushFocused"] as SolidColorBrush).Color = accentColor;
                                (Application.Current.Resources["RadioButtonOuterEllipseCheckedStroke"] as SolidColorBrush).Color = accentColor;
                                (Application.Current.Resources["SliderTrackValueFill"] as SolidColorBrush).Color = accentColor;
                                (Application.Current.Resources["SliderThumbBackground"] as SolidColorBrush).Color = accentColor;
                                (Application.Current.Resources["SystemControlHighlightAccentBrush"] as SolidColorBrush).Color = accentColor;
                                (Application.Current.Resources["SystemControlHighlightListAccentLowBrush"] as SolidColorBrush).Color = Color.FromArgb(204, accentColor.R, accentColor.G, accentColor.B);
                                (Application.Current.Resources["SystemControlHighlightListAccentHighBrush"] as SolidColorBrush).Color = Color.FromArgb(242, accentColor.R, accentColor.G, accentColor.B);
                                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                                {
                                    var statusBar = StatusBar.GetForCurrentView();
                                    if (statusBar != null)
                                    {
                                        statusBar.ForegroundColor = accentColor;
                                    }
                                }
                                AppSettings.SaveSettings("Accent", CustomAccent.SelectedIndex);
                                break;
                            }
                            count++;
                        }
                    }
                }
            }
            catch (Exception ex) { Helper.ThrowException(ex); }
        }

        private void HamBurMenu_Toggled(object sender, RoutedEventArgs e)
        {
            AppSettings.SaveSettings("SplitView", HamBurMenu.IsOn);
            Helper.pageNavigation.Invoke(HamBurMenu.IsOn ? 45 : 0, null);
        }

        private void CommandsWrapToggle_Toggled(object sender, RoutedEventArgs e)
        {
            //AppSettings.CommandsTextWrap = CommandsWrapToggle.IsOn;
            AppSettings.SaveSettings("CommandsTextWrap", CommandsWrapToggle.IsOn);
        }

        private void ConsoleFontSizeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppSettings.SaveSettings("ConFontSizeSet", ConsoleFontSizeBox.SelectedIndex);
        }

        private void ConKeyBtnTog_Toggled(object sender, RoutedEventArgs e)
        {
            AppSettings.SaveSettings("ConKeyBtnSet", ConKeyBtnTog.IsOn);
        }

        private void SnapNotifTog_Toggled(object sender, RoutedEventArgs e)
        {
            AppSettings.SaveSettings("SnapperNotify", SnapNotifTog.IsOn);
        }

        private void BootshToggle_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\Services\\Bootsh", "Start", RegistryType.REG_DWORD, BootshToggle.IsOn ? "00000002" : "00000004");
            if (flag)
            {
                BootshIndicator.Visible();
            }
        }

        private void UMCIToggle_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\Control\\CI", "UMCIAuditMode", RegistryType.REG_DWORD, UMCIToggle.IsOn.ToDWORDStr());
            if (flag)
            {
                UMCIModeIndicator.Visible();
            }
        }

        private void WifiServiceToggle_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\KeepWiFiOnSvc", "Start", RegistryType.REG_DWORD, WifiServiceToggle.IsOn ? "00000002" : "00000004");
            if (flag)
            {
                WifiServiceIndicator.Visible();
            }
        }

        private void StorageTog_Toggled(object sender, RoutedEventArgs e)
        {
            AppSettings.SaveSettings("StorageSet", StorageTog.IsOn);
        }

        private void DefaultTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.GoToKey(RegistryHive.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\DefaultApplications");

            if (DefaultTog.IsOn)
            {
                try
                {
                    RegEdit.SetRegValue(".xap", RegistryType.REG_SZ, "CMDInjector_kqyng60eng17c");
                    RegEdit.SetRegValue(".appx", RegistryType.REG_SZ, "CMDInjector_kqyng60eng17c");
                    RegEdit.SetRegValue(".appxbundle", RegistryType.REG_SZ, "CMDInjector_kqyng60eng17c");
                }
                catch (Exception ex) { Helper.ThrowException(ex); }
            }
            else
            {
                if (Helper.IsTelnetConnected() && HomeHelper.IsConnected())
                {
                    RegEdit.DeleteRegValue(".xap");
                    RegEdit.DeleteRegValue(".appx");
                    RegEdit.DeleteRegValue(".appxbundle");
                }
                else
                {
                    Helper.MessageBox(HomeHelper.GetTelnetTroubleshoot(), Helper.SoundHelper.Sound.Error, "Error");
                }
            }
            if (flag == true)
            {
                DefInstIndicator.Visible();
            }
        }

        private async void LogPathBtn_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder LogPath = await folderPicker.PickSingleFolderAsync();
            if (LogPath != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PMLogPath", LogPath);
                LogPathBox.Text = $"{(await StorageApplicationPermissions.FutureAccessList.GetFolderAsync("PMLogPath")).Path}\\PacMan_Installer.pmlog";
            }
        }

        private void ArgConfirmTog_Toggled(object sender, RoutedEventArgs e)
        {
            AppSettings.SaveSettings("TerminalRunArg", ArgConfirmTog.IsOn);
        }

        private void SplashScrTog_Toggled(object sender, RoutedEventArgs e)
        {
            AppSettings.SaveSettings("SplashScreen", SplashScrTog.IsOn);
            SplashScrCombo.Visibility = SplashScrTog.IsOn.ToVisibility();
        }

        private void SplashScrCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var obj = sender as ComboBox;
            AppSettings.SaveSettings("SplashAnim", (obj.SelectedItem as ComboBoxItem).Content.ToString());
            AppSettings.SaveSettings("SplashAnimIndex", obj.SelectedIndex);
        }

        private void LoginReqTog_Toggled(object sender, RoutedEventArgs e)
        {
            AppSettings.SaveSettings("LoginTogReg", LoginReqTog.IsOn);
        }

        private async void BackupFoldBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = await PacManHelper.SetBackupFolder();
            if (result)
            {
                BackupFoldBox.Text = (await PacManHelper.GetBackupFolder()).Path;
            }
        }

        private async void StartupRstBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = await Helper.MessageBox("Are you sure you want to reset the Startup commands?", Helper.SoundHelper.Sound.Alert, "", "No", true, "Yes");
            if (result == 0)
            {
                FilesHelper.CopyFromAppRoot("\\BatchScripts\\Startup.bat", @"C:\Windows\System32\Startup.bat");
            }
        }

        private async void ResCapTog_Toggled(object sender, RoutedEventArgs e)
        {
            if (ResCapTog.IsOn && flag)
            {
                ResCapTog.IsEnabled = false;
                if (!await Helper.AskCapabilitiesPermission())
                {
                    ResCapTog.IsEnabled = true;
                    ResCapTog.IsOn = false;
                }
            }
        }

        private void SnapSoundTog_Toggled(object sender, RoutedEventArgs e)
        {
            AppSettings.SaveSettings("SnapSoundTog", SnapSoundTog.IsOn);
        }

        private void MenuTransitionTog_Toggled(object sender, RoutedEventArgs e)
        {
            AppSettings.SaveSettings("MenuTransition", MenuTransitionTog.IsOn);
        }
    }
}
