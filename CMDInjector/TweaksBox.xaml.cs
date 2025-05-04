using CMDInjectorHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System.UserProfile;
using Windows.UI;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace CMDInjector
{
    public sealed partial class TweakBox : Page
    {
        string[] Paths;
        string TileIcon;
        ObservableCollection<int> glyphUnicodes = new ObservableCollection<int>();
        ObservableCollection<PacManHelper.AppsDetails> Packages = new ObservableCollection<PacManHelper.AppsDetails>();
        bool buttonOnHold = false;
        bool flag = false;
        bool secondFlag = false;

        public TweakBox()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Init();

            if (AppSettings.LoadSettings("UnlockHidden", false))
            {
                if (Helper.build == 14393)
                {
                    UfpModeStack.Visible();
                }

                SecMgrStack.Visible();
                NDTKStack.Visible();

                AppSettings.SaveSettings("UnlockHidden", false);
            }
            else
            {
                UfpModeStack.Collapse();
                SecMgrStack.Collapse();
                NDTKStack.Collapse();
            }

            foreach (var name in Directory.EnumerateFiles($"{Globals.installedLocation.Path}\\Contents\\GlanceScreen\\lpmFonts_4.1.12.4"))
                FontFileBox.Items.Add(name.Replace("lpmFont_", "").Replace(".bin", ""));
        }

        private async void Init()
        {
            if (Helper.IsTelnetConnected() && HomeHelper.IsConnected())
            {
                AutoWallTog.IsEnabled = true;
                DisplayOrient.IsEnabled = true;
            }

            if (Helper.build >= 15063)
            {
                CUBtn.IsEnabled = false;
                InitializeFolders();

                if (Helper.build >= 15254)
                    FCUBtn.IsEnabled = false;
            }
            else
            {
                NavRoots.Collapse();

                if (Helper.build < 14393)
                {
                    DisplayOrient.IsEnabled = false;
                    GlanceTog.IsEnabled = false;
                    ZipFileBtn.IsEnabled = false;

                    if (Helper.build < 14320)
                    {
                        VolOptStack.Collapse();

                        if (Helper.build < 10570)
                        {
                            SearchOptStack.Collapse();

                            if (Helper.build < 10549)
                            {
                                LiveLockStack.Collapse();
                            }
                        }
                    }
                    else
                    {
                        AUBtn.IsEnabled = false;
                    }
                }
            }

            RestoreNDTKTog.IsOn =
                RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE,
                                    "SOFTWARE\\OEM\\Nokia\\NokiaSvcHost\\Plugins\\NsgExtA\\NdtkSvc", "Path",
                                    RegistryType.REG_SZ).ToLower() == "c:\\windows\\system32\\ndtksvc.dll";

            TipText.Visibility = AppSettings.LoadSettings("TipSettings", true).ToVisibility();

            if (AppSettings.StartWallSwitch)
            {
                StartWallTog.IsOn = true;
                WallSwitchExtraStack.Visible();
            }
            else
            {
                StartWallTog.IsOn = false;
                WallSwitchExtraStack.Collapse();
            }

            var libraryPath = (await TweakBoxHelper.GetWallpaperLibrary()).Path;

            StartWallLibPathBox.Text = libraryPath.Contains(Helper.installedLocation.Path) ?
                                            "CMDInjector:\\Assets\\Images\\Lockscreens\\Stripes" :
                                            libraryPath;

            StartWallTrigCombo.SelectedIndex = AppSettings.StartWallTrigger;
            StartWallInterCombo.SelectedIndex = AppSettings.StartWallInterval;
            
            string BurnInProtectionBlackReplacementColor = RegEdit.GetRegValue("SOFTWARE\\Microsoft\\Shell\\NavigationBar", "BurnInProtectionBlackReplacementColor", RegistryType.REG_DWORD);


            foreach (var color in typeof(Colors).GetRuntimeProperties().Where(color => !Globals.CursedColors.Contains(color.Name)))
            {
                #region
                var selectColor = new Rectangle { Width = 20, Height = 20, Margin = new Thickness(0, 0, 10, 0), Fill = new SolidColorBrush((Color)color.GetValue(null)) };
                var colorText = new TextBlock { Text = color.Name, VerticalAlignment = VerticalAlignment.Center };
                var colorStack = new StackPanel { Orientation = Orientation.Horizontal };
                colorStack.Children.Add(selectColor);
                colorStack.Children.Add(colorText);

                AccentColorCombo.Items.Add(new ComboBoxItem { Content = colorStack });
                #endregion

                #region
                selectColor = new Rectangle { Width = 20, Height = 20, Margin = new Thickness(0, 0, 10, 0), Fill = new SolidColorBrush((Color)color.GetValue(null)) };
                colorText = new TextBlock { Text = color.Name, VerticalAlignment = VerticalAlignment.Center };
                colorStack = new StackPanel { Orientation = Orientation.Horizontal };
                colorStack.Children.Add(selectColor);
                colorStack.Children.Add(colorText);

                AccentColorTwoCombo.Items.Add(new ComboBoxItem { Content = colorStack });
                #endregion

                #region
                selectColor = new Rectangle { Width = 20, Height = 20, Margin = new Thickness(0, 0, 10, 0), Fill = new SolidColorBrush((Color)color.GetValue(null)) };
                colorText = new TextBlock { Text = color.Name, VerticalAlignment = VerticalAlignment.Center };
                colorStack = new StackPanel { Orientation = Orientation.Horizontal };

                colorStack.Children.Add(selectColor);
                colorStack.Children.Add(colorText);

                var cbi = new ComboBoxItem { Content = colorStack };
                ColorPickCombo.Items.Add(cbi);

                SolidColorBrush solidColor = (SolidColorBrush)selectColor.Fill;

                if (solidColor.Color.ToString().Remove(0, 3).ToInt32(16) == BurnInProtectionBlackReplacementColor.ToUInt32())
                {
                    ColorPickCombo.SelectedItem = cbi;
                }
                #endregion
            }

            #region
            IEnumerable<Package> allPackages = null;
            await Task.Run(() => { allPackages = new PackageManager().FindPackagesForUserWithPackageTypes("", PackageTypes.Main); });
            SearchAppLoadingProg.Maximum = allPackages.Count();

            IProgress<double> progress = new Progress<double>(value =>
            {
                SearchAppLoadingProg.Value += value;
                SearchAppLoadingText.Text = $"Loading... {Math.Round(100 * (SearchAppLoadingProg.Value / SearchAppLoadingProg.Maximum))}%";
            });

            var pressFound = false;
            var holdFound = false;

            await Task.Run(async () => { Packages = await PacManHelper.GetPackagesByType(PackageTypes.Main, false, progress); });

            var press = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\Input\\WEH\\Buttons\\WEHButton4\\Press", "AppID", RegistryType.REG_SZ);
            var hold = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\Input\\WEH\\Buttons\\WEHButton4\\PressAndHold", "AppID", RegistryType.REG_SZ);

            foreach (var Package in Packages)
            {
                SearchPressAppsCombo.Items.Add(Package.DisplayName);
                SearchHoldAppsCombo.Items.Add(Package.DisplayName);

                try
                {
                    var manifest = await Package.InstalledLocation.GetFileAsync("AppxManifest.xml");
                    var tags = XElement.Load(manifest.Path).Elements().Where(i => i.Name.LocalName == "PhoneIdentity");
                    var attributes = tags.Attributes().Where(i => i.Name.LocalName == "PhoneProductId");

                    try
                    {
                        if (pressFound == false)
                        {
                            if (!press.HasContent())
                            {
                                pressFound = true;
                                SearchPressAppsCombo.SelectedIndex = 1;
                            }
                            else if (press == "{None}")
                            {
                                SearchPressAppsCombo.SelectedIndex = 2;
                                pressFound = true;
                            }
                            else if (press == $"{{{attributes.First().Value}}}")
                            {
                                SearchPressAppsCombo.SelectedIndex = SearchPressAppsCombo.Items.Count - 1;
                                pressFound = true;
                            }
                            else
                            {
                                SearchPressAppsCombo.SelectedIndex = 0;
                            }

                            SearchPressParaCombo.Visible(SearchPressAppsCombo.SelectedItem.ToString() == "CMD Injector");
                        }

                        if (holdFound == false)
                        {
                            if (!hold.HasContent())
                            {
                                SearchHoldAppsCombo.SelectedIndex = 1;
                                holdFound = true;
                            }
                            else if (hold == "{None}")
                            {
                                SearchHoldAppsCombo.SelectedIndex = 2;
                                holdFound = true;
                            }
                            else if (hold == $"{{{attributes.First().Value}}}")
                            {
                                SearchHoldAppsCombo.SelectedIndex = SearchHoldAppsCombo.Items.Count - 1;
                                holdFound = true;
                            }
                            else
                            {
                                SearchHoldAppsCombo.SelectedIndex = 0;
                            }

                            SearchHoldParaCombo.Visible(SearchHoldAppsCombo.SelectedItem.ToString() == "CMD Injector");
                        }
                    }
                    catch (Exception ex)
                    {
                        //Helper.ThrowException(ex);
                    }
                }
                catch (Exception ex)
                {
                    //Helper.ThrowException(ex);
                }
            }

            SearchPressAppsCombo.IsEnabled = true;
            SearchPressParaCombo.IsEnabled = true;
            SearchHoldAppsCombo.IsEnabled = true;
            SearchHoldParaCombo.IsEnabled = true;
            SearchAppLoadStack.Collapse();
            #endregion

            #region Set selected choices
            var pressParam = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\Input\\WEH\\Buttons\\WEHButton4\\Press", "AppParam", RegistryType.REG_SZ);
            var HoldParam = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\Input\\WEH\\Buttons\\WEHButton4\\PressAndHold", "AppParam", RegistryType.REG_SZ);

            if (!pressParam.HasContent())
                SearchPressParaCombo.SelectedIndex = 1;

            // According to the old code:
            // There are VolUp, VolDown, VolMute params.
            else if (pressParam.StartsWith("Vol"))
            {
                if (pressParam == "VolMute")
                    SearchPressParaCombo.SelectedItem = "Volume Mute/Unmute";
                else
                    SearchPressParaCombo.SelectedItem = pressParam.Replace("Vol", "Volume ");
            }

            else if (pressParam == "FFULoader")
            {
                SearchPressParaCombo.SelectedItem = "FFU Loader";
            }

            // CMD Injector pages
            else if (pressParam.EndsWith("Page"))
            {
                SearchPressParaCombo.SelectedItem = pressParam.Replace("Page", "");
            }

            else
            {
                SearchPressParaCombo.SelectedItem = pressParam;
            }

            // The same with pressParam.
            if (HoldParam == "") SearchHoldParaCombo.SelectedIndex = 1;

            else if (HoldParam.StartsWith("Vol"))
            {
                if (HoldParam == "VolMute")
                    SearchHoldParaCombo.SelectedItem = "Volume Mute/Unmute";
                else
                    SearchHoldParaCombo.SelectedItem = HoldParam.Replace("Vol", "Volume ");
            }

            else if (HoldParam == "FFULoader")
            {
                SearchHoldParaCombo.SelectedItem = "FFU Loader";
            }

            // CMD Injector pages
            else if (HoldParam.EndsWith("Page"))
            {
                SearchHoldParaCombo.SelectedItem = HoldParam.Replace("Page", "");
            }

            else
            {
                SearchHoldParaCombo.SelectedItem = HoldParam;
            }
            #endregion
            secondFlag = true;

            try
            {
                DisplayOrient.SelectedIndex = AppSettings.LoadSettings("OrientSet", 0);

                BrightTog.IsOn = RegEdit.GetRegValue(
                        RegistryHive.HKEY_LOCAL_MACHINE,
                        "SOFTWARE\\OEM\\NOKIA\\Display\\ColorAndLight",
                        "UserSettingNoBrightnessSettings",
                        RegistryType.REG_DWORD
                    ).HasContent();
                
                BackgModeCombo.SelectedIndex = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\Theme",
                    "CurrentTheme",
                    RegistryType.REG_DWORD
                )
                        .IsDWORDStrFullZeros()
                        .ToInt();
                AutoBackgCombo.SelectedIndex = AppSettings.AutoThemeMode.ToInt();

                var autoLightSplits = AppSettings.AutoThemeLight.Split(':');
                var autoDarkSplits = AppSettings.AutoThemeDark.Split(':');

                BackgStartTime.Time = new TimeSpan(autoLightSplits[0].ToInt32(), autoLightSplits[1].ToInt32(), 00);
                BackgStopTime.Time = new TimeSpan(autoDarkSplits[0].ToInt32(), autoDarkSplits[1].ToInt32(), 00);

                UptTog.IsOn = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "Software\\Microsoft\\Windows\\CurrentVersion\\DeviceUpdate\\Agent\\Settings",
                    "GuidOfCategoryToScan",
                    RegistryType.REG_SZ
                ) != "F1E8E1CD-9819-4AC5-B0A7-2AFF3D29B46E";

                RegEdit.GoToKey(RegistryHive.HKEY_LOCAL_MACHINE, "SOFTWARE\\OEM\\Nokia\\lpm");
                var fontFile = RegEdit.GetRegValue("FontFile", RegistryType.REG_SZ);

                GlanceTog.IsOn = File.Exists("C:\\Data\\SharedData\\OEM\\Public\\NsgGlance_NlpmService_4.1.12.4.dll") &&
                                 fontFile.HasContent() && (await Helper.IsCapabilitiesAllowed()) &&
                                 RegEdit.GetRegValue("Enabled", RegistryType.REG_DWORD).HasContent();

                // Selected font kind/file
                int i = 0;
                foreach (var name in Directory.GetFiles($"{Globals.installedLocation.Path}\\Contents\\GlanceScreen\\lpmFonts_4.1.12.4"))
                {
                    if (fontFile == name)
                    {
                        FontFileBox.SelectedIndex = ++i;
                        break;
                    }
                }

                uint ClockAndIndicatorsCustomColor = RegEdit.GetRegValue(
                    "ClockAndIndicatorsCustomColor",
                    RegistryType.REG_DWORD
                ).ToUInt32(16);

                if (ClockAndIndicatorsCustomColor != 0)
                {
                    FontColorTog.IsOn = true;
                    RedRadio.IsChecked = false;
                    CyanRadio.IsChecked = false;
                    GreenRadio.IsChecked = false;
                    MagentaRadio.IsChecked = false;
                    BlueRadio.IsChecked = false;
                    YellowRadio.IsChecked = false;

                    // Consider using DWORD instead
                    switch (ClockAndIndicatorsCustomColor)
                    {
                        case 16711680: RedRadio.IsChecked = true; break;
                        case 65280: GreenRadio.IsChecked = true; break;
                        case 255: BlueRadio.IsChecked = true; break;
                        case 65535: CyanRadio.IsChecked = true; break;
                        case 16711935: MagentaRadio.IsChecked = true; break;
                        case 16776960: YellowRadio.IsChecked = true; break;
                    }
                    GlanceColorStack.Visible();
                }
                else
                {
                    FontColorTog.IsOn = false;
                    GlanceColorStack.Collapse();
                }

                GlanceAutoColor.SelectedIndex = AppSettings.GlanceAutoColorEnabled.ToInt();
                MoveClockTog.IsOn = RegEdit.GetRegValue("MoveClock", RegistryType.REG_DWORD).DWORDFlagToBool();

                #region Automatic wallpaper change
                AutoWallTog.IsOn = "LiveLockscreen.bat".IsAFileIn(Helper.localFolder.Path);

                // TODO: File write extension
                if (!"Lockscreen.dat".IsAFileIn(Helper.localFolder.Path))
                {
                    WallCollectionCombo.SelectedIndex = 0;
                    await FileIO.WriteTextAsync(
                        await Helper.localFolder.CreateFileAsync("Lockscreen.dat"),
                        $"{Helper.installedLocation.Path}\\Assets\\Images\\Lockscreens\\RedMoon\n65\nTrue\n00:00\n00:00\nTrue"
                    );
                }

                var datFile = await Helper.localFolder.GetFileAsync("Lockscreen.dat");
                // FIXME: will .Split() split lines?
                var LockscreenData = "Lockscreen.dat".ReadFromDir(Helper.localFolder.Path).Split('\n');

                if (LockscreenData.Count() <= 2)
                {
                    await FileIO.WriteTextAsync(datFile, $"{LockscreenData[0]}\n{LockscreenData[1]}\nTrue\n00:00\n00:00\nTrue");
                }
                else if (File.ReadLines((await Helper.localFolder.GetFileAsync("Lockscreen.dat")).Path).Count() <= 6)
                {
                    await FileIO.WriteTextAsync(datFile, string.Join("\n", Enumerable.Take(LockscreenData, 5)) + "\nTrue");
                }

                if (LockscreenData[0].StartsWith(Helper.installedLocation.Path))
                {
                    WallCollectionBox.Text = $"CMDInjector:\\Assets\\Images\\Lockscreens\\{System.IO.Path.GetFileName(LockscreenData[0])}";

                    if (LockscreenData[0].Contains("RedMoon"))
                    {
                        WallCollectionCombo.SelectedIndex = 0;
                    }
                    else if (LockscreenData[0].Contains("Flowers"))
                    {
                        WallCollectionCombo.SelectedIndex = 1;
                    }
                    else if (LockscreenData[0].Contains("Timelapse"))
                    {
                        WallCollectionCombo.SelectedIndex = 2;
                    }
                }
                else
                {
                    if (LockscreenData[0].Contains(Helper.localFolder.Path))
                        WallCollectionBox.Text = $"CMDInjector:\\Library\\{System.IO.Path.GetFileName(LockscreenData[0])}";
                    else
                        WallCollectionBox.Text = LockscreenData[0];

                    WallCollectionStack.Visible();
                    WallCollectionCombo.SelectedIndex = 3;
                }

                WallIntervalBox.Text = LockscreenData[1];
                WallRevLoopTog.IsOn = LockscreenData[2] == "True";

                if (AppSettings.LoadSettings("ActiveHours", true) && LockscreenData[3] != LockscreenData[4])
                {
                    ActiveHoursTog.IsOn = true;
                    ActiveHoursStack.Visible();
                }
                else
                {
                    ActiveHoursTog.IsOn = false;
                    ActiveHoursStack.Collapse();
                }

                if (flag == false)
                {
                    await Task.Delay(200);

                    // FIXME: Use TimeSpan.Parse
                    string[] startTimes = LockscreenData[3].Split(':');
                    string[] stopTimes  = LockscreenData[4].Split(':');

                    StartTimePkr.Time = new TimeSpan(startTimes[0].ToInt32(), startTimes[1].ToInt32(), 0);
                    StopTimePkr.Time  = new TimeSpan(stopTimes[0].ToInt32(), stopTimes[1].ToInt32(), 0);
                }

                WallDisBatSavTog.IsOn = LockscreenData[5] == "True";
                #endregion

                #region Boot up & Shutdown stuff
                // Boot animations
                RegEdit.GoToKey(RegistryHive.HKEY_LOCAL_MACHINE, "BCD00000001\\Objects\\{7ea2e1ac-2e61-4728-aaa3-896d9d0a9f0e}\\Elements\\");
                BootAnimTog.IsOn = RegEdit.GetRegValue("16000069", "Element", RegistryType.REG_BINARY).Contains("01") &&
                                   RegEdit.GetRegValue("1600007a", "Element", RegistryType.REG_BINARY).Contains("01");

                // Boot up & Shutdown images
                RegEdit.GoToKey(RegistryHive.HKEY_LOCAL_MACHINE, "System\\Shell\\OEM\\bootscreens");
                BootImageTog.IsOn = RegEdit.GetRegValue("wpbootscreenoverride", RegistryType.REG_SZ).HasContent();
                ShutdownImageTog.IsOn = RegEdit.GetRegValue("wpshutdownscreenoverride", RegistryType.REG_SZ).HasContent();
                #endregion

                #region Navigation bar
                RegEdit.GoToKey(RegistryHive.HKEY_LOCAL_MACHINE, "SOFTWARE\\Microsoft\\Shell\\NavigationBar");

                SoftNavTog.IsOn       = RegEdit.GetRegValue("SoftwareModeEnabled", RegistryType.REG_DWORD).DWORDFlagToBool();
                DoubleTapTog.IsOn     = RegEdit.GetRegValue("IsDoubleTapOffEnabled", RegistryType.REG_DWORD).DWORDFlagToBool();
                AutoHideTog.IsOn      = RegEdit.GetRegValue("IsAutoHideEnabled", RegistryType.REG_DWORD).DWORDFlagToBool();
                SwipeUpTog.IsOn       = RegEdit.GetRegValue("IsSwipeUpToHideEnabled", RegistryType.REG_DWORD).DWORDFlagToBool();
                UserManagedTog.IsOn   = RegEdit.GetRegValue("IsUserManaged", RegistryType.REG_DWORD).DWORDFlagToBool();
                BurninProtTog.IsOn    = RegEdit.GetRegValue("IsBurnInProtectionEnabled", RegistryType.REG_DWORD).DWORDFlagToBool();
                BurninTimeoutBox.Text = RegEdit.GetRegValue("BurnInProtectionIdleTimerTimeout", RegistryType.REG_DWORD).ToUInt32(16).ToString();
                OpacitySlide.Value    = RegEdit.GetRegValue("BurnInProtectionIconsOpacity", RegistryType.REG_DWORD).ToUInt32(16);
                #endregion

                RegEdit.GoToKey(RegistryHive.HKEY_LOCAL_MACHINE, "SOFTWARE\\Microsoft\\Shell\\Start");
                switch (RegEdit.GetRegValue("TileColumnSize", RegistryType.REG_DWORD))
                {
                    case "00000004":
                        TileCombo.SelectedIndex = 0;
                        break;
                    case "00000006":
                        TileCombo.SelectedIndex = 1;
                        break;
                    case "00000008":
                        TileCombo.SelectedIndex = 2;
                        break;
                    case "0000000a":
                        TileCombo.SelectedIndex = 3;
                        break;
                    case "0000000c":
                        TileCombo.SelectedIndex = 4;
                        break;
                }

                DngTog.IsOn = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "SOFTWARE\\OEM\\NOKIA\\Camera\\Barc", "DNGDisabled", RegistryType.REG_DWORD).IsDWORDStrFullZeros();

                VirtualMemBox.Text = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "System\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "PagingFiles", RegistryType.REG_MULTI_SZ);

                RegEdit.GoToKey(RegistryHive.HKEY_LOCAL_MACHINE, "SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps");

                int dumpType = Convert.ToInt32(RegEdit.GetRegValue("DumpValue", RegistryType.REG_DWORD).ToUInt32(16));
                DumpTypeCombo.SelectedIndex = ((dumpType > 2) || (dumpType < 0)) ? 0 : dumpType;

                string dumpCount = RegEdit.GetRegValue("DumpCount", RegistryType.REG_DWORD);
                if (dumpCount.HasContent())
                {
                    int converted = Convert.ToInt32(dumpCount.ToUInt32(16) - 1);
                    DumpCountCombo.SelectedIndex = ((converted >= 10) || (converted < 0)) ? 9 : converted;
                }
                else
                    DumpCountCombo.SelectedIndex = 0;
                
                DumpFolderBox.Text = RegEdit.GetRegValue("DumpFolder", RegistryType.REG_MULTI_SZ);

                flag = true;
            }
            catch (Exception ex)
            {
                //Helper.ThrowException(ex);
            }
        }

        private void InitializeFolders()
        {
            MenuFlyoutItem CustomPath = new MenuFlyoutItem();
            CustomPath.Click += Items_Click;
            CustomPath.Text = "Custom Location";
            AddFlyMenu.Items.Add(CustomPath);

            var navRoots = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Windows\\CurrentVersion\\FileExplorer\\Config", "NavigationRoots", RegistryType.REG_SZ);

            if (!navRoots.Contains("shell:::{679f85cb-0220-4080-b29b-5540cc05aab6}"))
            {
                MenuFlyoutItem Recent = new MenuFlyoutItem();
                Recent.Click += Items_Click;
                Recent.Text = "Recent";
                AddFlyMenu.Items.Add(Recent);
            }

            if (!navRoots.Contains("knownfolder:{1C2AC1DC-4358-4B6C-9733-AF21156576F0}"))
            {
                MenuFlyoutItem ThisDevice = new MenuFlyoutItem();
                ThisDevice.Click += Items_Click;
                ThisDevice.Text = "This Device";
                AddFlyMenu.Items.Add(ThisDevice);
            }
            if (!navRoots.Contains("::{20D04FE0-3AEA-1069-A2D8-08002B30309D}"))
            {
                MenuFlyoutItem ThisPC = new MenuFlyoutItem();
                ThisPC.Click += Items_Click;
                ThisPC.Text = "This PC";
                AddFlyMenu.Items.Add(ThisPC);
            }

            string[] NavigationRoots = navRoots.Split(';');
            if (NavigationRoots.Length <= 1)
            {
                RemoveBtn.IsEnabled = false;
                MoveUpBtn.IsEnabled = false;
            }
            else
            {
                RemoveBtn.IsEnabled = true;
                MoveUpBtn.IsEnabled = true;
            }

            Paths = new string[NavigationRoots.Length];
            for (int i = 0; i < NavigationRoots.Length; i++)
            {
                Paths[i] = NavigationRoots[i];
                NavigationRoots[i] = places.First(obj => obj.Value.Equals(NavigationRoots[i], StringComparison.CurrentCultureIgnoreCase)).Key;
                FolderBox.Items.Add(NavigationRoots[i]);
                RootOrderList.Items.Add(NavigationRoots[i]);
                FolderPathCombo.Items.Add(NavigationRoots[i]);
                if (RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Windows\\CurrentVersion\\FileExplorer\\Config", "DefaultFolder", RegistryType.REG_SZ) == Paths[i]) FolderBox.SelectedIndex = i;
            }
            if (FolderBox.SelectedIndex == -1)
            {
                try
                {
                    string DefaultPath = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Windows\\CurrentVersion\\FileExplorer\\Config", "DefaultFolder", RegistryType.REG_SZ);
                    DefaultPath = places.First(obj => obj.Value.Equals(DefaultPath, StringComparison.CurrentCultureIgnoreCase)).Key;
                    FolderBox.Items.Add(DefaultPath);
                    FolderBox.SelectedIndex = FolderBox.Items.Count - 1;
                }
                catch (Exception ex) { }
            }
            InstalledFont ifg = new InstalledFont();
            var Glyphs = ifg.GetCharacters("Segoe MDL2 Assets");
            foreach (var Glyph in Glyphs)
            {
                glyphUnicodes.Add(Glyph.UnicodeIndex);
                FolderIconCombo.Items.Add(Glyph.Char);
            }
            if (FolderIconCombo.SelectedIndex == -1) FolderPathCombo.SelectedIndex = 0;
        }

        private void FolderBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RegEdit.SetHKLMValue("Software\\Microsoft\\Windows\\CurrentVersion\\FileExplorer\\Config", "DefaultFolder", RegistryType.REG_SZ, Paths[FolderBox.SelectedIndex]);
        }

        private void Items_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem clickedItem = (MenuFlyoutItem)sender;
            if (clickedItem.Text == "Custom Location")
            {
                CustomLocation();
                return;
            }
            else if (clickedItem.Text == "This PC")
            {
                RegEdit.SetHKLMValue("Software\\Microsoft\\Windows\\CurrentVersion\\FileExplorer\\Config\\FolderIconCharacters", "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}", RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse("57873")));
            }
            RootOrderList.Items.Add(clickedItem.Text);
            AddFlyMenu.Items.Remove(clickedItem);
            SaveLocation();
        }

        private async void CustomLocation()
        {
            ContentDialog addPath = new ContentDialog
            {
                Title = "Select Option",
                IsSecondaryButtonEnabled = true,
                PrimaryButtonText = "Browse Location",
                SecondaryButtonText = "Enter Location"
            };
            Helper.SoundHelper.PlaySound(Helper.SoundHelper.Sound.Alert);
            var result = await addPath.ShowAsync();
            if (result == ContentDialogResult.None)
            {
                return;
            }
            else if (result == ContentDialogResult.Primary)
            {
                var folderPicker = new FolderPicker
                {
                    SuggestedStartLocation = PickerLocationId.ComputerFolder
                };
                folderPicker.FileTypeFilter.Add("*");
                StorageFolder CustomFolder = await folderPicker.PickSingleFolderAsync();
                if (CustomFolder != null)
                {
                    RegEdit.SetHKLMValue("Software\\Microsoft\\Windows\\CurrentVersion\\FileExplorer\\Config\\FolderIconCharacters", CustomFolder.Path, RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse("60737")));
                    string value;
                    if (CustomFolder.Path.Equals("C:\\", StringComparison.CurrentCultureIgnoreCase)) value = "MainOS (C:)";
                    else if (CustomFolder.Path.Equals("U:\\", StringComparison.CurrentCultureIgnoreCase)) value = "Data (U:)";
                    else if (CustomFolder.Path.Equals("D:\\", StringComparison.CurrentCultureIgnoreCase)) value = "SD Card (D:)";
                    else
                    {
                        value = places.First(obj => CustomFolder.Path.Equals(obj.Value, StringComparison.CurrentCultureIgnoreCase)).Key;
                        if (!value.HasContent())
                            value = CustomFolder.Path;
                    }
                    RootOrderList.Items.Add(value);
                }
            }
            else
            {
                TextBox inputTextBox = new TextBox
                {
                    AcceptsReturn = false,
                    IsSpellCheckEnabled = false,
                    IsTextPredictionEnabled = false,
                    PlaceholderText = "C:\\Data\\Users\\Public\\Downloads"
                };
                ContentDialog customPath = new ContentDialog
                {
                    Title = "Add Location",
                    IsSecondaryButtonEnabled = true,
                    PrimaryButtonText = "Add",
                    SecondaryButtonText = "Cancel",
                    Content = inputTextBox
                };
                //try
                //{
                    Helper.SoundHelper.PlaySound(Helper.SoundHelper.Sound.Alert);
                    if (await customPath.ShowAsync() == ContentDialogResult.Primary)
                    {
                        if (Directory.Exists(inputTextBox.Text) && inputTextBox.Text.HasContent())
                        {
                            if (inputTextBox.Text[inputTextBox.Text.Length - 1].ToString() == "\\")
                            {
                                inputTextBox.Text = inputTextBox.Text.Remove(inputTextBox.Text.Length - 1);
                            }

                            RegEdit.SetHKLMValue("Software\\Microsoft\\Windows\\CurrentVersion\\FileExplorer\\Config\\FolderIconCharacters", inputTextBox.Text, RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse("60737")));
                            inputTextBox.Text = places.First(obj => inputTextBox.Text.Equals(obj.Value, StringComparison.CurrentCultureIgnoreCase)).Key;
                            RootOrderList.Items.Add(inputTextBox.Text);
                        }
                        else
                        {
                            await Helper.MessageBox("The entered location doesn't exist.", Helper.SoundHelper.Sound.Error, "Missing Location");
                        }
                        inputTextBox.Text = string.Empty;
                    }
                //}
                //catch (Exception ex)
                //{
                //    if (inputTextBox.Text[inputTextBox.Text.Length - 1].ToString() == "\\")
                //    {
                //        inputTextBox.Text = inputTextBox.Text.Remove(inputTextBox.Text.Length - 1);
                //    }
                //    RegEdit.SetHKLMValue("Software\\Microsoft\\Windows\\CurrentVersion\\FileExplorer\\Config\\FolderIconCharacters", inputTextBox.Text, RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse("60737")));
                //    if (inputTextBox.Text.Equals("C:", StringComparison.CurrentCultureIgnoreCase)) inputTextBox.Text = "MainOS (C:)";
                //    else if (inputTextBox.Text.Equals("U:", StringComparison.CurrentCultureIgnoreCase)) inputTextBox.Text = "Data (U:)";
                //    else if (inputTextBox.Text.Equals("D:", StringComparison.CurrentCultureIgnoreCase)) inputTextBox.Text = "SD Card (D:)";
                //    RootOrderList.Items.Add(inputTextBox.Text);
                //}
            }
            SaveLocation();
        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RootOrderList.SelectedIndex == -1)
            {
                return;
            }
            RootOrderList.Items.Remove(RootOrderList.SelectedItem);
            SaveLocation();
        }

        private void MoveUpBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RootOrderList.SelectedIndex == 0 || RootOrderList.SelectedIndex == -1)
            {
                return;
            }
            int newIndex = RootOrderList.SelectedIndex - 1;
            object selectedIndex = RootOrderList.SelectedItem;
            RootOrderList.Items.Remove(selectedIndex);
            RootOrderList.Items.Insert(newIndex, selectedIndex);
            RootOrderList.SelectedIndex = newIndex;
            SaveLocation();
        }

        private void SaveLocation()
        {
            try
            {
                string folderRoots = string.Empty;
                for (int i = 0; i < RootOrderList.Items.Count; i++)
                {
                    if (RootOrderList.Items[i].ToString().Equals("Recent", StringComparison.CurrentCultureIgnoreCase)) RootOrderList.Items[i] = "shell:::{679f85cb-0220-4080-b29b-5540cc05aab6}";
                    else if (RootOrderList.Items[i].ToString().Equals("This Device", StringComparison.CurrentCultureIgnoreCase)) RootOrderList.Items[i] = "knownfolder:{1C2AC1DC-4358-4B6C-9733-AF21156576F0}";
                    else if (RootOrderList.Items[i].ToString().Equals("This PC", StringComparison.CurrentCultureIgnoreCase)) RootOrderList.Items[i] = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                    else if (RootOrderList.Items[i].ToString().Equals("MainOS (C:)", StringComparison.CurrentCultureIgnoreCase)) RootOrderList.Items[i] = "C:";
                    else if (RootOrderList.Items[i].ToString().Equals("Data (U:)", StringComparison.CurrentCultureIgnoreCase)) RootOrderList.Items[i] = "U:";
                    else if (RootOrderList.Items[i].ToString().Equals("SD Card (D:)", StringComparison.CurrentCultureIgnoreCase)) RootOrderList.Items[i] = "D:";
                    folderRoots += RootOrderList.Items[i] + ";";
                }
                RegEdit.SetHKLMValue("Software\\Microsoft\\Windows\\CurrentVersion\\FileExplorer\\Config", "NavigationRoots", RegistryType.REG_SZ, folderRoots.Remove(folderRoots.Length - 1, 1));
                FolderBox.Items.Clear();
                RootOrderList.Items.Clear();
                FolderPathCombo.Items.Clear();
                FolderIconCombo.Items.Clear();
                InitializeFolders();
            }
            catch (Exception ex)
            {
                if (ex.Message == "Index was outside the bounds of the array." || ex.Message == "Arg_IndexOutOfRangeException")
                {
                    AddFlyMenu.Items.Clear();
                    RootOrderList.Items.Clear();
                    FolderPathCombo.Items.Clear();
                    FolderIconCombo.Items.Clear();
                    InitializeFolders();
                }
                else
                {
                    Helper.ThrowException(ex);
                }
            }
        }

        /// <summary>
        /// Keys are names, values are actual paths.
        /// </summary>
        private Dictionary<string, string> places = new Dictionary<string, string>
        {
            ["MainOS (C:)"] = "C:",
            ["Data (U:)"] = "U:",
            ["SDCard (D:)"] = "D:",
            ["This Device"] = "::{5b934b42-522b-4c34-bbfe-37a3ef7b9c90}",
            ["This PC"] = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}",
            ["Recent"] = "::{679f85cb-0220-4080-b29b-5540cc05aab6}"
        };

        private void FolderPathCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FolderPathCombo.SelectedIndex != -1)
            {
                string SelFoldUri = places.First(obj => obj.Key.Equals(FolderPathCombo.SelectedItem as string, StringComparison.CurrentCultureIgnoreCase)).Value;

                var folderIconChars = Convert.ToInt32(
                    RegEdit.GetRegValue(
                        RegistryHive.HKEY_LOCAL_MACHINE,
                        "Software\\Microsoft\\Windows\\CurrentVersion\\FileExplorer\\Config\\FolderIconCharacters",
                        SelFoldUri,
                        RegistryType.REG_DWORD
                     ),
                    16
                );
                int first = glyphUnicodes.First(obj => obj.Equals(folderIconChars));

                FolderIconCombo.SelectedIndex = glyphUnicodes.IndexOf(first);
            }
        }

        private void FolderIconCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FolderIconCombo.SelectedIndex != -1)
            {
                string SelFoldUri = places.First(obj => obj.Key.Equals(FolderPathCombo.SelectedItem.ToString(), StringComparison.CurrentCultureIgnoreCase)).Value;
                RegEdit.SetHKLMValue(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\FileExplorer\\Config\\FolderIconCharacters",
                    SelFoldUri, RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse(glyphUnicodes[FolderIconCombo.SelectedIndex].ToString()))
                );
            }
        }

        private async void PinFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder PinTile = await folderPicker.PickSingleFolderAsync();
            StorageFolder nd = PinTile;
            if (PinTile != null)
            {
                TextBox displayName = new TextBox
                {
                    AcceptsReturn = false,
                    IsSpellCheckEnabled = false,
                    IsTextPredictionEnabled = false,
                    PlaceholderText = "Downloads"
                };

                ContentDialog pinTextBox = new ContentDialog
                {
                    Title = "Display Name",
                    IsSecondaryButtonEnabled = true,
                    PrimaryButtonText = "OK",
                    SecondaryButtonText = "Cancel",
                    Content = displayName
                };

                if (await pinTextBox.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        if (displayName.Text.HasContent())
                        {
                            StorageApplicationPermissions.FutureAccessList.AddOrReplace(displayName.Text, PinTile);
                            string upperedName = displayName.ToString().ToUpper();

                            if (upperedName.Contains("DOCUMENT"))
                                TileIcon = "ms-appx:///Assets/Icons/Folders/DocumentsFolderTileLogo.png";

                            else if (upperedName.Contains("DOWNLOAD"))
                                TileIcon = "ms-appx:///Assets/Icons/Folders/DownloadsFolderTileLogo.png";

                            else if (upperedName.Contains("FAVORITE"))
                                TileIcon = "ms-appx:///Assets/Icons/Folders/FavoritesFolderTileLogo.png";

                            else if (upperedName.Contains("GAME") || upperedName.Contains("APP"))
                                TileIcon = "ms-appx:///Assets/Icons/Folders/GamesFolderTileLogo.png";

                            else if (upperedName.Contains("MUSIC") || upperedName.Contains("SONG"))
                                TileIcon = "ms-appx:///Assets/Icons/Folders/MusicFolderTileLogo.png";

                            else if (upperedName.Contains("PICTURE") || upperedName.Contains("IMAGE") || upperedName.Contains("PHOTO"))
                                TileIcon = "ms-appx:///Assets/Icons/Folders/PicturesFolderTileLogo.png";

                            else if (upperedName.Contains("USER") || upperedName.Contains("MY "))
                                TileIcon = "ms-appx:///Assets/Icons/Folders/UserFolderTileLogo.png";

                            else if (upperedName.Contains("VIDEO") || upperedName.Contains("MOVIE") || upperedName.Contains("FILM"))
                                TileIcon = "ms-appx:///Assets/Icons/Folders/VideosFolderTileLogo.png";
                            else
                                TileIcon = "ms-appx:///Assets/Icons/Folders/ExplorerFolderTileLogo.png";

                            var FolderTile = new SecondaryTile(Regex.Replace(displayName.Text, "[^A-Za-z0-9]", ""), displayName.Text, displayName.Text, new Uri(TileIcon), TileSize.Default);

                            FolderTile.VisualElements.ShowNameOnSquare150x150Logo = true;
                            FolderTile.VisualElements.ShowNameOnWide310x150Logo = true;

                            await FolderTile.RequestCreateAsync();
                        }
                    }
                    catch (Exception ex) { Helper.ThrowException(ex); }

                }
            }
        }

        private void DisplayOrient_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flag == true)
            {
                if (Helper.IsTelnetConnected() && HomeHelper.IsConnected())
                {
                    AppSettings.SaveSettings("OrientSet", DisplayOrient.SelectedIndex);
                    Helper.Send($"setDisplayResolution.exe -Orientation:{(DisplayOrient.SelectedIndex == 0 ? 0 : 180)}");
                }
                else
                {
                    Helper.MessageBox(HomeHelper.GetTelnetTroubleshoot(), Helper.SoundHelper.Sound.Error, "Error");
                }
            }
        }

        private void BrightTog_Toggled(object sender, RoutedEventArgs e)
        {
            if (BrightTog.IsOn)
            {
                RegEdit.SetHKLMValue("SOFTWARE\\OEM\\NOKIA\\Display\\ColorAndLight", "UserSettingNoBrightnessSettings", RegistryType.REG_DWORD, BitConverter.GetBytes(1u));
            }
            else
            {
                if (Helper.IsTelnetConnected() && HomeHelper.IsConnected())
                {
                    RegEdit.DeleteRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "SOFTWARE\\OEM\\NOKIA\\Display\\ColorAndLight", "UserSettingNoBrightnessSettings");
                }
                else
                {
                    Helper.MessageBox(HomeHelper.GetTelnetTroubleshoot(), Helper.SoundHelper.Sound.Error, "Error");
                }
            }
            BrightTogIndicator.Visible(flag);
        }

        private void UptTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "Software\\Microsoft\\Windows\\CurrentVersion\\DeviceUpdate\\Agent\\Settings",
                "GuidOfCategoryToScan", RegistryType.REG_SZ,
                UptTog.IsOn ? "00000000-0000-0000-0000-000000000000" : "F1E8E1CD-9819-4AC5-B0A7-2AFF3D29B46E"
            );
        }

        private async void SystemUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Helper.IsTelnetConnected() && HomeHelper.IsConnected())
                {
                    await Helper.Send($"reg export HKLM\\System\\Platform\\DeviceTargetingInfo {Helper.localFolder.Path}\\DeviceTargetingInfo.reg");
                }
                RegEdit.GoToKey(RegistryHive.HKEY_LOCAL_MACHINE, "System\\Platform\\DeviceTargetingInfo");

                switch ((sender as Button).Content as string)
                {
                    case "Anniversary Update":
                        RegEdit.SetRegValue("PhoneManufacturer", RegistryType.REG_SZ, "NOKIA");
                        RegEdit.SetRegValue("PhoneManufacturerModelName", RegistryType.REG_SZ, "RM-1045_1083");
                        RegEdit.SetRegValue("PhoneModelName", RegistryType.REG_SZ, "Lumia 930");
                        break;

                    case "Creator Update":
                        RegEdit.SetRegValue("PhoneManufacturer", RegistryType.REG_SZ, "NOKIA");
                        RegEdit.SetRegValue("PhoneManufacturerModelName", RegistryType.REG_SZ, "RM-1096_1002");
                        RegEdit.SetRegValue("PhoneModelName", RegistryType.REG_SZ, "RM-1096");
                        break;

                    case "Fall Creator Update":
                        RegEdit.SetRegValue("PhoneManufacturer", RegistryType.REG_SZ, "MicrosoftMDG");
                        RegEdit.SetRegValue("PhoneManufacturerModelName", RegistryType.REG_SZ, "RM-1116_11258");
                        RegEdit.SetRegValue("PhoneModelName", RegistryType.REG_SZ, "Lumia 950 XL");
                        break;
                }
                await Helper.MessageBox("Now you can update your phone to your desired version. Make sure to have free storage for the update.", Helper.SoundHelper.Sound.Alert, "Success");
            }
            catch (Exception ex) { Helper.ThrowException(ex); }
        }

        private async void GlanceTog_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                if (flag && GlanceTog.IsOn)
                {
                    if (!await Helper.IsCapabilitiesAllowed() && !await Helper.AskCapabilitiesPermission())
                    {
                        GlanceTog.IsOn = false;
                        return;
                    }

                    string OEMPublicPath = "C:\\Data\\SharedData\\OEM\\Public";
                    Directory.CreateDirectory($"{OEMPublicPath}\\lpmFonts_4.1.12.4");

                    foreach (var name in Directory.GetFiles($"{Globals.installedLocation.Path}\\Contents\\GlanceScreen\\lpmFonts_4.1.12.4"))
                        FilesHelper.CopyFileToDir(name, $"{OEMPublicPath}\\lpmFonts_4.1.12.4");

                    FilesHelper.CopyFromAppRoot("Contents\\GlanceScreen\\NsgGlance_NlpmService_4.1.12.4.dll", OEMPublicPath);
                    FilesHelper.CopyFromAppRoot("Contents\\GlanceScreen\\NsgGlance_NlpmServiceImpl_4.1.12.4.dll", OEMPublicPath);

                    await Helper.Send($"reg import {Globals.installedLocation.Path}\\Contents\\GlanceScreen\\Enable.reg");
                   
                    FontFileBox.SelectedIndex = 3;
                    RestoreGlanceIndicator.Visible();
                }
                else
                {
                    GlanceTog.IsOn = true;
                }
            }
            catch (Exception ex) { Helper.ThrowException(ex); }
        }

        private void FontFileBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "SOFTWARE\\OEM\\Nokia\\lpm",
                "FontFile",
                RegistryType.REG_SZ,
                Directory.GetFiles(
                    $"{Globals.installedLocation.Path}\\Contents\\GlanceScreen\\lpmFonts_4.1.12.4"
                )[FontFileBox.SelectedIndex - 1]
            );
        }

        private void FontColorTog_Toggled(object sender, RoutedEventArgs e)
        {
            if (FontColorTog.IsOn)
            {
                RegEdit.SetHKLMValue("SOFTWARE\\OEM\\Nokia\\lpm", "ClockAndIndicatorsCustomColor", RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse("16711680")));
                RedRadio.IsChecked = true;
                GlanceColorStack.Visible();
            }
            else
            {
                RegEdit.SetHKLMValue("SOFTWARE\\OEM\\Nokia\\lpm", "ClockAndIndicatorsCustomColor", RegistryType.REG_DWORD, BitConverter.GetBytes(0u));
                GlanceAutoColor.SelectedIndex = 0;
                RedRadio.IsChecked = false;
                GreenRadio.IsChecked = false;
                BlueRadio.IsChecked = false;
                CyanRadio.IsChecked = false;
                MagentaRadio.IsChecked = false;
                YellowRadio.IsChecked = false;
                GlanceColorStack.Collapse();
            }
        }

        private void GlanceAutoColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flag)
                AppSettings.SaveSettings("GlanceAutoColorEnabled", GlanceAutoColor.SelectedIndex);
        }

        private void FontColor_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            uint valueToUse = 0u;

            switch ((sender as RadioButton).Content)
            {
                case "Red": valueToUse = uint.Parse("16711680"); break;
                case "Green": valueToUse = uint.Parse("65280"); break;
                case "Blue": valueToUse = uint.Parse("255"); break;
                case "Cyan": valueToUse = uint.Parse("65535"); break;
                case "Magenta": valueToUse = uint.Parse("16711935"); break;
                case "Yellow": valueToUse = uint.Parse("16776960"); break;
            }

            if (valueToUse != 0u)
                RegEdit.SetHKLMValue(
                    "SOFTWARE\\OEM\\Nokia\\lpm", "ClockAndIndicatorsCustomColor",
                    RegistryType.REG_DWORD, BitConverter.GetBytes(valueToUse)
                );
        }

        private void MoveClockTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue("SOFTWARE\\OEM\\Nokia\\lpm", "MoveClock", RegistryType.REG_DWORD, BitConverter.GetBytes(MoveClockTog.IsOn ? 1u : 0u));
        }

        private async void AutoWallTog_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AutoWallTog.IsOn)
                {
                    if (flag)
                    {
                        AppSettings.SaveSettings("BackupCurrentWall", RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Shell\\Wallpaper", "CurrentWallpaper", RegistryType.REG_SZ));
                        var currentLibrary = await StorageFolder.GetFolderFromPathAsync((await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[0]);
                        FilesHelper.CopyFile((await currentLibrary.GetFilesAsync())[0].Path, $"{Helper.localFolder.Path}\\{System.IO.Path.GetFileName((await currentLibrary.GetFilesAsync())[0].Path)}");
                        UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                        await profileSettings.TrySetLockScreenImageAsync(await Helper.localFolder.GetFileAsync($"{System.IO.Path.GetFileName((await currentLibrary.GetFilesAsync())[0].Path)}"));
                        Globals.nrpc.FileCopy(Helper.installedLocation.Path + "\\Contents\\BatchScripts\\LiveLockscreen.bat", Helper.localFolder.Path + "\\LiveLockscreen.bat", 0);
                        Helper.Send("start " + Helper.localFolder.Path + "\\LiveLockscreen.bat");
                    }
                }
                else
                {
                    if ("LiveLockscreen.bat".IsAFileIn(Helper.localFolder.Path))
                    {
                        File.Delete($"{Helper.localFolder.Path}\\LiveLockscreen.bat");
                    }

                    if (flag)
                    {
                        if (AppSettings.LoadSettings("BackupCurrentWall", null) != null)
                        {
                            FilesHelper.CopyFile(AppSettings.LoadSettings("BackupCurrentWall", null), $"{Helper.localFolder.Path}\\{System.IO.Path.GetFileName(AppSettings.LoadSettings("BackupCurrentWall", null))}");
                            UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                            await profileSettings.TrySetLockScreenImageAsync(await Helper.localFolder.GetFileAsync($"{System.IO.Path.GetFileName(AppSettings.LoadSettings("BackupCurrentWall", null))}"));
                            RegEdit.SetHKLMValue("Software\\Microsoft\\Shell\\Wallpaper", "CurrentWallpaper", RegistryType.REG_SZ, AppSettings.LoadSettings("BackupCurrentWall", null));
                        }
                    }
                }
            }
            catch (Exception ex) { }
        }

        private async void AutoWallCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (flag)
                {
                    var splits = (await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n');

                    if (WallCollectionCombo.SelectedItem.ToString().HasContent())
                    {
                        // WallCollectionCombo items:
                        // index 0 = Red Moon
                        // index 1 = Flowers
                        // index 2 = Timelapse
                        // index 3 = Custom

                        switch (WallCollectionCombo.SelectedIndex)
                        {
                            case 0:
                                WallCollectionStack.Collapse();
                                WallRevLoopTog.IsOn = true;
                                splits[1] = "65";
                                splits[0] = $"{Helper.installedLocation.Path}\\Assets\\Images\\Lockscreens\\RedMoon";
                                break;

                            case 1:
                                WallCollectionStack.Collapse();
                                WallRevLoopTog.IsOn = true;
                                splits[1] = "60";
                                splits[0] = $"{Helper.installedLocation.Path}\\Assets\\Images\\Lockscreens\\Flowers";
                                break;

                            case 2:
                                WallCollectionStack.Collapse();
                                WallRevLoopTog.IsOn = false;
                                splits[1] = "70";
                                splits[0] = $"{Helper.installedLocation.Path}\\Assets\\Images\\Lockscreens\\Timelapse";
                                break;

                            case 3:
                                WallCollectionStack.Visible();
                                WallRevLoopTog.IsOn = false;
                                splits[0] = $"{Helper.installedLocation.Path}\\Assets\\Images\\Lockscreens\\Stripes";
                                splits[1] = "15000";
                                break;
                        }

                        WallIntervalBox.Text = splits[1];
                        await FileIO.WriteTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"),
                            // first line
                            splits[0] + "\n" +

                            // second line
                            splits[1] + "\n" +

                            // third line
                            (WallCollectionCombo.SelectedIndex < 2).ToString() + "\n" + // index 0 or 1

                            // keep other lines
                            splits[3] + "\n" + splits[4] + "\n" + splits[5]
                        );
                    }

                    WallCollectionBox.Text =
                        splits[0].Contains(Helper.localFolder.Path) ?
                            $"CMDInjector:\\Library\\{System.IO.Path.GetFileName(splits[0])}" :
                                splits[0].Contains(Helper.installedLocation.Path) ?
                                    $"CMDInjector:\\Assets\\Images\\Lockscreens\\{System.IO.Path.GetFileName(splits[0])}" :
                                        splits[0];

                    if (AutoWallTog.IsOn)
                    {
                        var currentLibrary = await StorageFolder.GetFolderFromPathAsync(splits[0]);
                        FilesHelper.CopyFile((await currentLibrary.GetFilesAsync())[0].Path, $"{Helper.localFolder.Path}\\{System.IO.Path.GetFileName((await currentLibrary.GetFilesAsync())[0].Path)}");
                        UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                        await profileSettings.TrySetLockScreenImageAsync(await Helper.localFolder.GetFileAsync($"{System.IO.Path.GetFileName((await currentLibrary.GetFilesAsync())[0].Path)}"));
                        await Helper.localFolder.CreateFileAsync("LockscreenBreak.txt", CreationCollisionOption.OpenIfExists);
                    }
                }
            }
            catch (Exception ex) { }
        }

        private async void LibraryBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WallCollectionBtn.IsEnabled = false;
                MenuFlyoutItem clickedItem = (MenuFlyoutItem)sender;
                if (clickedItem.Text == "Zip File")
                {
                    var filePicker = new FileOpenPicker
                    {
                        SuggestedStartLocation = PickerLocationId.ComputerFolder
                    };
                    filePicker.FileTypeFilter.Add(".zip");
                    StorageFile zipFile = await filePicker.PickSingleFileAsync();
                    if (zipFile != null)
                    {
                        ZipFileProg.Visible();
                        ZipFileProg.IsIndeterminate = true;
                        await Task.Run(async () =>
                        {
                            try
                            {
                                StorageFolder libraryFolder = await Helper.localFolder.CreateFolderAsync("Library", CreationCollisionOption.OpenIfExists);
                                bool isCacheExist = false;
                                IReadOnlyList<StorageFolder> oldCaches = null;
                                if (!Directory.Exists($"{libraryFolder.Path}\\{System.IO.Path.GetFileNameWithoutExtension(zipFile.Path)}"))
                                {
                                    oldCaches = await libraryFolder.GetFoldersAsync();
                                    isCacheExist = true;
                                }
                                using (Stream zipStream = await zipFile.OpenStreamForReadAsync())
                                using (ZipArchive archive = new ZipArchive(zipStream))
                                {
                                    foreach (var entry in archive.Entries)
                                    {
                                        if (System.IO.Path.GetExtension(entry.FullName).ToLower() == ".jpeg" || System.IO.Path.GetExtension(entry.FullName).ToLower() == ".jpg" || System.IO.Path.GetExtension(entry.FullName).ToLower() == ".png")
                                        {
                                            await libraryFolder.CreateFolderAsync($"{System.IO.Path.GetFileNameWithoutExtension(zipFile.Path)}", CreationCollisionOption.OpenIfExists);
                                            entry.ExtractToFile($"{libraryFolder.Path}\\{System.IO.Path.GetFileNameWithoutExtension(zipFile.Path)}\\{entry.FullName}", true);
                                        }
                                    }
                                }
                                if (Directory.Exists($"{libraryFolder.Path}\\{System.IO.Path.GetFileNameWithoutExtension(zipFile.Path)}"))
                                {
                                    await FileIO.WriteTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"), $"{libraryFolder.Path}\\{System.IO.Path.GetFileNameWithoutExtension(zipFile.Path)}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[1]}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[2]}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[3]}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[4]}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[5]}");
                                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                    {
                                        WallCollectionBox.Text = $"CMDInjector:\\Library\\{System.IO.Path.GetFileNameWithoutExtension(zipFile.Path)}";
                                    });
                                    //archive.ExtractToDirectory($"{libraryFolder.Path}\\{System.IO.Path.GetFileNameWithoutExtension(zipFile.Path)}");
                                    await Helper.localFolder.CreateFileAsync("LockscreenBreak.txt", CreationCollisionOption.OpenIfExists);
                                    if (isCacheExist)
                                    {
                                        foreach (var oldCache in oldCaches)
                                        {
                                            Directory.Delete(oldCache.Path, true);
                                        }
                                    }
                                }
                                else
                                {
                                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                    {
                                        _ = Helper.MessageBox("The selected zip file does not contain any image files.", Helper.SoundHelper.Sound.Error);
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                                {
                                    if (ex.Message == "End of Central Directory record could not be found.")
                                    {
                                        _ = Helper.MessageBox("The selected file is corrupted or not a zip file.", Helper.SoundHelper.Sound.Error);
                                    }
                                    else
                                    {
                                        Helper.ThrowException(ex);
                                        await Helper.localFolder.CreateFileAsync("LockscreenBreak.txt", CreationCollisionOption.OpenIfExists);
                                    }
                                });
                            }
                        });
                        ZipFileProg.Collapse();
                        ZipFileProg.IsIndeterminate = false;
                    }
                }
                else
                {
                    var folderPicker = new FolderPicker
                    {
                        SuggestedStartLocation = PickerLocationId.ComputerFolder
                    };

                    folderPicker.FileTypeFilter.Add(".jpeg");
                    folderPicker.FileTypeFilter.Add(".jpg");
                    folderPicker.FileTypeFilter.Add(".png");

                    StorageFolder wallFolder = await folderPicker.PickSingleFolderAsync();
                    if (wallFolder != null)
                    {
                        bool isImagePresent = false;
                        foreach (var file in await wallFolder.GetFilesAsync())
                        {
                            if (System.IO.Path.GetExtension(file.Path) == ".jpeg" || System.IO.Path.GetExtension(file.Path) == ".jpg" || System.IO.Path.GetExtension(file.Path) == ".png")
                            {
                                isImagePresent = true;
                                break;
                            }
                        }
                        if (isImagePresent)
                        {
                            WallCollectionBox.Text = wallFolder.Path;
                            await FileIO.WriteTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"), $"{WallCollectionBox.Text}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[1]}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[2]}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[3]}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[4]}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[5]}");
                            await Helper.localFolder.CreateFileAsync("LockscreenBreak.txt", CreationCollisionOption.OpenIfExists);
                        }
                        else
                        {
                            _ = Helper.MessageBox("The selected folder does not contain any image files.", Helper.SoundHelper.Sound.Error);
                        }
                    }
                }
                WallCollectionBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Helper.ThrowException(ex);
            }
        }

        private async void WallIntervalBtn_Click(object sender, RoutedEventArgs e)
        {
            await FileIO.WriteTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"), $"{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[0]}\n{WallIntervalBox.Text}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[2]}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[3]}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[4]}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[5]}");
            await Helper.localFolder.CreateFileAsync("LockscreenBreak.txt", CreationCollisionOption.OpenIfExists);
        }

        private void WallIntervalBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            WallIntervalBtn.IsEnabled =
                WallIntervalBox.Text.HasContent() &&
                !WallIntervalBox.Text.Contains('.') &&
                WallIntervalBox.Text.ToInt32() >= 50 &&
                WallIntervalBox.Text.ToInt32() <= 60000;
        }

        private async void WallRevLoopTog_Toggled(object sender, RoutedEventArgs e)
        {
            var file = await Helper.localFolder.GetFileAsync("Lockscreen.dat");
            var originalLines = (await FileIO.ReadTextAsync(file)).Split('\n');

            originalLines[2] = WallRevLoopTog.IsOn ? "True" : "False";

            await FileIO.WriteTextAsync(file, string.Join("\n", originalLines));
            await Helper.localFolder.CreateFileAsync("LockscreenBreak.txt", CreationCollisionOption.OpenIfExists);
        }

        private async void ActiveHoursTog_Toggled(object sender, RoutedEventArgs e)
        {
            if (flag == true)
            {
                var file = await Helper.localFolder.GetFileAsync("Lockscreen.dat");
                var originalLines = (await FileIO.ReadTextAsync(file)).Split('\n');

                if (ActiveHoursTog.IsOn)
                {
                    originalLines[3] = new DateTime(StartTimePkr.Time.Ticks).ToString("HH:mm");
                    originalLines[4] = new DateTime(StopTimePkr.Time.Ticks).ToString("HH:mm");
                    await FileIO.WriteTextAsync(file, string.Join("\n", originalLines));
                    AppSettings.SaveSettings("ActiveHours", true);
                    ActiveHoursStack.Visible();
                }
                else
                {
                    originalLines[3] = "00:00";
                    originalLines[4] = "00:00";
                    await FileIO.WriteTextAsync(file, string.Join("\n", originalLines));
                    AppSettings.SaveSettings("ActiveHours", false);
                    ActiveHoursStack.Collapse();
                }

                await Helper.localFolder.CreateFileAsync("LockscreenBreak.txt", CreationCollisionOption.OpenIfExists);
            }
        }

        private async void LockscreenTimePkr_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            if (flag == true)
            {
                await FileIO.WriteTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"), $"{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[0]}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[1]}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[2]}\n{new DateTime(StartTimePkr.Time.Ticks).ToString("HH:mm")}\n{new DateTime(StopTimePkr.Time.Ticks).ToString("HH:mm")}\n{(await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n')[5]}");
                await Helper.localFolder.CreateFileAsync("LockscreenBreak.txt", CreationCollisionOption.OpenIfExists);
            }
        }

        private void BootAnimTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "BCD00000001\\Objects\\{7ea2e1ac-2e61-4728-aaa3-896d9d0a9f0e}\\Elements\\16000069",
                "Element", RegistryType.REG_BINARY, ToBinary(BootAnimTog.IsOn ? "00" : "01"));

            RegEdit.SetHKLMValue(
                "BCD00000001\\Objects\\{7ea2e1ac-2e61-4728-aaa3-896d9d0a9f0e}\\Elements\\1600007a",
                "Element", RegistryType.REG_BINARY, ToBinary(BootAnimTog.IsOn ? "00" : "01"));
        }

        private void BootImageTog_Toggled(object sender, RoutedEventArgs e)
        {
            if (BootImageTog.IsOn)
            {
                BootImageStack.Visible();

                if (flag == true)
                {
                    RegEdit.SetHKLMValue("System\\Shell\\OEM\\bootscreens", "wpbootscreenoverride", RegistryType.REG_SZ, Helper.installedLocation.Path + "\\Assets\\Images\\Bootscreens\\BootupImage.png");
                    BootImageBox.Text = $"CMDInjector:\\Assets\\Images\\Bootscreens\\BootupImage.png";
                    return;
                }

                BootImageBox.Text = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "System\\Shell\\OEM\\bootscreens", "wpbootscreenoverride", RegistryType.REG_SZ);

                if (BootImageBox.Text.Contains(Helper.installedLocation.Path))
                {
                    BootImageBox.Text = $"CMDInjector:\\Assets\\Images\\Bootscreens\\BootupImage.png";
                }
            }
            else
            {
                BootImageStack.Collapse();
                RegEdit.DeleteRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "System\\Shell\\OEM\\bootscreens", "wpbootscreenoverride");
            }
        }

        private async void BootImageBtn_Click(object sender, RoutedEventArgs e)
        {
            var folderOpenPicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            folderOpenPicker.FileTypeFilter.Add(".png");
            folderOpenPicker.FileTypeFilter.Add(".jpg");
            StorageFile bootImage = await folderOpenPicker.PickSingleFileAsync();
            if (bootImage == null)
            {
                return;
            }
            BootImageBox.Text = bootImage.Path;
            RegEdit.SetHKLMValue("System\\Shell\\OEM\\bootscreens", "wpbootscreenoverride", RegistryType.REG_SZ, bootImage.Path);
        }

        private void ShutdownImageTog_Toggled(object sender, RoutedEventArgs e)
        {
            if (ShutdownImageTog.IsOn)
            {
                ShutdownImageStack.Visible();
                if (flag == true)
                {
                    RegEdit.SetHKLMValue("System\\Shell\\OEM\\bootscreens", "wpshutdownscreenoverride", RegistryType.REG_SZ, Helper.installedLocation.Path + "\\Assets\\Images\\Bootscreens\\ShutdownImage.png");
                    ShutdownImageBox.Text = $"CMDInjector:\\Assets\\Images\\Bootscreens\\ShutdownImage.png";
                    return;
                }

                ShutdownImageBox.Text = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "System\\Shell\\OEM\\bootscreens", "wpshutdownscreenoverride", RegistryType.REG_SZ);
                if (ShutdownImageBox.Text.Contains(Helper.installedLocation.Path))
                {
                    ShutdownImageBox.Text = $"CMDInjector:\\Assets\\Images\\Bootscreens\\ShutdownImage.png";
                }
            }
            else
            {
                ShutdownImageStack.Collapse();
                RegEdit.DeleteRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "System\\Shell\\OEM\\bootscreens", "wpshutdownscreenoverride");
            }
        }

        private async void ShutdownImageBtn_Click(object sender, RoutedEventArgs e)
        {
            var folderOpenPicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };

            folderOpenPicker.FileTypeFilter.Add(".png");
            folderOpenPicker.FileTypeFilter.Add(".jpg");

            StorageFile shutdownImage = await folderOpenPicker.PickSingleFileAsync();
            if (shutdownImage == null)
            {
                return;
            }
            ShutdownImageBox.Text = shutdownImage.Path;
            RegEdit.SetHKLMValue("System\\Shell\\OEM\\bootscreens", "wpshutdownscreenoverride", RegistryType.REG_SZ, shutdownImage.Path);
        }

        private void SoftNavTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "SOFTWARE\\Microsoft\\Shell\\NavigationBar", "SoftwareModeEnabled",
                RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse(SoftNavTog.IsOn ? "1" : "0"))
            );

            SoftwareModeIndicator.Visible(flag);
        }

        private void DoubleTapTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "SOFTWARE\\Microsoft\\Shell\\NavigationBar", "IsDoubleTapOffEnabled",
                RegistryType.REG_DWORD, BitConverter.GetBytes(DoubleTapTog.IsOn ? 1u : 0u)
            );
            DoubleTapIndicator.Visible(flag);
        }

        private void AutoHideTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "SOFTWARE\\Microsoft\\Shell\\NavigationBar", "IsAutoHideEnabled",
                RegistryType.REG_DWORD, BitConverter.GetBytes(AutoHideTog.IsOn ? 1u : 0u)
            );
            AutoHideIndicator.Visible(flag);
        }

        private void SwipeUpTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "SOFTWARE\\Microsoft\\Shell\\NavigationBar", "IsSwipeUpToHideEnabled",
                RegistryType.REG_DWORD, BitConverter.GetBytes(SwipeUpTog.IsOn ? 1u : 0u));
            SwipeUpIndicator.Visible(flag);
        }

        private void UserManagedTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "SOFTWARE\\Microsoft\\Shell\\NavigationBar", "IsUserManaged",
                RegistryType.REG_DWORD, BitConverter.GetBytes(UserManagedTog.IsOn ? 1u : 0u)
            );
            UserManagedIndicator.Visible(flag);
        }

        private void BurninProtTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "SOFTWARE\\Microsoft\\Shell\\NavigationBar",
                "BurnInProtectionMaskSwitchingInterval",
                RegistryType.REG_DWORD,
                BitConverter.GetBytes(BurninProtTog.IsOn ? 1u : 0u)
            );
            RegEdit.SetHKLMValue(
                "SOFTWARE\\Microsoft\\Shell\\NavigationBar",
                "IsBurnInProtectionEnabled", RegistryType.REG_DWORD,
                BitConverter.GetBytes(BurninProtTog.IsOn ? 1u : 0u)
            );

            BurnInIndicator.Visible(flag);
        }

        private void BurninTimeoutBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            BurninTimeoutBtn.IsEnabled = BurninTimeoutBox.Text.HasContent();
        }

        private void BurninTimeoutBtn_Click(object sender, RoutedEventArgs e)
        {
            if (BurninTimeoutBox.Text != string.Empty)
            {
                RegEdit.SetHKLMValue("SOFTWARE\\Microsoft\\Shell\\NavigationBar", "BurnInProtectionIdleTimerTimeout", RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse(BurninTimeoutBox.Text)));
                BurnInTimeoutIndicator.Visible();
            }
        }

        private void ColorPickCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flag && ColorPickCombo.SelectedIndex != 0)
            {
                ComboBoxItem cbi = ColorPickCombo.SelectedItem as ComboBoxItem;
                StackPanel stackPanel = cbi.Content as StackPanel;
                Brush selectedColor = (stackPanel.Children[0] as Rectangle).Fill;
                SolidColorBrush solidColor = (SolidColorBrush)selectedColor;
                string hexColor = solidColor.Color.ToString().Remove(0, 3);
                int decimalColor = Convert.ToInt32(hexColor, 16);
                RegEdit.SetHKLMValue("SOFTWARE\\Microsoft\\Shell\\NavigationBar", "BurnInProtectionBlackReplacementColor", RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse(decimalColor.ToString())));
                BurnInColorIndicator.Visible();
            }
        }

        private void OpacitySlide_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (sender is Slider slide)
            {
                RegEdit.SetHKLMValue("SOFTWARE\\Microsoft\\Shell\\NavigationBar", "BurnInProtectionIconsOpacity", RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse(slide.Value.ToString())));
            }
            BurnInOpacityIndicator.Visible(flag);
        }

        private void DumpTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RegEdit.SetHKLMValue("SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps", "DumpType", RegistryType.REG_DWORD, "0000000" + DumpTypeCombo.SelectedIndex);
        }

        private void DumpCountCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RegEdit.SetHKLMValue("SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps", "DumpCount", RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse(((ComboBoxItem)DumpCountCombo.SelectedItem).Content.ToString())));
        }

        private async void DumpFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder dumpFolder = await folderPicker.PickSingleFolderAsync();
            if (dumpFolder != null)
            {
                RegEdit.SetHKLMValue("SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps", "DumpFolder", RegistryType.REG_MULTI_SZ, Encoding.Unicode.GetBytes(dumpFolder.Path + '\0'));
                DumpFolderBox.Text = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps", "DumpFolder", RegistryType.REG_MULTI_SZ);
            }
        }

        private void TileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TileCombo.SelectedIndex == 0) RegEdit.SetHKLMValue("Software\\Microsoft\\Shell\\Start", "TileColumnSize", RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse("4")));
            else if (TileCombo.SelectedIndex == 1) RegEdit.SetHKLMValue("Software\\Microsoft\\Shell\\Start", "TileColumnSize", RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse("6")));
            else if (TileCombo.SelectedIndex == 2) RegEdit.SetHKLMValue("Software\\Microsoft\\Shell\\Start", "TileColumnSize", RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse("8")));
            else if (TileCombo.SelectedIndex == 3) RegEdit.SetHKLMValue("Software\\Microsoft\\Shell\\Start", "TileColumnSize", RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse("10")));
            else if (TileCombo.SelectedIndex == 4) RegEdit.SetHKLMValue("Software\\Microsoft\\Shell\\Start", "TileColumnSize", RegistryType.REG_DWORD, BitConverter.GetBytes(uint.Parse("12")));

            StartTileIndicator.Visible(flag);
        }

        private void VirtualMemBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            VirtualMemBtn.IsEnabled = VirtualMemBox.Text.HasContent();
        }

        private void DngTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue("SOFTWARE\\OEM\\NOKIA\\Camera\\Barc", "DNGDisabled", RegistryType.REG_DWORD, DngTog.IsOn.Toggle().ToDWORDStr());
        }

        private void VirtualMemBtn_Click(object sender, RoutedEventArgs e)
        {
            if (VirtualMemBox.Text.HasContent())
            {
                RegEdit.SetHKLMValue(
                    "System\\CurrentControlSet\\Control\\Session Manager\\Memory Management",
                    "PagingFiles", RegistryType.REG_MULTI_SZ, Encoding.Unicode.GetBytes(VirtualMemBox.Text + '\0')
                );
                VirtualMemoryIndicator.Visible();
            }
        }

        private byte[] ToBinary(string data)
        {
            data = data.Replace("-", "");
            return Enumerable.Range(0, data.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(data.Substring(x, 2), 16)).ToArray();
        }

        private void BackgModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BackgModeCombo.SelectedIndex == 0)
            {
                /*Helper.Send("reg add HKLM\\Software\\Microsoft\\Windows\\Currentversion\\Themes\\Personalize /v SystemUsesLightTheme /t REG_DWORD /d 0 /f" +
                    "&reg add HKLM\\Software\\Microsoft\\Windows\\Currentversion\\Themes\\Personalize /v AppsUseLightTheme /t REG_DWORD /d 0 /f" +
                    "&reg add \"HKLM\\Software\\Microsoft\\Windows\\Currentversion\\Control Panel\\Theme\" /v CurrentTheme /t REG_DWORD /d 1 /f");*/
                RegEdit.SetHKLMValue("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "SystemUsesLightTheme", RegistryType.REG_DWORD, "00000000");
                RegEdit.SetHKLMValue("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", RegistryType.REG_DWORD, "00000000");
                RegEdit.SetHKLMValue("Software\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\Theme", "CurrentTheme", RegistryType.REG_DWORD, "00000001");

                AutoBackgItem.Content = "Dark Until Sunrise";

                if (AutoBackgCombo.SelectedIndex == 1)
                {
                    AutoBackgCombo.SelectedIndex = -1;
                    AutoBackgCombo.SelectedIndex = 1;
                }
            }
            else
            {
                /*Helper.Send("reg add HKLM\\Software\\Microsoft\\Windows\\Currentversion\\Themes\\Personalize /v SystemUsesLightTheme /t REG_DWORD /d 1 /f" +
                    "&reg add HKLM\\Software\\Microsoft\\Windows\\Currentversion\\Themes\\Personalize /v AppsUseLightTheme /t REG_DWORD /d 1 /f" +
                    "&reg add \"HKLM\\Software\\Microsoft\\Windows\\Currentversion\\Control Panel\\Theme\" /v CurrentTheme /t REG_DWORD /d 0 /f");*/
                RegEdit.SetHKLMValue("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "SystemUsesLightTheme", RegistryType.REG_DWORD, "00000001");
                RegEdit.SetHKLMValue("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", RegistryType.REG_DWORD, "00000001");
                RegEdit.SetHKLMValue("Software\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\Theme", "CurrentTheme", RegistryType.REG_DWORD, "00000000");

                AutoBackgItem.Content = "Light Until Sunset";

                if (AutoBackgCombo.SelectedIndex == 1)
                {
                    AutoBackgCombo.SelectedIndex = -1;
                    AutoBackgCombo.SelectedIndex = 1;
                }
            }
            if (!AppSettings.ThemeSettings)
            {
                Helper.color = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\Theme",
                    "CurrentTheme", RegistryType.REG_DWORD
                ).IsDWORDStrFullZeros() ? Colors.White : Colors.Black;
            }
        }

        private void AutoBackgCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AutoBackgCombo.SelectedIndex == 0)
            {
                BackgShiftStack.Collapse();
            }
            else if (AutoBackgCombo.SelectedIndex == 1)
            {
                BackgShiftStack.Collapse();
                AppSettings.SaveSettings("AutoThemeLight", "06:00");
                AppSettings.SaveSettings("AutoThemeDark", "18:00");
                BackgStartTime.Time = new TimeSpan(06, 00, 00);
                BackgStopTime.Time = new TimeSpan(18, 00, 00);
            }
            else if (AutoBackgCombo.SelectedIndex == 2)
            {
                BackgShiftStack.Visible();
            }
            AppSettings.SaveSettings("AutoThemeMode", AutoBackgCombo.SelectedIndex);
        }

        private void ThemeBackgTime_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            if (flag)
            {
                AppSettings.AutoThemeLight = new DateTime(BackgStartTime.Time.Ticks).ToString("HH:mm");
                AppSettings.AutoThemeDark = new DateTime(BackgStopTime.Time.Ticks).ToString("HH:mm");
            }
        }

        private async void AccentColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (AccentColorCombo.SelectedIndex != 0)
                {
                    var cbi = AccentColorCombo.SelectedItem as ComboBoxItem;
                    var stackPanel = cbi.Content as StackPanel;
                    var selectedColor = (stackPanel.Children[0] as Rectangle).Fill;
                    var solidColor = (SolidColorBrush)selectedColor;
                    var col = Color.FromArgb(255, solidColor.Color.R, solidColor.Color.G, solidColor.Color.B);
                    var CurrentAccentHex = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, @"Software\Microsoft\Windows\CurrentVersion\Control Panel\Theme", "CurrentAccent", RegistryType.REG_DWORD);
                    var CurrentAccent = int.Parse(CurrentAccentHex, System.Globalization.NumberStyles.HexNumber);
                    for (int i = 0; i <= 1; i++)
                    {
                        RegEdit.SetHKLMValue($@"Software\Microsoft\Windows\CurrentVersion\Control Panel\Theme\Themes\{i}\Accents\{CurrentAccent.ToString()}", "Color", RegistryType.REG_DWORD, col.ToString().Replace("#", string.Empty));
                        RegEdit.SetHKLMValue($@"Software\Microsoft\Windows\CurrentVersion\Control Panel\Theme\Themes\{i}\Accents\{CurrentAccent.ToString()}", "ComplementaryColor", RegistryType.REG_DWORD, col.ToString().Replace("#", string.Empty));
                        //RegEdit.SetHKLMValue($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Theme\Themes", "SpecialColor", RegistryType.REG_DWORD, Convert.ToInt32(col.R.ToString("X2") + col.G.ToString("X2") + col.B.ToString("X2"), 16).ToString());
                    }
                    var regvalue = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, @"Software\Microsoft\Windows\CurrentVersion\Control Panel\Theme", "AccentPalette", RegistryType.REG_BINARY);
                    var array = regvalue.ToCharArray();
                    var anotherArray = col.ToString().Replace("#", string.Empty).ToCharArray();

                    array.SetValue(anotherArray.GetValue(0), 24);
                    array.SetValue(anotherArray.GetValue(1), 25);
                    array.SetValue(anotherArray.GetValue(2), 26);
                    array.SetValue(anotherArray.GetValue(3), 27);
                    array.SetValue(anotherArray.GetValue(4), 28);
                    array.SetValue(anotherArray.GetValue(5), 29);

                    var newpalette = string.Join("", array);
                    RegEdit.SetHKLMValue(@"Software\Microsoft\Windows\CurrentVersion\Control Panel\Theme", "AccentPalette", RegistryType.REG_BINARY, newpalette);
                    await Task.Delay(200);
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
                    if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                    {
                        var statusBar = StatusBar.GetForCurrentView();
                        if (statusBar != null)
                        {
                            statusBar.ForegroundColor = accentColor;
                        }
                    }
                    AccentColorTwoCombo.SelectedIndex = AccentColorCombo.SelectedIndex;
                    AccentColorCombo.IsEnabled = false;
                    AccentColorTwoCombo.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Helper.ThrowException(ex);
            }
        }

        private void AccentColorTwoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (AccentColorTwoCombo.SelectedIndex == 0) return;
                var cbi = AccentColorTwoCombo.SelectedItem as ComboBoxItem;
                var stackPanel = cbi.Content as StackPanel;
                var selectedColor = (stackPanel.Children[0] as Rectangle).Fill;
                var solidColor = (SolidColorBrush)selectedColor;
                var col = Color.FromArgb(255, solidColor.Color.R, solidColor.Color.G, solidColor.Color.B);
                var CurrentAccentHex = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, @"Software\Microsoft\Windows\CurrentVersion\Control Panel\Theme", "CurrentAccent", RegistryType.REG_DWORD);
                var CurrentAccent = int.Parse(CurrentAccentHex, System.Globalization.NumberStyles.HexNumber);
                for (int i = 0; i <= 1; i++)
                {
                    RegEdit.SetHKLMValue($@"Software\Microsoft\Windows\CurrentVersion\Control Panel\Theme\Themes\{i}\Accents\{CurrentAccent.ToString()}", "Color", RegistryType.REG_DWORD, col.ToString().Replace("#", string.Empty));
                    RegEdit.SetHKLMValue($@"Software\Microsoft\Windows\CurrentVersion\Control Panel\Theme\Themes\{i}\Accents\{CurrentAccent.ToString()}", "ComplementaryColor", RegistryType.REG_DWORD, col.ToString().Replace("#", string.Empty));
                    //RegEdit.SetHKLMValue($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Theme\Themes", "SpecialColor", 4, Convert.ToInt32(col.R.ToString("X2") + col.G.ToString("X2") + col.B.ToString("X2"), 16).ToString(), 0);
                }
                var regvalue = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, @"Software\Microsoft\Windows\CurrentVersion\Control Panel\Theme", "AccentPalette", RegistryType.REG_BINARY);
                var array = regvalue.ToCharArray();
                var colAsAnArray = col.ToString().Replace("#", "").ToCharArray();

                array.SetValue(colAsAnArray.GetValue(0), 24);
                array.SetValue(colAsAnArray.GetValue(1), 25);
                array.SetValue(colAsAnArray.GetValue(2), 26);
                array.SetValue(colAsAnArray.GetValue(3), 27);
                array.SetValue(colAsAnArray.GetValue(4), 28);
                array.SetValue(colAsAnArray.GetValue(5), 29);

                var newpalette = string.Join("", array);
                RegEdit.SetHKLMValue(@"Software\Microsoft\Windows\CurrentVersion\Control Panel\Theme", "AccentPalette", RegistryType.REG_BINARY, newpalette);
            }
            catch (Exception ex)
            {
                //Helper.ThrowException(ex);
            }
        }

        private async void AccentColorInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            TextBlock resultBlock = new TextBlock()
            {
                Text = "Two accent color properties are available:\n1. The primary color used by UWP apps.\n2. The secondary color used by Silverlight apps.\n\nThe secondary color will change as well when you change the primary color. To select a secondary color you must first select a primary color. Once the primary color is selected, you need to restart this App to change the primary color again.",
                TextWrapping = TextWrapping.Wrap
            };
            ScrollViewer resultScrollViewer = new ScrollViewer() { Content = resultBlock };
            ContentDialog resultDialog = new ContentDialog()
            {
                Content = resultScrollViewer,
                Title = "Info",
                CloseButtonText = "OK",
            };
            Helper.SoundHelper.PlaySound(Helper.SoundHelper.Sound.Alert);
            await resultDialog.ShowAsync();
        }

        private void NightModeSlide_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Slider slider = sender as Slider;
            var rounded = Math.Round(slider.Value / 100 * (63 - 32) + (63 - 32) + 1);
            RegEdit.SetHKLMValue(@"SOFTWARE\OEM\Nokia\Display\ColorAndLight", "UserSettingWhitePoint", RegistryType.REG_DWORD, BitConverter.GetBytes(Convert.ToUInt32(rounded.ToString(), 16)));
            RegEdit.SetHKLMValue(@"SOFTWARE\OEM\Nokia\Display\ColorAndLight", "UserSettingNightLightPct", RegistryType.REG_SZ, Encoding.Unicode.GetBytes($"0,{rounded}" + '\0'));
        }

        private async Task Restart()
        {
            try
            {
                var result = await Helper.MessageBox("Are you sure you want to restart the device?", Helper.SoundHelper.Sound.Alert, "", "No", true, "Yes");
                if (result == 0)
                {
                    Helper.RebootSystem();
                }
            }
            catch (Exception ex) { Helper.ThrowException(ex); }
        }

        private async Task Shutdown()
        {
            try
            {
                var result = await Helper.MessageBox("Are you sure you want to shutdown the device?", Helper.SoundHelper.Sound.Alert, "", "No", true, "Yes");
                if (result == 0)
                {
                    if (Helper.IsTelnetConnected() && HomeHelper.IsConnected())
                    {
                        Helper.Send("shutdown /s /t 0");
                    }
                    else
                    {
                        _ = Helper.MessageBox(HomeHelper.GetTelnetTroubleshoot(), Helper.SoundHelper.Sound.Error, "Error");
                    }
                }
            }
            catch (Exception ex) { Helper.ThrowException(ex); }
        }

        private void ShutBtn_Click(object sender, RoutedEventArgs e)
        {
            if (buttonOnHold)
            {
                buttonOnHold = false;
                return;
            }
            _ = Shutdown();
        }

        private void RestartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (buttonOnHold)
            {
                buttonOnHold = false;
                return;
            }
            _ = Restart();
        }

        private async void RestartBtn_Holding(object sender, HoldingRoutedEventArgs e)
        {
            try
            {
                buttonOnHold = true;
                if (e.HoldingState != Windows.UI.Input.HoldingState.Started) return;
                SecondaryTile RestartTile = new SecondaryTile("RestartTileID", "Restart", "Restart", new Uri("ms-appx:///Assets/Icons/PowerOptions/RestartPowerOptionTileLogo.png"), TileSize.Default);
                RestartTile.VisualElements.ShowNameOnSquare150x150Logo = true;
                RestartTile.VisualElements.ShowNameOnWide310x150Logo = true;
                RestartTile.VisualElements.ShowNameOnSquare310x310Logo = true;
                bool isPinned = await RestartTile.RequestCreateAsync();
                if (isPinned)
                {
                    TipText.Collapse();
                    AppSettings.SaveSettings("TipSettings", false);
                }
            }
            catch (Exception ex) { /*Helper.ThrowException(ex);*/ }
        }

        private async void ShutBtn_Holding(object sender, HoldingRoutedEventArgs e)
        {
            try
            {
                buttonOnHold = true;
                if (Helper.IsTelnetConnected() && HomeHelper.IsConnected())
                {
                    SecondaryTile ShutdownTile = new SecondaryTile("ShutdownTileID", "Shutdown", "Shutdown", new Uri("ms-appx:///Assets/Icons/PowerOptions/ShutdownPowerOptionTileLogo.png"), TileSize.Default);
                    ShutdownTile.VisualElements.ShowNameOnSquare150x150Logo = true;
                    ShutdownTile.VisualElements.ShowNameOnWide310x150Logo = true;
                    ShutdownTile.VisualElements.ShowNameOnSquare310x310Logo = true;
                    bool isPinned = await ShutdownTile.RequestCreateAsync();
                    if (isPinned)
                    {
                        TipText.Collapse();
                        AppSettings.SaveSettings("TipSettings", false);
                    }
                }
                else
                {
                    _ = Helper.MessageBox(HomeHelper.GetTelnetTroubleshoot(), Helper.SoundHelper.Sound.Error, "Error");
                }
            }
            catch (Exception ex) { /*Helper.ThrowException(ex);*/ }
        }

        private async void FFULoaderBtn_Holding(object sender, HoldingRoutedEventArgs e)
        {
            try
            {
                buttonOnHold = true;
                if (e.HoldingState != Windows.UI.Input.HoldingState.Started) return;
                if (Helper.IsTelnetConnected() && HomeHelper.IsConnected() && File.Exists(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"))
                {
                    SecondaryTile FFULoaderTile = new SecondaryTile("FFULoaderTileID", "FFU Loader", "FFULoader", new Uri("ms-appx:///Assets/Icons/PowerOptions/FFULoaderPowerOptionTileLogo.png"), TileSize.Default);
                    FFULoaderTile.VisualElements.ShowNameOnSquare150x150Logo = true;
                    FFULoaderTile.VisualElements.ShowNameOnWide310x150Logo = true;
                    FFULoaderTile.VisualElements.ShowNameOnSquare310x310Logo = true;
                    bool isPinned = await FFULoaderTile.RequestCreateAsync();
                    if (isPinned)
                    {
                        TipText.Collapse();
                        AppSettings.SaveSettings("TipSettings", false);
                    }
                }
                else
                {
                    _ = Helper.MessageBox(HomeHelper.GetTelnetTroubleshoot(), Helper.SoundHelper.Sound.Error, "Error");
                }
            }
            catch (Exception ex) { /*Helper.ThrowException(ex);*/ }
        }

        private void LockscreenBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (buttonOnHold)
                {
                    buttonOnHold = false;
                    return;
                }
                if (Helper.IsTelnetConnected() && HomeHelper.IsConnected())
                {
                    Helper.Send("powertool -screenoff");
                    Helper.SoundHelper.PlaySound(Helper.SoundHelper.Sound.Lock);
                }
                else
                {
                    _ = Helper.MessageBox(HomeHelper.GetTelnetTroubleshoot(), Helper.SoundHelper.Sound.Error, "Error");
                }
            }
            catch (Exception ex) { Helper.ThrowException(ex); }
        }

        private async void FFULoaderBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (buttonOnHold)
                {
                    buttonOnHold = false;
                    return;
                }
                var result = await Helper.MessageBox("Are you sure you want to reboot the device to FFU Loader?", Helper.SoundHelper.Sound.Alert, "", "No", true, "Yes");
                if (result == 0)
                {
                    if (Helper.IsTelnetConnected() && HomeHelper.IsConnected() && File.Exists(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"))
                    {
                        Helper.Send(Helper.installedLocation.Path + "\\Contents\\BatchScripts\\RebootToFlashingMode.bat");
                        await Helper.MessageBox("Rebooting to FFU Loader...", Helper.SoundHelper.Sound.Alert);
                    }
                    else
                    {
                        _ = Helper.MessageBox(HomeHelper.GetTelnetTroubleshoot(), Helper.SoundHelper.Sound.Error, "Error");
                    }
                }
            }
            catch (Exception ex) { Helper.ThrowException(ex); }
        }

        private async void LockscreenBtn_Holding(object sender, HoldingRoutedEventArgs e)
        {
            try
            {
                buttonOnHold = true;
                if (e.HoldingState != Windows.UI.Input.HoldingState.Started) return;
                if (Helper.IsTelnetConnected() && HomeHelper.IsConnected())
                {
                    SecondaryTile LockscreenTile = new SecondaryTile("LockscreenTileID", "Lockscreen", "Lockscreen", new Uri("ms-appx:///Assets/Icons/PowerOptions/LockscreenPowerOptionTileLogo.png"), TileSize.Default);
                    LockscreenTile.VisualElements.ShowNameOnSquare150x150Logo = true;
                    LockscreenTile.VisualElements.ShowNameOnWide310x150Logo = true;
                    LockscreenTile.VisualElements.ShowNameOnSquare310x310Logo = true;
                    bool isPinned = await LockscreenTile.RequestCreateAsync();
                    if (isPinned)
                    {
                        TipText.Collapse();
                        AppSettings.SaveSettings("TipSettings", false);
                    }
                }
                else
                {
                    _ = Helper.MessageBox(HomeHelper.GetTelnetTroubleshoot(), Helper.SoundHelper.Sound.Error, "Error");
                }
            }
            catch (Exception ex) { /*Helper.ThrowException(ex);*/ }
        }

        private void VolumeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (buttonOnHold)
            {
                buttonOnHold = false;
                return;
            }
            if (Helper.IsTelnetConnected() && HomeHelper.IsConnected())
            {
                var button = sender as Button;
                if (button.Content.ToString() == "Volume Up")
                {
                    Helper.Send("SendKeys -v \"0xAF 0xAF\"");
                }
                else if (button.Content.ToString() == "Volume Down")
                {
                    Helper.Send("SendKeys -v \"0xAE 0xAE\"");
                }
                else if (button.Content.ToString() == "Mute/Unmute")
                {
                    Helper.Send("SendKeys -v \"0xAD\"");
                }
            }
            else
            {
                _ = Helper.MessageBox(HomeHelper.GetTelnetTroubleshoot(), Helper.SoundHelper.Sound.Error, "Error");
            }
        }

        private void RestoreNDTKTog_Toggled(object sender, RoutedEventArgs e)
        {
            if (secondFlag == false) return;
            if (RestoreNDTKTog.IsOn)
            {
                RegEdit.SetHKLMValue("SOFTWARE\\OEM\\Nokia\\NokiaSvcHost\\Plugins\\NsgExtA\\NdtkSvc", "Path", RegistryType.REG_SZ, "C:\\Windows\\System32\\NdtkSvc.dll");
                NDTKIndicator.Visible();
            }
            else
            {
                RegEdit.SetHKLMValue("SOFTWARE\\OEM\\Nokia\\NokiaSvcHost\\Plugins\\NsgExtA\\NdtkSvc", "Path", RegistryType.REG_SZ, "NdtkSvc.dll");
                NDTKIndicator.Visible();
            }
        }

        private async void SecMgrPatchBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = await Helper.MessageBox("Are you sure you want to patch the driver?", Helper.SoundHelper.Sound.Alert, "", "No", true, "Yes");
            if (result == 0)
            {
                FilesHelper.CopyFromAppRoot("\\Drivers\\PatchedSecMgr.sys", @"C:\Windows\System32\Drivers\SecMgr.sys");
                SecMgrIndicator.Visible();
            }
        }

        private async void SecMgrRestoreBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = await Helper.MessageBox("Are you sure you want to restore the driver?", Helper.SoundHelper.Sound.Alert, "", "No", true, "Yes");
            if (result == 0)
            {
                FilesHelper.CopyFromAppRoot("\\Drivers\\OriginalSecMgr.sys", @"C:\Windows\System32\Drivers\SecMgr.sys");
                SecMgrIndicator.Visible();
            }
        }

        private void UfpEnableBtn_Click(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValueEx("BCD00000001\\Objects\\{01de5a27-8705-40db-bad6-96fa5187d4a6}\\Elements\\25000209", "Element", RegistryType.REG_BINARY, "0100000000000000");
            RegEdit.SetHKLMValueEx("BCD00000001\\Objects\\{01de5a27-8705-40db-bad6-96fa5187d4a6}\\Elements\\26000207", "Element", RegistryType.REG_BINARY, "01");
            RegEdit.SetHKLMValueEx("BCD00000001\\Objects\\{0ff5f24a-3785-4aeb-b8fe-4226215b88c4}\\Elements\\25000209", "Element", RegistryType.REG_BINARY, "0100000000000000");
            RegEdit.SetHKLMValueEx("BCD00000001\\Objects\\{0ff5f24a-3785-4aeb-b8fe-4226215b88c4}\\Elements\\26000207", "Element", RegistryType.REG_BINARY, "01");
        }

        private async void UfpDisableBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Helper.IsTelnetConnected() && HomeHelper.IsConnected())
            {
                await Helper.Send("reg delete hklm\\BCD00000001\\Objects\\{01de5a27-8705-40db-bad6-96fa5187d4a6}\\Elements\\25000209 /f" +
                                 "&reg delete hklm\\BCD00000001\\Objects\\{01de5a27-8705-40db-bad6-96fa5187d4a6}\\Elements\\26000207 /f" +
                                 "&reg delete hklm\\BCD00000001\\Objects\\{0ff5f24a-3785-4aeb-b8fe-4226215b88c4}\\Elements\\25000209 /f" +
                                 "&reg delete hklm\\BCD00000001\\Objects\\{0ff5f24a-3785-4aeb-b8fe-4226215b88c4}\\Elements\\26000207 /f");
            }
            else
            {
                await Helper.MessageBox(HomeHelper.GetTelnetTroubleshoot(), Helper.SoundHelper.Sound.Error, "Error");
            }
        }

        private async void SearchPressAppsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (secondFlag == false || SearchPressAppsCombo.SelectedIndex == 0) return;
                if (!await Helper.IsCapabilitiesAllowed())
                {
                    if (!await Helper.AskCapabilitiesPermission())
                    {
                        SearchPressAppsCombo.SelectedIndex = 0;
                        return;
                    }
                }
                if (SearchPressAppsCombo.SelectedIndex == 1)
                {
                    RegEdit.SetHKLMValue("SYSTEM\\Input\\WEH\\Buttons\\WEHButton4\\Press", "AppID", RegistryType.REG_SZ, "");
                }
                else if (SearchPressAppsCombo.SelectedIndex == 2)
                {
                    RegEdit.SetHKLMValue("SYSTEM\\Input\\WEH\\Buttons\\WEHButton4\\Press", "AppID", RegistryType.REG_SZ, "{None}");
                }
                else
                {
                    SearchPressParaCombo.SelectedIndex = 1;
                    if (Packages[SearchPressAppsCombo.SelectedIndex - 3].DisplayName == "CMD Injector") SearchPressParaCombo.Visible();
                    else SearchPressParaCombo.Collapse();
                    var manifest = await Packages[SearchPressAppsCombo.SelectedIndex - 3].InstalledLocation.GetFileAsync("AppxManifest.xml");
                    var tags = XElement.Load(manifest.Path).Elements().Where(i => i.Name.LocalName == "PhoneIdentity");
                    var attributes = tags.Attributes().Where(i => i.Name.LocalName == "PhoneProductId");
                    RegEdit.SetHKLMValue("SYSTEM\\Input\\WEH\\Buttons\\WEHButton4\\Press", "AppID", RegistryType.REG_SZ, $"{{{attributes.First().Value}}}");
                }

                var isRemapped = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\Features", "ButtonRemapping", RegistryType.REG_SZ);
                SearchOptIndicator.Visible(isRemapped != "WEHButtonRouter.dll");
                RegEdit.SetHKLMValue("SYSTEM\\Features", "ButtonRemapping", RegistryType.REG_SZ, "WEHButtonRouter.dll");
            }
            catch (Exception ex)
            {
                Helper.ThrowException(ex);
            }
        }

        private async void SearchHoldAppsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (secondFlag == false || SearchHoldAppsCombo.SelectedIndex == 0) return;
                if (!await Helper.IsCapabilitiesAllowed())
                {
                    if (!await Helper.AskCapabilitiesPermission())
                    {
                        SearchHoldAppsCombo.SelectedIndex = 0;
                        return;
                    }
                }

                if (SearchHoldAppsCombo.SelectedIndex == 1)
                {
                    RegEdit.SetHKLMValue("SYSTEM\\Input\\WEH\\Buttons\\WEHButton4\\PressAndHold", "AppID", RegistryType.REG_SZ, "");
                }
                else if (SearchHoldAppsCombo.SelectedIndex == 2)
                {
                    RegEdit.SetHKLMValue("SYSTEM\\Input\\WEH\\Buttons\\WEHButton4\\PressAndHold", "AppID", RegistryType.REG_SZ, "{None}");
                }
                else
                {
                    SearchHoldParaCombo.SelectedIndex = 1;
                    if (Packages[SearchHoldAppsCombo.SelectedIndex - 3].DisplayName == "CMD Injector") SearchHoldParaCombo.Visible();
                    else SearchHoldParaCombo.Collapse();
                    var manifest = await Packages[SearchHoldAppsCombo.SelectedIndex - 3].InstalledLocation.GetFileAsync("AppxManifest.xml");
                    var tags = XElement.Load(manifest.Path).Elements().Where(i => i.Name.LocalName == "PhoneIdentity");
                    var attributes = tags.Attributes().Where(i => i.Name.LocalName == "PhoneProductId");
                    RegEdit.SetHKLMValue("SYSTEM\\Input\\WEH\\Buttons\\WEHButton4\\PressAndHold", "AppID", RegistryType.REG_SZ, $"{{{attributes.First().Value}}}");
                }

                var isRemapped = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\Features", "ButtonRemapping", RegistryType.REG_SZ);
                SearchOptIndicator.Visible(isRemapped != "WEHButtonRouter.dll");
                RegEdit.SetHKLMValue("SYSTEM\\Features", "ButtonRemapping", RegistryType.REG_SZ, "WEHButtonRouter.dll");
            }
            catch (Exception ex)
            {
                Helper.ThrowException(ex);
            }
        }

        private void StartWallTog_Toggled(object sender, RoutedEventArgs e)
        {
            AppSettings.StartWallSwitch = StartWallTog.IsOn;
            WallSwitchExtraStack.Visible(StartWallTog.IsOn);
            AppSettings.StartWallImagePosition = 0;
        }

        private async void StartWallLibraryBtn_Click(object sender, RoutedEventArgs e)
        {
            var library = await TweakBoxHelper.SetWallpaperLibrary();
            if (library != null)
            {
                StartWallLibPathBox.Text =
                    library.Path.Contains(Helper.installedLocation.Path) ?
                        "CMDInjector:\\Assets\\Images\\Lockscreens\\Stripes" :
                        library.Path;
                AppSettings.StartWallImagePosition = 0;
            }
        }

        private void StartWallTrigCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartWallInterCombo.Visible(StartWallTrigCombo.SelectedIndex == 0);
            AppSettings.StartWallTrigger = StartWallTrigCombo.SelectedIndex;
        }

        private async void VolDownBtn_Holding(object sender, HoldingRoutedEventArgs e)
        {
            try
            {
                buttonOnHold = true;
                if (e.HoldingState != Windows.UI.Input.HoldingState.Started) return;
                SecondaryTile VolDownTile = new SecondaryTile("VolDownTileID", "Volume down", "VolDown", new Uri("ms-appx:///Assets/Icons/VolumeOptions/DownVolumeOptionTileLogo.png"), TileSize.Default);
                VolDownTile.VisualElements.ShowNameOnSquare150x150Logo = true;
                VolDownTile.VisualElements.ShowNameOnWide310x150Logo = true;
                VolDownTile.VisualElements.ShowNameOnSquare310x310Logo = true;
                bool isPinned = await VolDownTile.RequestCreateAsync();
                if (isPinned)
                {
                    TipText.Collapse();
                    AppSettings.SaveSettings("TipSettings", false);
                }
            }
            catch (Exception ex) { /*Helper.ThrowException(ex);*/ }
        }

        private async void VolUpBtn_Holding(object sender, HoldingRoutedEventArgs e)
        {
            try
            {
                buttonOnHold = true;
                SecondaryTile VolUpTile = new SecondaryTile("VolUpTileID", "Volume up", "VolUp", new Uri("ms-appx:///Assets/Icons/VolumeOptions/UpVolumeOptionTileLogo.png"), TileSize.Default);
                VolUpTile.VisualElements.ShowNameOnSquare150x150Logo = true;
                VolUpTile.VisualElements.ShowNameOnWide310x150Logo = true;
                VolUpTile.VisualElements.ShowNameOnSquare310x310Logo = true;
                bool isPinned = await VolUpTile.RequestCreateAsync();
                if (isPinned)
                {
                    TipText.Collapse();
                    AppSettings.SaveSettings("TipSettings", false);
                }
            }
            catch (Exception ex) { /*Helper.ThrowException(ex);*/ }
        }

        private async void VolMuteBtn_Holding(object sender, HoldingRoutedEventArgs e)
        {
            try
            {
                buttonOnHold = true;
                if (e.HoldingState != Windows.UI.Input.HoldingState.Started) return;
                SecondaryTile VolMuteTile = new SecondaryTile("VolMuteTileID", "Volume mute/unmute", "VolMute", new Uri("ms-appx:///Assets/Icons/VolumeOptions/MuteVolumeOptionTileLogo.png"), TileSize.Default);
                VolMuteTile.VisualElements.ShowNameOnSquare150x150Logo = true;
                VolMuteTile.VisualElements.ShowNameOnWide310x150Logo = true;
                VolMuteTile.VisualElements.ShowNameOnSquare310x310Logo = true;
                bool isPinned = await VolMuteTile.RequestCreateAsync();
                if (isPinned)
                {
                    TipText.Collapse();
                    AppSettings.SaveSettings("TipSettings", false);
                }
            }
            catch (Exception ex) { /*Helper.ThrowException(ex);*/ }
        }

        private async void WallDisBatSavTog_Toggled(object sender, RoutedEventArgs e)
        {
            var splits = (await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Lockscreen.dat"))).Split('\n');
            splits[5] = WallDisBatSavTog.IsOn.ToString();

            await FileIO.WriteTextAsync(
                await Helper.localFolder.GetFileAsync("Lockscreen.dat"),
                string.Join("\n", splits)
            );

            await Helper.localFolder.CreateFileAsync("LockscreenBreak.txt", CreationCollisionOption.OpenIfExists);
        }

        private void SearchPressParaCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selected = SearchPressParaCombo.SelectedItem as ComboBoxItem;

            // Headers, "Default" or "None"
            if (!selected.IsHitTestVisible || selected.Content.ToString().Equals("None"))
                return;

            RegEdit.SetHKLMValue(
                "SYSTEM\\Input\\WEH\\Buttons\\WEHButton4\\Press", "AppParam",
                RegistryType.REG_SZ, selected.Content.ToString() + (SearchPressParaCombo.SelectedIndex >= 16 ? "" : "Page")
            );
        }

        private void SearchHoldParaCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selected = SearchHoldParaCombo.SelectedItem as ComboBoxItem;

            // Headers, "Default" or "None"
            if (!selected.IsHitTestVisible || selected.Content.ToString().Equals("None"))
                return;

            RegEdit.SetHKLMValue(
                "SYSTEM\\Input\\WEH\\Buttons\\WEHButton4\\PressAndHold", "AppParam",
                RegistryType.REG_SZ, selected.Content.ToString() + (SearchHoldParaCombo.SelectedIndex >= 16 ? "" : "Page")
            );
        }

        private void StartWallInterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = StartWallInterCombo.SelectedValue.ToString();

            if (selectedItem.Contains("Hours"))
                AppSettings.StartWallInterval = selectedItem.Replace(" Hours", "").ToInt32() * 60;
            else
                AppSettings.StartWallInterval = selectedItem.Replace(" Minutes", "").ToInt32();

            AppSettings.SaveSettings("StartWallInterItem", StartWallInterCombo.SelectedIndex);
        }
    }
}
