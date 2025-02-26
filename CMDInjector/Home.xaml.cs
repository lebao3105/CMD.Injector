using CMDInjectorHelper;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace CMDInjector
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Home : Page
    {
        bool isRootFrame = false;

        public Home()
        {
            InitializeComponent();

            try
            {
                /*if (build < 14393 && Helper.IsSecureBootPolicyInstalled() && !File.Exists(@"C:\Windows\System32\CMDInjectorVersion.dat"))
                {
                    UnlockBLBox.Visibility = Visibility.Visible;
                }*/

                InjectionTypeCombo.SelectedIndex = File.Exists(@"C:\Windows\servicing\Packages\FadilFadz.CMDInjector.Permanent~628844477771337a~arm~~1.0.0.0.mum").ToInt();

                if (File.Exists(@"C:\Windows\System32\CMDInjector.dat") || File.Exists(@"C:\Windows\System32\CMDUninjector.dat"))
                {
                    InjectBtn.IsEnabled = UnInjectBtn.IsEnabled = false;

                    if (File.Exists(@"C:\Windows\System32\CMDInjector.dat"))
                    {
                        InjectBtn.Content = "Injected";
                    }
                    else
                    {
                        UnInjectBtn.Content = "Un-Injected";
                    }

                    reInjectionReboot.Text = "A previous un-injection reboot is pending. Please reboot your device to apply the changes.";

                    UnInjectBtn.Visibility = Visibility.Visible;
                    reInjectionReboot.Visibility = Visibility.Visible;
                }
                else if (HomeHelper.IsCMDInjected() && File.Exists(@"C:\Windows\System32\CMDInjectorVersion.dat"))
                {
                    int injectedBatchVer = Globals.InjectedBatchVersion.ToInt32();

                    if (injectedBatchVer > Globals.currentBatchVersion)
                    {
                        InjectBtn.IsEnabled = false;
                        InjectBtn.Content = "Injected";
                        reInjectionNote.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        if (injectedBatchVer < Helper.currentBatchVersion)
                        {
                            reInjectionBox.Visibility = Visibility.Visible;
                        }
                        InjectBtn.Content = "Re-Inject";
                    }
                    UnInjectBtn.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex) { Helper.ThrowException(ex); }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter != null)
            {
                switch (e.Parameter.ToString())
                {
                    case "HomePage":
                        isRootFrame = true;
                        break;

                    case "InjectCMD=Re-Inject;ReinjectionRequired":
                        {
                            while (true)
                            {
                                await Task.Delay(300);
                                if (Globals.userVerified)
                                {
                                    break;
                                }
                            }
                            InjectBtn_Click(null, null);
                            break;
                        }
                }
            }
        }

        private async Task Reboot()
        {
            try
            {
                var result = await Helper.MessageBox(
                    "To apply the changes made, you have to reboot your device.",
                    Helper.SoundHelper.Sound.Alert, "Reboot Required", "Reboot later", true, "Reboot now");

                if (result == 0)
                {
                    Helper.RebootSystem();
                }
            }
            catch (Exception ex) { Helper.ThrowException(ex); }
        }

        private async void InjectBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                /*if (UnlockBLBox.Visibility == Visibility.Visible)
                {
                    MessageDialog showDialog = new MessageDialog("Are you sure you want to inject?", "Confirmation");
                    showDialog.Commands.Add(new UICommand("Yes")
                    {
                        Id = 0
                    });
                    showDialog.Commands.Add(new UICommand("No")
                    {
                        Id = 1
                    });
                    showDialog.DefaultCommandIndex = 0;
                    showDialog.CancelCommandIndex = 1;
                    var result = await showDialog.ShowAsync();
                    if ((int)result.Id == 1)
                    {
                        return;
                    }
                }*/
                InjectBtn.IsEnabled = false;
                UnInjectBtn.IsEnabled = false;
                InjectBtn.Content = "Injecting";
                await Task.Run(async () =>
                {
                    if (Helper.build < 14393)
                    {
                        //CommonHelper.CopyFile(CommonHelper.installFolder.Path + "\\Contents\\Drivers\\PatchedSecMgr.sys", @"C:\Windows\System32\Drivers\SecMgr.sys");

                        if (File.Exists(@"C:\Windows\System32\CMDInjectorVersion.dat"))
                        {
                            if (Convert.ToInt32(Helper.InjectedBatchVersion) <= 3550)
                            {
                                Helper.CopyFromAppRoot("Contents\\Drivers\\OriginalSecMgr.sys", @"C:\Windows\System32\Drivers\SecMgr.sys");
                            }
                        }
                    }
                    await FileIO.WriteTextAsync(await Helper.localFolder.CreateFileAsync("CMDInjector.dat", CreationCollisionOption.ReplaceExisting), Helper.currentBatchVersion.ToString());
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if (InjectionTypeCombo.SelectedIndex == 0)
                        {
                            Helper.CopyFile(Helper.localFolder.Path + "\\CMDInjector.dat", @"C:\Windows\System32\CMDInjectorTemporary.dat");
                        }
                        else
                        {
                            Helper.CopyFile(Helper.localFolder.Path + "\\CMDInjector.dat", @"C:\Windows\System32\CMDInjectorPermanent.dat");
                        }
                    });

                    Helper.CopyFile(Helper.localFolder.Path + "\\CMDInjector.dat", @"C:\Windows\System32\CMDInjector.dat");
                    Helper.CopyFile(Helper.localFolder.Path + "\\CMDInjector.dat", @"C:\Windows\System32\CMDInjectorVersion.dat");
                    Helper.CopyFromAppRoot("Contents\\BatchScripts\\MessageDialog.bat", @"C:\Windows\System32\MessageDialog.bat");
                    Helper.CopyFromAppRoot("Contents\\BatchScripts\\SystemWide.bat", @"C:\Windows\System32\CMDInjector.bat");
                    Helper.CopyFromAppRoot("Contents\\BatchScripts\\Setup.bat", @"C:\Windows\System32\CMDInjectorSetup.bat");
                    Helper.CopyFromAppRoot("Contents\\Bootsh\\bootshsvc.dll", @"C:\Windows\System32\bootshsvc.dll");
                    Helper.CopyFromAppRoot("Contents\\Bootsh\\bootshsvc.dll.mui", @"C:\Windows\System32\en-US\bootshsvc.dll.mui");
                    Helper.CopyFromAppRoot("Contents\\Startup\\startup.bsc", @"C:\Windows\System32\Boot\startup.bsc");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\attrib.exe.mui", @"C:\Windows\System32\en-US\attrib.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\bcdboot.exe.mui", @"C:\Windows\System32\en-US\bcdboot.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\bcdedit.exe.mui", @"C:\Windows\System32\en-US\bcdedit.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\CheckNetIsolation.exe.mui", @"C:\Windows\System32\en-US\CheckNetIsolation.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\cmd.exe.mui", @"C:\Windows\System32\en-US\cmd.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\cmdkey.exe.mui", @"C:\Windows\System32\en-US\cmdkey.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\cscript.exe.mui", @"C:\Windows\System32\en-US\cscript.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\Dism.exe.mui", @"C:\Windows\System32\en-US\Dism.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\find.exe.mui", @"C:\Windows\System32\en-US\find.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\findstr.exe.mui", @"C:\Windows\System32\en-US\findstr.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\finger.exe.mui", @"C:\Windows\System32\en-US\finger.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\ftp.exe.mui", @"C:\Windows\System32\en-US\ftp.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\help.exe.mui", @"C:\Windows\System32\en-US\help.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\hostname.exe.mui", @"C:\Windows\System32\en-US\hostname.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\ICacls.exe.mui", @"C:\Windows\System32\en-US\ICacls.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\ipconfig.exe.mui", @"C:\Windows\System32\en-US\ipconfig.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\label.exe.mui", @"C:\Windows\System32\en-US\label.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\logman.exe.mui", @"C:\Windows\System32\en-US\logman.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\mountvol.exe.mui", @"C:\Windows\System32\en-US\mountvol.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\neth.dll.mui", @"C:\Windows\System32\en-US\neth.dll.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\netsh.exe.mui", @"C:\Windows\System32\en-US\netsh.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\nslookup.exe.mui", @"C:\Windows\System32\en-US\nslookup.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\ping.exe.mui", @"C:\Windows\System32\en-US\ping.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\recover.exe.mui", @"C:\Windows\System32\en-US\recover.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\reg.exe.mui", @"C:\Windows\System32\en-US\reg.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\regsvr32.exe.mui", @"C:\Windows\System32\en-US\regsvr32.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\replace.exe.mui", @"C:\Windows\System32\en-US\replace.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\sc.exe.mui", @"C:\Windows\System32\en-US\sc.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\setx.exe.mui", @"C:\Windows\System32\en-US\setx.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\sort.exe.mui", @"C:\Windows\System32\en-US\sort.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\takeown.exe.mui", @"C:\Windows\System32\en-US\takeown.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\tzutil.exe.mui", @"C:\Windows\System32\en-US\tzutil.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\VaultCmd.exe.mui", @"C:\Windows\System32\en-US\VaultCmd.exe.mui");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\accesschk.exe", @"C:\Windows\System32\accesschk.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\AppXTest.Common.Feature.DeployAppx.dll", @"C:\Windows\System32\AppXTest.Common.Feature.DeployAppx.dll");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\attrib.exe", @"C:\Windows\System32\attrib.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\bcdboot.exe", @"C:\Windows\System32\bcdboot.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\bcdedit.exe", @"C:\Windows\System32\bcdedit.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\certmgr.exe", @"C:\Windows\System32\certmgr.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\CheckNetIsolation.exe", @"C:\Windows\System32\CheckNetIsolation.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\chkdsk.exe", @"C:\Windows\System32\chkdsk.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\cmd.exe", @"C:\Windows\System32\cmd.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\cmdkey.exe", @"C:\Windows\System32\cmdkey.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\cscript.exe", @"C:\Windows\System32\cscript.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\dcopy.exe", @"C:\Windows\System32\dcopy.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\depends.exe", @"C:\Windows\System32\depends.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\DeployAppx.exe", @"C:\Windows\System32\DeployAppx.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\DeployUtil.exe", @"C:\Windows\System32\DeployUtil.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\devcon.exe", @"C:\Windows\System32\devcon.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\DevToolsLauncher.exe", @"C:\Windows\System32\DevToolsLauncher.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\DIALTESTWP8.exe", @"C:\Windows\System32\DIALTESTWP8.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\Dism.exe", @"C:\Windows\System32\Dism.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\fc.exe", @"C:\Windows\System32\fc.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\find.exe", @"C:\Windows\System32\find.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\findstr.exe", @"C:\Windows\System32\findstr.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\finger.exe", @"C:\Windows\System32\finger.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\FolderPermissions.exe", @"C:\Windows\System32\FolderPermissions.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\format.com", @"C:\Windows\System32\format.com");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\ftp.exe", @"C:\Windows\System32\ftp.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\ftpd.exe", @"C:\Windows\System32\ftpd.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\FveEnable.exe", @"C:\Windows\System32\FveEnable.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\gbot.exe", @"C:\Windows\System32\gbot.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\gse.dll", @"C:\Windows\System32\gse.dll");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\help.exe", @"C:\Windows\System32\help.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\HOSTNAME.EXE", @"C:\Windows\System32\HOSTNAME.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\icacls.exe", @"C:\Windows\System32\icacls.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\imagex.exe", @"C:\Windows\System32\imagex.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\InputProcessorClient.dll", @"C:\Windows\System32\InputProcessorClient.dll");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\IoTSettings.exe", @"C:\Windows\System32\IoTSettings.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\IoTShell.exe", @"C:\Windows\System32\IoTShell.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\iotstartup.exe", @"C:\Windows\System32\iotstartup.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\ipconfig.exe", @"C:\Windows\System32\ipconfig.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\kill.exe", @"C:\Windows\System32\kill.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\label.exe", @"C:\Windows\System32\label.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\logman.exe", @"C:\Windows\System32\logman.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\MinDeployAppX.exe", @"C:\Windows\System32\MinDeployAppX.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\minshutdown.exe", @"C:\Windows\System32\minshutdown.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\more.com", @"C:\Windows\System32\more.com");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\mountvol.exe", @"C:\Windows\System32\mountvol.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\msnap.exe", @"C:\Windows\System32\msnap.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\mwkdbgctrl.exe", @"C:\Windows\System32\mwkdbgctrl.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\net.exe", @"C:\Windows\System32\net.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\net1.exe", @"C:\Windows\System32\net1.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\neth.dll", @"C:\Windows\System32\neth.dll");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\neth.exe", @"C:\Windows\System32\neth.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\netsh.exe", @"C:\Windows\System32\netsh.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\nslookup.exe", @"C:\Windows\System32\nslookup.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\pacman_ierror.dll", @"C:\Windows\System32\pacman_ierror.dll");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\pacmanerr.dll", @"C:\Windows\System32\pacmanerr.dll");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\ping.exe", @"C:\Windows\System32\ping.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\PMTestErrorLookup.exe", @"C:\Windows\System32\PMTestErrorLookup.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\PowerTool.exe", @"C:\Windows\System32\PowerTool.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\ProvisioningTool.exe", @"C:\Windows\System32\ProvisioningTool.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\recover.exe", @"C:\Windows\System32\recover.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\reg.exe", @"C:\Windows\System32\reg.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\regsvr32.exe", @"C:\Windows\System32\regsvr32.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\replace.exe", @"C:\Windows\System32\replace.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\sc.exe", @"C:\Windows\System32\sc.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\ScreenCapture.exe", @"C:\Windows\System32\ScreenCapture.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\SendKeys.exe", @"C:\Windows\System32\SendKeys.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\setbootoption.exe", @"C:\Windows\System32\setbootoption.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\setcomputername.exe", @"C:\Windows\System32\setcomputername.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\SetDisplayResolution.exe", @"C:\Windows\System32\SetDisplayResolution.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\setx.exe", @"C:\Windows\System32\setx.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\sfpdisable.exe", @"C:\Windows\System32\sfpdisable.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\shutdown.exe", @"C:\Windows\System32\shutdown.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\SirepController.exe", @"C:\Windows\System32\SirepController.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\sleep.exe", @"C:\Windows\System32\sleep.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\sort.exe", @"C:\Windows\System32\sort.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\takeown.exe", @"C:\Windows\System32\takeown.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\TaskSchUtil.exe", @"C:\Windows\System32\TaskSchUtil.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\telnetd.exe", @"C:\Windows\System32\telnetd.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\TestDeploymentInfo.dll", @"C:\Windows\System32\TestDeploymentInfo.dll");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\TestNavigationApi.exe", @"C:\Windows\System32\TestNavigationApi.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\TH.exe", @"C:\Windows\System32\TH.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\tlist.exe", @"C:\Windows\System32\tlist.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\TouchRecorder.exe", @"C:\Windows\System32\TouchRecorder.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\tracelog.exe", @"C:\Windows\System32\tracelog.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\tree.com", @"C:\Windows\System32\tree.com");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\tzutil.exe", @"C:\Windows\System32\tzutil.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\VaultCmd.exe", @"C:\Windows\System32\VaultCmd.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\WPConPlatDev.exe", @"C:\Windows\System32\WPConPlatDev.exe");
                    Helper.CopyFromAppRoot("Contents\\ConsoleApps\\xcopy.exe", @"C:\Windows\System32\xcopy.exe");
                    Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Control\\CI", "UMCIAuditMode", Helper.RegistryHelper.RegistryType.REG_DWORD, "00000001");
                    //Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\KeepWiFiOnSvc", "Start", Helper.RegistryHelper.RegistryType.REG_DWORD, "00000002");
                    Helper.RegistryHelper.SetRegValueEx(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "Path", Helper.RegistryHelper.RegistryType.REG_EXPAND_SZ, "%SystemRoot%\\system32;%SystemRoot%;%SystemDrive%\\Programs\\CommonFiles\\System;%SystemDrive%\\wtt;%SystemDrive%\\data\\test\\bin;%SystemRoot%\\system32\\WindowsPowerShell\\v1.0;");
                    Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SOFTWARE\\Microsoft\\SecurityManager\\PrincipalClasses\\PRINCIPAL_CLASS_TCB", "Directories", Helper.RegistryHelper.RegistryType.REG_MULTI_SZ, "C:\\ ");
                    Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh", "Type", Helper.RegistryHelper.RegistryType.REG_DWORD, "00000010");
                    Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh", "Start", Helper.RegistryHelper.RegistryType.REG_DWORD, "00000002");
                    Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh", "ServiceSidType", Helper.RegistryHelper.RegistryType.REG_DWORD, "00000001");
                    Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh", "ErrorControl", Helper.RegistryHelper.RegistryType.REG_DWORD, "00000001");
                    Helper.RegistryHelper.SetRegValueEx(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh", "ImagePath", Helper.RegistryHelper.RegistryType.REG_EXPAND_SZ, "%SystemRoot%\\system32\\svchost.exe -k Bootshsvc");
                    Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh", "DisplayName", Helper.RegistryHelper.RegistryType.REG_SZ, "@bootshsvc.dll,-1");
                    Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh", "Description", Helper.RegistryHelper.RegistryType.REG_SZ, "@bootshsvc.dll,-2");
                    Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh", "ObjectName", Helper.RegistryHelper.RegistryType.REG_SZ, "LocalSystem");
                    Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh", "DependOnService", Helper.RegistryHelper.RegistryType.REG_MULTI_SZ, "Afd lmhosts keyiso ");
                    Helper.RegistryHelper.SetRegValueEx(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh", "FailureActions", Helper.RegistryHelper.RegistryType.REG_BINARY, "80510100000000000000000003000000140000000100000060EA00000100000060EA00000000000000000000");
                    Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh", "RequiredPrivileges", Helper.RegistryHelper.RegistryType.REG_MULTI_SZ, "SeAssignPrimaryTokenPrivilege SeAuditPrivilege SeSecurityPrivilege SeChangeNotifyPrivilege SeCreateGlobalPrivilege SeDebugPrivilege SeImpersonatePrivilege SeIncreaseQuotaPrivilege SeTcbPrivilege SeBackupPrivilege SeRestorePrivilege SeShutdownPrivilege SeSystemProfilePrivilege SeSystemtimePrivilege SeManageVolumePrivilege SeCreatePagefilePrivilege SeCreatePermanentPrivilege SeCreateSymbolicLinkPrivilege SeIncreaseBasePriorityPrivilege SeIncreaseWorkingSetPrivilege SeLoadDriverPrivilege SeLockMemoryPrivilege SeProfileSingleProcessPrivilege SeSystemEnvironmentPrivilege SeTakeOwnershipPrivilege SeTimeZonePrivilege ");
                    Helper.RegistryHelper.SetRegValueEx(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh\\Parameters", "ServiceDll", Helper.RegistryHelper.RegistryType.REG_EXPAND_SZ, "%SystemRoot%\\system32\\bootshsvc.dll");
                    Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh\\Parameters", "ServiceDllUnloadOnStop", Helper.RegistryHelper.RegistryType.REG_DWORD, "00000001");
                    /*Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh\\Parameters\\Commands", "Loopback", 1, "CheckNetIsolation.exe loopbackexempt -a -n=CMDInjector_kqyng60eng17c");
                    Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\services\\BootSh\\Parameters\\Commands", "Telnetd", 1, "telnetd.exe cmd.exe 9999");*/
                    Helper.RegistryHelper.SetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Svchost", "bootshsvc", Helper.RegistryHelper.RegistryType.REG_MULTI_SZ, "bootsh ");
                });
                reInjectionReboot.Text = "An injection reboot is pending. Please reboot your device as soon as possible to apply the changes.";
                reInjectionBox.Visibility = Visibility.Collapsed;
                reInjectionReboot.Visibility = Visibility.Visible;
                InjectBtn.Content = "Injected";
                ToastNotificationManager.History.Remove("Re-InjectTag");
                _ = Reboot();
            }
            catch (Exception ex) { Helper.ThrowException(ex); }
        }

        private void FaqHelp_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            if (isRootFrame)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                if (rootFrame == null)
                {
                    rootFrame = new Frame();
                }
                rootFrame.Navigate(typeof(Help));
            }
            else
            {
                Helper.pageNavigation.Invoke(9, null);
            }
        }

        private async void InjectionInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            var message = "Two types of injections are available:\n" +
                    "1. Temporary injection that wipes out on hard reset.\n" +
                    "2. Permanent injection that remains even after hard reset.\n\n" +
                    "Permanent injection makes the CMD and Startups work even after hard-resets. Registry changes and more will also be preserved.\n\n" +
                    "You can switch between the injection type any time by a re-inject. Builds below 14393 still require a re-injection even if the CMD files persists.";

            if (Helper.build < 15063)
            {
                _ = Helper.MessageBox(message, Helper.SoundHelper.Sound.Alert, "Info");
            }
            else
            {
                ContentDialog contentDialog = new ContentDialog
                {
                    Title = "Info",
                    Content = message,
                    CloseButtonText = "Close"
                };
                Helper.SoundHelper.PlaySound(Helper.SoundHelper.Sound.Alert);
                await contentDialog.ShowAsync();
            }
        }

        private async void UnInjectBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = await Helper.MessageBox("Are you sure you want to un-inject?\n\nThis will remove CMD from the system, but the App will still remain functional.", Helper.SoundHelper.Sound.Alert, "Confirmation", "No", true, "Yes");
            if (result == 0)
            {
                InjectBtn.IsEnabled = false;
                UnInjectBtn.IsEnabled = false;
                await FileIO.WriteTextAsync(await Helper.localFolder.CreateFileAsync("CMDInjector.dat", CreationCollisionOption.ReplaceExisting), Helper.currentBatchVersion.ToString());

                Helper.CopyFile(Helper.localFolder.Path + "\\CMDInjector.dat", @"C:\Windows\System32\CMDUninjector.dat");
                Helper.CopyFromAppRoot("Contents\\BatchScripts\\NonSystemWide.bat", @"C:\Windows\System32\CMDInjector.bat");
                Helper.CopyFromAppRoot("Contents\\InjectionType\\TemporaryInjection.reg", @"C:\Windows\System32\TemporaryInjection.reg");
                Helper.RegistryHelper.SetRegValueEx(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "Path", Helper.RegistryHelper.RegistryType.REG_EXPAND_SZ, $"%SystemRoot%\\system32;%SystemRoot%;%SystemDrive%\\Programs\\CommonFiles\\System;%SystemDrive%\\wtt;%SystemDrive%\\data\\test\\bin;%SystemRoot%\\system32\\WindowsPowerShell\\v1.0;{Helper.localFolder.Path}\\CMDInjector;");

                UnInjectBtn.Content = "Un-Injected";

                reInjectionReboot.Text = "An un-injection reboot is pending. Please reboot your device as soon as possible to apply the changes.";
                reInjectionBox.Visibility = Visibility.Collapsed;
                reInjectionNote.Visibility = Visibility.Collapsed;
                reInjectionReboot.Visibility = Visibility.Visible;
                _ = Reboot();
            }
        }
    }
}
