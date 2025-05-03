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
                    UnlockBLBox.Visible();
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

                    UnInjectBtn.Visible();
                    reInjectionReboot.Visible();
                }
                else if (HomeHelper.IsCMDInjected() && File.Exists(@"C:\Windows\System32\CMDInjectorVersion.dat"))
                {
                    int injectedBatchVer = Globals.InjectedBatchVersion.ToInt32();

                    if (injectedBatchVer > Globals.currentBatchVersion)
                    {
                        InjectBtn.IsEnabled = false;
                        InjectBtn.Content = "Injected";
                        reInjectionNote.Visible();
                    }
                    else
                    {
                        if (injectedBatchVer < Helper.currentBatchVersion)
                        {
                            reInjectionBox.Visible();
                        }
                        InjectBtn.Content = "Re-Inject";
                    }
                    UnInjectBtn.Visible();
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
                        //CommonFilesHelper.CopyFile(CommonHelper.installFolder.Path + "\\Contents\\Drivers\\PatchedSecMgr.sys", @"C:\Windows\System32\Drivers\SecMgr.sys");

                        if (File.Exists(@"C:\Windows\System32\CMDInjectorVersion.dat"))
                        {
                            if (Convert.ToInt32(Helper.InjectedBatchVersion) <= 3550)
                            {
                                FilesHelper.CopyFromAppRoot("Contents\\Drivers\\OriginalSecMgr.sys", @"C:\Windows\System32\Drivers\SecMgr.sys");
                            }
                        }
                    }
                    await FileIO.WriteTextAsync(await Helper.localFolder.CreateFileAsync("CMDInjector.dat", CreationCollisionOption.ReplaceExisting), Helper.currentBatchVersion.ToString());
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if (InjectionTypeCombo.SelectedIndex == 0)
                        {
                            FilesHelper.CopyFile(Helper.localFolder.Path + "\\CMDInjector.dat", @"C:\Windows\System32\CMDInjectorTemporary.dat");
                        }
                        else
                        {
                            FilesHelper.CopyFile(Helper.localFolder.Path + "\\CMDInjector.dat", @"C:\Windows\System32\CMDInjectorPermanent.dat");
                        }
                    });

                    FilesHelper.CopyFile(Helper.localFolder.Path + "\\CMDInjector.dat", @"C:\Windows\System32\CMDInjector.dat");
                    FilesHelper.CopyFile(Helper.localFolder.Path + "\\CMDInjector.dat", @"C:\Windows\System32\CMDInjectorVersion.dat");
                    FilesHelper.CopyFromAppRoot("Contents\\BatchScripts\\MessageDialog.bat", @"C:\Windows\System32\MessageDialog.bat");
                    FilesHelper.CopyFromAppRoot("Contents\\BatchScripts\\SystemWide.bat", @"C:\Windows\System32\CMDInjector.bat");
                    FilesHelper.CopyFromAppRoot("Contents\\BatchScripts\\Setup.bat", @"C:\Windows\System32\CMDInjectorSetup.bat");
                    FilesHelper.CopyFromAppRoot("Contents\\Bootsh\\bootshsvc.dll", @"C:\Windows\System32\bootshsvc.dll");
                    FilesHelper.CopyFromAppRoot("Contents\\Bootsh\\bootshsvc.dll.mui", @"C:\Windows\System32\en-US\bootshsvc.dll.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\Startup\\startup.bsc", @"C:\Windows\System32\Boot\startup.bsc");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\attrib.exe.mui", @"C:\Windows\System32\en-US\attrib.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\bcdboot.exe.mui", @"C:\Windows\System32\en-US\bcdboot.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\bcdedit.exe.mui", @"C:\Windows\System32\en-US\bcdedit.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\CheckNetIsolation.exe.mui", @"C:\Windows\System32\en-US\CheckNetIsolation.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\cmd.exe.mui", @"C:\Windows\System32\en-US\cmd.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\cmdkey.exe.mui", @"C:\Windows\System32\en-US\cmdkey.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\cscript.exe.mui", @"C:\Windows\System32\en-US\cscript.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\Dism.exe.mui", @"C:\Windows\System32\en-US\Dism.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\find.exe.mui", @"C:\Windows\System32\en-US\find.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\findstr.exe.mui", @"C:\Windows\System32\en-US\findstr.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\finger.exe.mui", @"C:\Windows\System32\en-US\finger.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\ftp.exe.mui", @"C:\Windows\System32\en-US\ftp.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\help.exe.mui", @"C:\Windows\System32\en-US\help.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\hostname.exe.mui", @"C:\Windows\System32\en-US\hostname.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\ICacls.exe.mui", @"C:\Windows\System32\en-US\ICacls.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\ipconfig.exe.mui", @"C:\Windows\System32\en-US\ipconfig.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\label.exe.mui", @"C:\Windows\System32\en-US\label.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\logman.exe.mui", @"C:\Windows\System32\en-US\logman.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\mountvol.exe.mui", @"C:\Windows\System32\en-US\mountvol.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\neth.dll.mui", @"C:\Windows\System32\en-US\neth.dll.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\netsh.exe.mui", @"C:\Windows\System32\en-US\netsh.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\nslookup.exe.mui", @"C:\Windows\System32\en-US\nslookup.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\ping.exe.mui", @"C:\Windows\System32\en-US\ping.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\recover.exe.mui", @"C:\Windows\System32\en-US\recover.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\reg.exe.mui", @"C:\Windows\System32\en-US\reg.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\regsvr32.exe.mui", @"C:\Windows\System32\en-US\regsvr32.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\replace.exe.mui", @"C:\Windows\System32\en-US\replace.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\sc.exe.mui", @"C:\Windows\System32\en-US\sc.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\setx.exe.mui", @"C:\Windows\System32\en-US\setx.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\sort.exe.mui", @"C:\Windows\System32\en-US\sort.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\takeown.exe.mui", @"C:\Windows\System32\en-US\takeown.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\tzutil.exe.mui", @"C:\Windows\System32\en-US\tzutil.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\en-US\\VaultCmd.exe.mui", @"C:\Windows\System32\en-US\VaultCmd.exe.mui");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\accesschk.exe", @"C:\Windows\System32\accesschk.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\AppXTest.Common.Feature.DeployAppx.dll", @"C:\Windows\System32\AppXTest.Common.Feature.DeployAppx.dll");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\attrib.exe", @"C:\Windows\System32\attrib.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\bcdboot.exe", @"C:\Windows\System32\bcdboot.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\bcdedit.exe", @"C:\Windows\System32\bcdedit.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\certmgr.exe", @"C:\Windows\System32\certmgr.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\CheckNetIsolation.exe", @"C:\Windows\System32\CheckNetIsolation.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\chkdsk.exe", @"C:\Windows\System32\chkdsk.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\cmd.exe", @"C:\Windows\System32\cmd.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\cmdkey.exe", @"C:\Windows\System32\cmdkey.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\cscript.exe", @"C:\Windows\System32\cscript.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\dcopy.exe", @"C:\Windows\System32\dcopy.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\depends.exe", @"C:\Windows\System32\depends.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\DeployAppx.exe", @"C:\Windows\System32\DeployAppx.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\DeployUtil.exe", @"C:\Windows\System32\DeployUtil.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\devcon.exe", @"C:\Windows\System32\devcon.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\DevToolsLauncher.exe", @"C:\Windows\System32\DevToolsLauncher.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\DIALTESTWP8.exe", @"C:\Windows\System32\DIALTESTWP8.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\Dism.exe", @"C:\Windows\System32\Dism.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\fc.exe", @"C:\Windows\System32\fc.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\find.exe", @"C:\Windows\System32\find.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\findstr.exe", @"C:\Windows\System32\findstr.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\finger.exe", @"C:\Windows\System32\finger.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\FolderPermissions.exe", @"C:\Windows\System32\FolderPermissions.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\format.com", @"C:\Windows\System32\format.com");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\ftp.exe", @"C:\Windows\System32\ftp.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\ftpd.exe", @"C:\Windows\System32\ftpd.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\FveEnable.exe", @"C:\Windows\System32\FveEnable.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\gbot.exe", @"C:\Windows\System32\gbot.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\gse.dll", @"C:\Windows\System32\gse.dll");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\help.exe", @"C:\Windows\System32\help.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\HOSTNAME.EXE", @"C:\Windows\System32\HOSTNAME.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\icacls.exe", @"C:\Windows\System32\icacls.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\imagex.exe", @"C:\Windows\System32\imagex.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\InputProcessorClient.dll", @"C:\Windows\System32\InputProcessorClient.dll");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\IoTAppSettings.exe", @"C:\Windows\System32\IoTAppSettings.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\IoTShell.exe", @"C:\Windows\System32\IoTShell.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\iotstartup.exe", @"C:\Windows\System32\iotstartup.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\ipconfig.exe", @"C:\Windows\System32\ipconfig.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\kill.exe", @"C:\Windows\System32\kill.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\label.exe", @"C:\Windows\System32\label.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\logman.exe", @"C:\Windows\System32\logman.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\MinDeployAppX.exe", @"C:\Windows\System32\MinDeployAppX.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\minshutdown.exe", @"C:\Windows\System32\minshutdown.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\more.com", @"C:\Windows\System32\more.com");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\mountvol.exe", @"C:\Windows\System32\mountvol.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\msnap.exe", @"C:\Windows\System32\msnap.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\mwkdbgctrl.exe", @"C:\Windows\System32\mwkdbgctrl.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\net.exe", @"C:\Windows\System32\net.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\net1.exe", @"C:\Windows\System32\net1.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\neth.dll", @"C:\Windows\System32\neth.dll");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\neth.exe", @"C:\Windows\System32\neth.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\netsh.exe", @"C:\Windows\System32\netsh.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\nslookup.exe", @"C:\Windows\System32\nslookup.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\pacman_ierror.dll", @"C:\Windows\System32\pacman_ierror.dll");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\pacmanerr.dll", @"C:\Windows\System32\pacmanerr.dll");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\ping.exe", @"C:\Windows\System32\ping.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\PMTestErrorLookup.exe", @"C:\Windows\System32\PMTestErrorLookup.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\PowerTool.exe", @"C:\Windows\System32\PowerTool.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\ProvisioningTool.exe", @"C:\Windows\System32\ProvisioningTool.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\recover.exe", @"C:\Windows\System32\recover.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\reg.exe", @"C:\Windows\System32\reg.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\regsvr32.exe", @"C:\Windows\System32\regsvr32.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\replace.exe", @"C:\Windows\System32\replace.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\sc.exe", @"C:\Windows\System32\sc.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\ScreenCapture.exe", @"C:\Windows\System32\ScreenCapture.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\SendKeys.exe", @"C:\Windows\System32\SendKeys.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\setbootoption.exe", @"C:\Windows\System32\setbootoption.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\setcomputername.exe", @"C:\Windows\System32\setcomputername.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\SetDisplayResolution.exe", @"C:\Windows\System32\SetDisplayResolution.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\setx.exe", @"C:\Windows\System32\setx.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\sfpdisable.exe", @"C:\Windows\System32\sfpdisable.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\shutdown.exe", @"C:\Windows\System32\shutdown.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\SirepController.exe", @"C:\Windows\System32\SirepController.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\sleep.exe", @"C:\Windows\System32\sleep.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\sort.exe", @"C:\Windows\System32\sort.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\takeown.exe", @"C:\Windows\System32\takeown.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\TaskSchUtil.exe", @"C:\Windows\System32\TaskSchUtil.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\telnetd.exe", @"C:\Windows\System32\telnetd.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\TestDeploymentInfo.dll", @"C:\Windows\System32\TestDeploymentInfo.dll");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\TestNavigationApi.exe", @"C:\Windows\System32\TestNavigationApi.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\TH.exe", @"C:\Windows\System32\TH.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\tlist.exe", @"C:\Windows\System32\tlist.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\TouchRecorder.exe", @"C:\Windows\System32\TouchRecorder.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\tracelog.exe", @"C:\Windows\System32\tracelog.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\tree.com", @"C:\Windows\System32\tree.com");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\tzutil.exe", @"C:\Windows\System32\tzutil.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\VaultCmd.exe", @"C:\Windows\System32\VaultCmd.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\WPConPlatDev.exe", @"C:\Windows\System32\WPConPlatDev.exe");
                    FilesHelper.CopyFromAppRoot("Contents\\ConsoleApps\\xcopy.exe", @"C:\Windows\System32\xcopy.exe");
                    RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\Control\\CI", "UMCIAuditMode", RegistryType.REG_DWORD, "00000001");
                    //RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\KeepWiFiOnSvc", "Start", RegistryType.REG_DWORD, "00000002");
                    RegEdit.SetHKLMValueEx("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "Path", RegistryType.REG_EXPAND_SZ, "%SystemRoot%\\system32;%SystemRoot%;%SystemDrive%\\Programs\\CommonFiles\\System;%SystemDrive%\\wtt;%SystemDrive%\\data\\test\\bin;%SystemRoot%\\system32\\WindowsPowerShell\\v1.0;");
                    RegEdit.SetHKLMValue("SOFTWARE\\Microsoft\\SecurityManager\\PrincipalClasses\\PRINCIPAL_CLASS_TCB", "Directories", RegistryType.REG_MULTI_SZ, "C:\\ ");
                    RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\BootSh", "Type", RegistryType.REG_DWORD, "00000010");
                    RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\BootSh", "Start", RegistryType.REG_DWORD, "00000002");
                    RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\BootSh", "ServiceSidType", RegistryType.REG_DWORD, "00000001");
                    RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\BootSh", "ErrorControl", RegistryType.REG_DWORD, "00000001");
                    RegEdit.SetHKLMValueEx("SYSTEM\\CurrentControlSet\\services\\BootSh", "ImagePath", RegistryType.REG_EXPAND_SZ, "%SystemRoot%\\system32\\svchost.exe -k Bootshsvc");
                    RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\BootSh", "DisplayName", RegistryType.REG_SZ, "@bootshsvc.dll,-1");
                    RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\BootSh", "Description", RegistryType.REG_SZ, "@bootshsvc.dll,-2");
                    RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\BootSh", "ObjectName", RegistryType.REG_SZ, "LocalSystem");
                    RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\BootSh", "DependOnService", RegistryType.REG_MULTI_SZ, "Afd lmhosts keyiso ");
                    RegEdit.SetHKLMValueEx("SYSTEM\\CurrentControlSet\\services\\BootSh", "FailureActions", RegistryType.REG_BINARY, "80510100000000000000000003000000140000000100000060EA00000100000060EA00000000000000000000");
                    RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\BootSh", "RequiredPrivileges", RegistryType.REG_MULTI_SZ, "SeAssignPrimaryTokenPrivilege SeAuditPrivilege SeSecurityPrivilege SeChangeNotifyPrivilege SeCreateGlobalPrivilege SeDebugPrivilege SeImpersonatePrivilege SeIncreaseQuotaPrivilege SeTcbPrivilege SeBackupPrivilege SeRestorePrivilege SeShutdownPrivilege SeSystemProfilePrivilege SeSystemtimePrivilege SeManageVolumePrivilege SeCreatePagefilePrivilege SeCreatePermanentPrivilege SeCreateSymbolicLinkPrivilege SeIncreaseBasePriorityPrivilege SeIncreaseWorkingSetPrivilege SeLoadDriverPrivilege SeLockMemoryPrivilege SeProfileSingleProcessPrivilege SeSystemEnvironmentPrivilege SeTakeOwnershipPrivilege SeTimeZonePrivilege ");
                    RegEdit.SetHKLMValueEx("SYSTEM\\CurrentControlSet\\services\\BootSh\\Parameters", "ServiceDll", RegistryType.REG_EXPAND_SZ, "%SystemRoot%\\system32\\bootshsvc.dll");
                    RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\BootSh\\Parameters", "ServiceDllUnloadOnStop", RegistryType.REG_DWORD, "00000001");
                    /*RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\BootSh\\Parameters\\Commands", "Loopback", 1, "CheckNetIsolation.exe loopbackexempt -a -n=CMDInjector_kqyng60eng17c");
                    RegEdit.SetHKLMValue("SYSTEM\\CurrentControlSet\\services\\BootSh\\Parameters\\Commands", "Telnetd", 1, "telnetd.exe cmd.exe 9999");*/
                    RegEdit.SetHKLMValue("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Svchost", "bootshsvc", RegistryType.REG_MULTI_SZ, "bootsh ");
                });
                reInjectionReboot.Text = "An injection reboot is pending. Please reboot your device as soon as possible to apply the changes.";
                reInjectionBox.Collapse();
                reInjectionReboot.Visible();
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

                FilesHelper.CopyFile(Helper.localFolder.Path + "\\CMDInjector.dat", @"C:\Windows\System32\CMDUninjector.dat");
                FilesHelper.CopyFromAppRoot("Contents\\BatchScripts\\NonSystemWide.bat", @"C:\Windows\System32\CMDInjector.bat");
                FilesHelper.CopyFromAppRoot("Contents\\InjectionType\\TemporaryInjection.reg", @"C:\Windows\System32\TemporaryInjection.reg");
                RegEdit.SetHKLMValueEx("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "Path", RegistryType.REG_EXPAND_SZ, $"%SystemRoot%\\system32;%SystemRoot%;%SystemDrive%\\Programs\\CommonFiles\\System;%SystemDrive%\\wtt;%SystemDrive%\\data\\test\\bin;%SystemRoot%\\system32\\WindowsPowerShell\\v1.0;{Helper.localFolder.Path}\\CMDInjector;");

                UnInjectBtn.Content = "Un-Injected";

                reInjectionReboot.Text = "An un-injection reboot is pending. Please reboot your device as soon as possible to apply the changes.";
                reInjectionBox.Collapse();
                reInjectionNote.Collapse();
                reInjectionReboot.Visible();
                _ = Reboot();
            }
        }
    }
}
