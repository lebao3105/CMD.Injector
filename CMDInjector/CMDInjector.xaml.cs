using CMDInjectorHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using XamlBrewer.Uwp.Controls;

namespace CMDInjector
{
    public sealed partial class CMDInjector : Page
    {
        bool buttonOnHold = false;

        public CMDInjector()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            Initialize();
        }

        private async void Initialize()
        {
            //RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\KeepWiFiOnSvc", "Start", RegistryType.REG_DWORD, "00000002");
            RegEdit.SetHKLMValueEx(
                "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "Path", RegistryType.REG_EXPAND_SZ,
                "%SystemRoot%\\system32;%SystemRoot%;%SystemDrive%\\Programs\\CommonFiles\\System;" +
                "%SystemDrive%\\wtt;%SystemDrive%\\data\\test\\bin;%SystemRoot%\\system32\\WindowsPowerShell\\v1.0;"
            );
            RegEdit.SetHKLMValue("SOFTWARE\\Microsoft\\SecurityManager\\PrincipalClasses\\PRINCIPAL_CLASS_TCB", "Directories", RegistryType.REG_MULTI_SZ, "C:\\ ");

            await Helper.WaitAppLaunch();
            bool isFirstLaunch = false;

            if (!"Startup.bat".IsAFileInSystem32())
            {
                FilesHelper.CopyToSystem32FromAppRoot("Contents\\BatchScripts", "Startup.bat", null);
            }

            if (Helper.currentVersion.IsGreaterThan(CMDInjectorHelper.Settings.InitialLaunch, '.'))
            {
                if (Helper.InjectedBatchVersion.ToInt32() < 3800)
                {
                    FilesHelper.CopyToSystem32FromAppRoot("Contents\\BatchScripts\\", "Startup.bat", null);
                }

                await Changelog.DisplayLog();
                CMDInjectorHelper.Settings.InitialLaunch = Helper.currentVersion;
                CMDInjectorHelper.Settings.TempInjection = true;
                CMDInjectorHelper.Settings.AskCapPermission = true;
            }

            if ((!HomeHelper.IsCMDInjected() && !"CMDInjector.dat".IsAFileInSystem32()) || !"WindowsPowerShell\\v1.0\\powershell.exe".IsAFileInSystem32())
            {
                var isInjected = await OperationInjection();
                if (isInjected)
                {
                    if (CMDInjectorHelper.Settings.FirstLaunch && !"CMDInjectorFirstLaunch.dat".IsAFileInSystem32()/* && build >= 14393*/)
                    {
                        isFirstLaunch = true;
                        CMDInjectorHelper.Settings.FirstLaunch.Toggle();

                        FilesHelper.CopyFile(Helper.localFolder.Path + "\\CMDInjector.dat", @"C:\Windows\System32\CMDInjectorFirstLaunch.dat");
                        var result = await Helper.MessageBox("A system reboot is required to initialize the App.", Helper.SoundHelper.Sound.Alert, "First Launch", "Cancel", true, "Reboot");
                        if (result == 0)
                            Helper.RebootSystem();
                    }
                }
                else
                {
                    ContentDialog contentDialog = new ContentDialog
                    {
                        Title = "CMD Injector",
                        Content = "Please restore the NDTKSvc in order to use this App.",
                        PrimaryButtonText = "How",
                        SecondaryButtonText = "Exit"
                    };
                    Helper.SoundHelper.PlaySound(Helper.SoundHelper.Sound.Alert);

                    if ((await contentDialog.ShowAsync()) != ContentDialogResult.Primary)
                    {
                        CoreApplication.Exit();
                    }
                    else
                    {
                        try
                        {
                            await Launcher.LaunchUriAsync(new Uri("https://www.google.com/search?q=How+to+interop+unlock+Windows+10+Mobile%3F"));
                        }
                        catch (Exception ex)
                        {
                            Helper.ThrowException(ex);
                        }
                        CoreApplication.Exit();
                        return;
                    }
                }
            }
            else
            {
                CMDInjectorHelper.Settings.FirstLaunch = false;
            }

            if (CMDInjectorHelper.Settings.TempInjection)
            {
                _ = OperationInjection();
                CMDInjectorHelper.Settings.TempInjection.Toggle();
            }

            if (CMDInjectorHelper.Settings.AskCapPermission && !isFirstLaunch && !File.Exists(@"C:\Windows\System32\CMDInjectorFirstLaunch.dat"))
            {
                try
                {
                    if (!await Helper.IsCapabilitiesAllowed())
                    {
                        await Helper.AskCapabilitiesPermission();
                        CMDInjectorHelper.Settings.AskCapPermission.Toggle();
                    }
                }
                catch (Exception ex)
                {
                    Helper.ThrowException(ex);
                }
            }
        }

        private async Task<bool> OperationInjection()
        {
            await Task.Run(async () =>
            {
                #region Files touches and copies
                await FileIO.WriteTextAsync(
                    await Globals.localFolder.CreateFileAsync("CMDInjector.dat", CreationCollisionOption.ReplaceExisting),
                    Globals.currentBatchVersion.ToString()
                );

                FilesHelper.CopyFile(Helper.localFolder.Path + "\\CMDInjector.dat", @"C:\Windows\System32\CMDInjectorTempSetup.dat");

                FilesHelper.CurrentFolder = "Contents\\BatchScripts";
                FilesHelper.CopyToSystem32FromAppRoot("MessageDialog.bat");

                if (!File.Exists(@"C:\Windows\System32\CMDInjector.bat"))
                {
                    FilesHelper.CopyToSystem32FromAppRoot("NonSystemWide.bat", "CMDInjector.bat");
                }

                FilesHelper.CopyToSystem32FromAppRoot("Setup.bat", "CMDInjectorSetup.bat");

                FilesHelper.CurrentFolder = "Contents\\Bootsh";
                FilesHelper.CopyToSystem32FromAppRoot("bootshsvc.dll");
                FilesHelper.CopyToSystem32FromAppRoot("bootshsvc.dll.mui", @"en-US\bootshsvc.dll.mui");
                FilesHelper.CopyToSystem32FromAppRoot("Contents\\Startup\\", "startup.bsc", "Boot\\startup.bsc");

                FilesHelper.CurrentFolder = "Contents\\ConsoleApps\\";
                FilesHelper.CopyToSystem32FromAppRoot("AppXTest.Common.Feature.DeployAppx.dll");
                FilesHelper.CopyToSystem32FromAppRoot("bcdedit.exe");
                FilesHelper.CopyToSystem32FromAppRoot("CheckNetIsolation.exe");
                FilesHelper.CopyToSystem32FromAppRoot("cmd.exe");
                FilesHelper.CopyToSystem32FromAppRoot("findstr.exe");
                FilesHelper.CopyToSystem32FromAppRoot("icacls.exe");
                FilesHelper.CopyToSystem32FromAppRoot("InputProcessorClient.dll");
                FilesHelper.CopyToSystem32FromAppRoot("MinDeployAppX.exe");
                FilesHelper.CopyToSystem32FromAppRoot("more.com");
                FilesHelper.CopyToSystem32FromAppRoot("PowerTool.exe");
                FilesHelper.CopyToSystem32FromAppRoot("reg.exe");
                FilesHelper.CopyToSystem32FromAppRoot("ScreenCapture.exe");
                FilesHelper.CopyToSystem32FromAppRoot("SendKeys.exe");
                FilesHelper.CopyToSystem32FromAppRoot("shutdown.exe");
                FilesHelper.CopyToSystem32FromAppRoot("sleep.exe");
                FilesHelper.CopyToSystem32FromAppRoot("sort.exe");
                FilesHelper.CopyToSystem32FromAppRoot("takeown.exe");
                FilesHelper.CopyToSystem32FromAppRoot("telnetd.exe");
                FilesHelper.CopyToSystem32FromAppRoot("TestNavigationApi.exe");
                FilesHelper.CopyToSystem32FromAppRoot("xcopy.exe");

                FilesHelper.CurrentFolder += "en-US";
                FilesHelper.CopyToSystem32FromAppRoot("bcdedit.exe.mui", "en-US\\bcdedit.exe.mui");
                FilesHelper.CopyToSystem32FromAppRoot("CheckNetIsolation.exe.mui", "en-US\\CheckNetIsolation.exe.mui");
                FilesHelper.CopyToSystem32FromAppRoot("cmd.exe.mui", "en-US\\cmd.exe.mui");
                FilesHelper.CopyToSystem32FromAppRoot("findstr.exe.mui", "en-US\\findstr.exe.mui");
                FilesHelper.CopyToSystem32FromAppRoot("ICacls.exe.mui", "en-US\\ICacls.exe.mui");
                FilesHelper.CopyToSystem32FromAppRoot("reg.exe.mui", "en-US\\reg.exe.mui");
                FilesHelper.CopyToSystem32FromAppRoot("sort.exe.mui", "en-US\\sort.exe.mui");
                FilesHelper.CopyToSystem32FromAppRoot("takeown.exe.mui", "en-US\\takeown.exe.mui");
                #endregion

                #region Modify the registry
                RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\Control\\CI", "UMCIAuditMode", RegistryType.REG_DWORD, "00000001");
                RegEdit.SetHKLMValue("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Svchost", "bootshsvc", RegistryType.REG_MULTI_SZ, "bootsh");

                RegEdit.GoToKey(RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrrentControlSet\\services\\BootSh");
                RegEdit.SetRegValue("Type", RegistryType.REG_DWORD, "00000010");
                RegEdit.SetRegValue("Start", RegistryType.REG_DWORD, "00000002");
                RegEdit.SetRegValue("ServiceSidType", RegistryType.REG_DWORD, "00000001");
                RegEdit.SetRegValue("ErrorControl", RegistryType.REG_DWORD, "00000001");
                RegEdit.SetRegValue("DisplayName", RegistryType.REG_SZ, "@bootshsvc.dll,-1");
                RegEdit.SetRegValue("Description", RegistryType.REG_SZ, "@bootshsvc.dll,-2");
                RegEdit.SetRegValue("ObjectName", RegistryType.REG_SZ, "LocalSystem");
                RegEdit.SetRegValue("DependOnService", RegistryType.REG_MULTI_SZ, "Afd lmhosts keyiso ");
                RegEdit.SetRegValue("RequiredPrivileges", RegistryType.REG_MULTI_SZ, "SeAssignPrimaryTokenPrivilege SeAuditPrivilege SeSecurityPrivilege SeChangeNotifyPrivilege SeCreateGlobalPrivilege SeDebugPrivilege SeImpersonatePrivilege SeIncreaseQuotaPrivilege SeTcbPrivilege SeBackupPrivilege SeRestorePrivilege SeShutdownPrivilege SeSystemProfilePrivilege SeSystemtimePrivilege SeManageVolumePrivilege SeCreatePagefilePrivilege SeCreatePermanentPrivilege SeCreateSymbolicLinkPrivilege SeIncreaseBasePriorityPrivilege SeIncreaseWorkingSetPrivilege SeLoadDriverPrivilege SeLockMemoryPrivilege SeProfileSingleProcessPrivilege SeSystemEnvironmentPrivilege SeTakeOwnershipPrivilege SeTimeZonePrivilege ");
                RegEdit.SetRegValueEx("ImagePath", RegistryType.REG_EXPAND_SZ, "%SystemRoot%\\system32\\svchost.exe -k Bootshsvc");
                RegEdit.SetRegValueEx("FailureActions", RegistryType.REG_BINARY, "80510100000000000000000003000000140000000100000060EA00000100000060EA00000000000000000000");

                RegEdit.GoToKey(RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrrentControlSet\\services\\BootSh\\Parameters");
                RegEdit.SetRegValueEx("ServiceDll", RegistryType.REG_EXPAND_SZ, "%SystemRoot%\\system32\\bootshsvc.dll");
                RegEdit.SetRegValue("ServiceDllUnloadOnStop", RegistryType.REG_DWORD, "00000001");
                /*RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\BootSh\\Parameters\\Commands", "Loopback", RegistryType.REG_SZ, "CheckNetIsolation.exe loopbackexempt -a -n=CMDInjector_kqyng60eng17c");
                RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\BootSh\\Parameters\\Commands", "Telnetd", RegistryType.REG_SZ, "start telnetd.exe cmd.exe 9999");*/
                #endregion
            });
            return HomeHelper.IsCMDInjected();
        }

        private Dictionary<string, int> NavigationIndexes = new Dictionary<string, int>
        {
            ["Home"] = 1, ["Terminal"] = 2, ["Startup"] = 3, ["PacMan"] = 4,
            ["Snapper"] = 5, ["BootConfig"] = 6, ["TweakBox"] = 7,
            ["Settings"] = 8, ["Help"] = 9, ["About"] = 10
        };

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (buttonOnHold)
            {
                buttonOnHold = false;
                return;
            }

            var button = sender as Button;
            var stackPanel = button.Content as StackPanel;
            var textBlock = stackPanel.Children[1] as TextBlock;
            var image = stackPanel.Children[0] as Image;
            var bitmapImage = image.Source as BitmapImage;

            Helper.pageNavigation.Invoke(NavigationIndexes[textBlock.Text], null);

            if (Helper.LocalSettingsHelper.LoadSettings("MenuTransition", true) && Globals.build >= 10572)
            {
                (Frame.Content as Page).OpenFromSplashScreen(Globals.rect, Globals.color, bitmapImage.UriSource);
            }
        }

        private async void Button_Holding(object sender, HoldingRoutedEventArgs e)
        {
            buttonOnHold = true;
            if (e.HoldingState != Windows.UI.Input.HoldingState.Started)
            {
                return;
            }

            var button = sender as Button;
            var stackPanel = button.Content as StackPanel;
            var textBlock = stackPanel.Children[1] as TextBlock;
            SecondaryTile CMDInjectorTile = null;
            var logoPath = new Uri($"ms-appx:///Assets/Icons/Menus/{textBlock.Text}MenuTileLogo.png");

            switch (textBlock.Text)
            {
                case "Terminal":
                    {
                        try
                        {
                            TextBlock terminalTextblock = new TextBlock
                            {
                                Text = "Argument (Optional)"
                            };

                            TextBox terminalTextBox = new TextBox
                            {
                                AcceptsReturn = false,
                                IsSpellCheckEnabled = false,
                                IsTextPredictionEnabled = false,
                                PlaceholderText = "echo Hello World from cookers!"
                            };

                            StackPanel terminalStackpanel = new StackPanel();
                            terminalStackpanel.Children.Add(terminalTextblock);
                            terminalStackpanel.Children.Add(terminalTextBox);

                            ContentDialog argumentTerm = new ContentDialog
                            {
                                Title = "Terminal",
                                IsSecondaryButtonEnabled = true,
                                PrimaryButtonText = "Pin",
                                SecondaryButtonText = "Cancel",
                                Content = terminalStackpanel
                            };

                            Helper.SoundHelper.PlaySound(Helper.SoundHelper.Sound.Alert);
                            if (await argumentTerm.ShowAsync() == ContentDialogResult.Primary)
                            {
                                if (string.IsNullOrEmpty(terminalTextBox.Text))
                                {
                                    CMDInjectorTile = new SecondaryTile(textBlock.Text + "PageID", textBlock.Text, textBlock.Text + "Page", logoPath, TileSize.Default);
                                }
                                else
                                {
                                    var cmd = TerminalHelper.EscapeSymbols(terminalTextBox.Text, false);
                                    CMDInjectorTile = new SecondaryTile(
                                        textBlock.Text + "PageID" + cmd.Replace(" ", "_"),
                                        textBlock.Text + $" ({terminalTextBox.Text})",
                                        textBlock.Text + "Page" + $" {cmd}",
                                        logoPath,
                                        TileSize.Default);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Helper.ThrowException(ex);
                        }
                        break;
                    }

                case "Home":
                case "Settings":
                case "Help":
                case "About":
                    {
                        CMDInjectorTile = new SecondaryTile(textBlock.Text + "PageID", textBlock.Text + " (CMD Injector)", textBlock.Text + "Page", logoPath, TileSize.Default);
                        break;
                    }

                default:
                    {
                        CMDInjectorTile = new SecondaryTile(textBlock.Text + "PageID", textBlock.Text, textBlock.Text + "Page", logoPath, TileSize.Default);
                        break;
                    }
            };

            CMDInjectorTile.VisualElements.ShowNameOnSquare150x150Logo = true;
            CMDInjectorTile.VisualElements.ShowNameOnWide310x150Logo = true;
            CMDInjectorTile.VisualElements.ShowNameOnSquare310x310Logo = true;
            await CMDInjectorTile.RequestCreateAsync();
        }
    }
}
