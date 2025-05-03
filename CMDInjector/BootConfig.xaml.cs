using CMDInjectorHelper;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Phone.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace CMDInjector
{
    public sealed partial class BootConfig : Page
    {
        StatusBarProgressIndicator Indicator { get; set; }
        string[] Identifier = new string[100];
        int flag = 0;

        #region Helpers
        private async Task<string[]> GetObjects()
        {
            using (var reader = File.OpenText($"{Helper.localFolder.Path}\\BootConfigObjects.txt"))
            {
                var Objects = await reader.ReadToEndAsync();
                return Objects.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            }
        }

        private byte[] ToBinary(string data)
        {
            data = data.Replace("-", "");
            return Enumerable.Range(0, data.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(data.Substring(x, 2), 16)).ToArray();
        }

        private async Task GetEntries()
        {
            try
            {
                flag = 1;
                VolUpBox.Items.Add(new ComboBoxItem
                {
                    Content = "None (Use for navigation)",
                    IsEnabled = !(!AppSettings.LoadSettings("HaveCamera", false) && !AppSettings.LoadSettings("UnlockHidden", false))
                });

                DescriptionBox.Text = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "BCD00000001\\Objects\\{7619dcc9-fafe-11d9-b411-000476eba25f}\\Elements\\12000004",
                    "Element",
                    RegistryType.REG_SZ
                );

                var TwoFourMilPlusOne = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\24000001",
                    "Element", RegistryType.REG_MULTI_SZ
                );

                if (TwoFourMilPlusOne.HasContent())
                {
                    string[] DisplayOrder = TwoFourMilPlusOne.Split(' ');
                    for (int i = 0; i < DisplayOrder.Length; i++)
                    {
                        DisplayOrderList.Items.Add(RegEdit.GetRegValue(
                            RegistryHive.HKEY_LOCAL_MACHINE,
                            $"BCD00000001\\Objects\\{DisplayOrder[i]}\\Elements\\12000004",
                            "Element", RegistryType.REG_SZ)
                        );
                    }
                }

                RemoveBtn.IsEnabled = DisplayOrderList.Items.Count > 1;

                ManTestSigningTog.IsOn = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\16000049",
                    "Element",
                    RegistryType.REG_BINARY
                ).Contains("01");

                ManNoIntegrityChecksTog.IsOn = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\16000048",
                    "Element",
                    RegistryType.REG_BINARY
                ).Contains("01");

                var timeOutGet = RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, "BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\25000004", "Element", RegistryType.REG_BINARY);

                if (timeOutGet.HasContent())
                {
                    string[] HexTimeout = timeOutGet.ToCharArray().Select(c => c.ToString()).ToArray();
                    TimeoutBox.Text = Convert.ToString(Convert.ToInt32(HexTimeout[0] + HexTimeout[1], 16));
                }
                
                BootMenuTog.IsOn = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\26000020",
                   "Element", RegistryType.REG_BINARY
                ).Contains("01");

                LoadTestSigningTog.IsOn = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "BCD00000001\\Objects\\{7619dcc9-fafe-11d9-b411-000476eba25f}\\Elements\\16000049",
                    "Element", RegistryType.REG_BINARY
                ).Contains("01");

                LoadNoIntegrityChecksTog.IsOn = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "BCD00000001\\Objects\\{7619dcc9-fafe-11d9-b411-000476eba25f}\\Elements\\16000048",
                    "Element",
                    RegistryType.REG_BINARY
                ).Contains("01");

                LoadFlightSignTog.IsOn = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "BCD00000001\\Objects\\{7619dcc9-fafe-11d9-b411-000476eba25f}\\Elements\\1600007e",
                    "Element",
                    RegistryType.REG_BINARY
                ).Contains("01");

                var TwoFiveKC2 = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "BCD00000001\\Objects\\{7619dcc9-fafe-11d9-b411-000476eba25f}\\Elements\\250000c2",
                    "Element", RegistryType.REG_BINARY
                );
                if (TwoFiveKC2.HasContent())
                {
                    string[] BootMenuPol = TwoFiveKC2.ToCharArray().Select(c => c.ToString()).ToArray();
                    BootMenuPolBox.SelectedIndex = Convert.ToInt32(BootMenuPol[1]);
                }

                AdvOptTog.IsOn = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "BCD00000001\\Objects\\{6efb52bf-1766-41db-a6b3-0ee5eff72bd7}\\Elements\\16000040",
                    "Element", RegistryType.REG_BINARY
                ).Contains("01");

                OptEditTog.IsOn = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE,
                    "BCD00000001\\Objects\\{6efb52bf-1766-41db-a6b3-0ee5eff72bd7}\\Elements\\16000041",
                    "Element", RegistryType.REG_BINARY
                ).Contains("01");

                if (Helper.IsTelnetConnected() && HomeHelper.IsConnected())
                {
                    File.Delete($"{Helper.localFolder.Path}\\BootConfigEnd.txt");
                    File.Delete($"{Helper.localFolder.Path}\\BootConfigObjects.txt");

                    await Helper.Send($"for /f \"delims=\\ tokens=4\" %a in ('reg query hklm\\bcd00000001\\objects') do echo %a >>{Helper.localFolder.Path}\\BootConfigObjects.txt&echo. >{Helper.localFolder.Path}\\BootConfigEnd.txt");

                    //while (File.Exists($"{Helper.localFolder.Path}\\BootConfigEnd.txt") == false)
                    //{
                    //    await Task.Delay(200);
                    //}
                    //string[] Objects = File.ReadAllLines($"{Helper.localFolder.Path}\\BootConfigObjects.txt");
                    await Task.Delay(1500);
                    string[] Objects = await GetObjects();
                    /*string toDisplay = string.Join(Environment.NewLine, Objects);
                    _ = new MessageDialog(toDisplay).ShowAsync();*/
                    var foundDevMenu = false;
                    var FiveFourMilPlusOne = RegEdit.GetRegValue(
                        RegistryHive.HKEY_LOCAL_MACHINE,
                        "BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\54000001",
                        "Element", RegistryType.REG_MULTI_SZ
                    );
                    var FiveFourMilPlusTwo = RegEdit.GetRegValue(
                        RegistryHive.HKEY_LOCAL_MACHINE,
                        "BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\54000002",
                        "Element", RegistryType.REG_MULTI_SZ
                    );
                    int j = 0;

                    for (int i = 0; i < Objects.Length; i++)
                    {
                        var regexEd = Regex.Replace(Objects[i], @"\s+", "");
                        var OneTwoMilPlusFour = RegEdit.GetRegValue(
                            RegistryHive.HKEY_LOCAL_MACHINE,
                            $"BCD00000001\\Objects\\{regexEd}\\Elements\\12000004",
                            "Element", RegistryType.REG_SZ
                        );

                        if (regexEd == "{311b88b5-9b30-491d-bad9-167ca3e2d417}" || regexEd == "{01de5a27-8705-40db-bad6-96fa5187d4a6}" ||
                            regexEd == "{0ce4991b-e6b3-4b16-b23c-5e0d9250e5d9}" || regexEd == "{4636856e-540f-4170-a130-a84776f4c654}" ||
                            regexEd == "{6efb52bf-1766-41db-a6b3-0ee5eff72bd7}" || regexEd == "{7ea2e1ac-2e61-4728-aaa3-896d9d0a9f0e}" ||
                            regexEd == "{ae5534e0-a924-466c-b836-758539a3ee3a}" || regexEd == "{9dea862c-5cdd-4e70-acc1-f32b344d4795}" ||

                            !OneTwoMilPlusFour.HasContent())
                        {
                            continue;
                        }

                        Identifier[j] = regexEd;

                        if (OneTwoMilPlusFour == "Developer Menu")
                        {
                            foundDevMenu = true;
                            DevMenuBtn.Content = "Uninstall";

                            DevTestSigningTog.IsOn = RegEdit.GetRegValue(
                                RegistryHive.HKEY_LOCAL_MACHINE,
                                $"BCD00000001\\Objects\\{regexEd}\\Elements\\16000049",
                                "Element", RegistryType.REG_BINARY
                            ).Contains("01");

                            DevNoIntegrityChecksTog.IsOn = RegEdit.GetRegValue(
                                RegistryHive.HKEY_LOCAL_MACHINE,
                                $"BCD00000001\\Objects\\{regexEd}\\Elements\\16000048",
                                "Element", RegistryType.REG_BINARY
                            ).Contains("01");

                            if (!Helper.IsSecureBootPolicyInstalled() && AppSettings.LoadSettings("UnlockHidden", false))
                            {
                                DevTestSigningTog.IsEnabled = true;
                                DevNoIntegrityChecksTog.IsEnabled = true;
                            }
                        }
                        else
                        {
                            if (!foundDevMenu) // ?
                            {
                                DevMenuBtn.Content = "Install";
                                if (Helper.IsSecureBootPolicyInstalled() && !AppSettings.LoadSettings("UnlockHidden", false))
                                {
                                    DevTestSigningTog.IsEnabled = false;
                                    DevNoIntegrityChecksTog.IsEnabled = false;
                                }
                            }
                        }

                        if (!TwoFourMilPlusOne.Contains(regexEd))
                        {
                            MenuFlyoutItem Items = new MenuFlyoutItem();
                            Items.Click += Items_Click;
                            Items.Text = OneTwoMilPlusFour;
                            AddFlyMenu.Items.Add(Items);
                        }
                        
                        DefaultBox.Items.Add(OneTwoMilPlusFour);
                        VolUpBox  .Items.Add(OneTwoMilPlusFour);
                        VolDownBox.Items.Add(OneTwoMilPlusFour);

                        if (RegEdit.GetRegValue(
                                RegistryHive.HKEY_LOCAL_MACHINE,
                                "BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\23000003",
                                "Element", RegistryType.REG_MULTI_SZ) == regexEd)

                            DefaultBox.SelectedIndex = j;

                        if (FiveFourMilPlusOne == regexEd)
                            VolUpBox.SelectedIndex = j + 1;

                        if (FiveFourMilPlusTwo == regexEd)
                            VolDownBox.SelectedIndex = j + 1;

                        j++;
                    }

                    if (!FiveFourMilPlusOne.HasContent())
                        VolUpBox.SelectedIndex = 0;

                    if (!FiveFourMilPlusTwo.HasContent())
                        VolDownBox.SelectedIndex = 0;

                    if (DisplayOrderList.Items.Count > 0)
                        SaveBtn.IsEnabled = true;

                    DefaultBox.IsEnabled = true;
                    VolUpBox.IsEnabled = true;
                    VolDownBox.IsEnabled = true;
                    DevMenuBtn.IsEnabled = true;
                    Indicator.ProgressValue = 0;
                    Indicator.Text = string.Empty;
                }
                flag = 0;
            }
            catch (NullReferenceException ex)
            {
                return;
            }
            catch (IndexOutOfRangeException ex)
            {
                DisplayOrderList.Items.Clear();
                await GetEntries();
            }

            AppSettings.SaveSettings("UnlockHidden", false);
        }

        private async void StatusIndicator()
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar")) // <- TO BE REMOVED?
            {
                StatusBar statusBar = StatusBar.GetForCurrentView();
                Indicator = statusBar.ProgressIndicator;
                Indicator.ProgressValue = null;
                Indicator.Text = "Reading...";
                await Indicator.ShowAsync();
            }
        }

        private async void StatusProgress()
        {
            File.Delete($"{Helper.localFolder.Path}\\BootConfigEnd.txt");
            Indicator.ProgressValue = null;
            Indicator.Text = "Writing...";
            while (File.Exists($"{Helper.localFolder.Path}\\BootConfigEnd.txt") == false)
            {
                await Task.Delay(200);
            }
            Indicator.ProgressValue = 0;
            Indicator.Text = string.Empty;
        }
        #endregion

        public BootConfig()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;

            HardwareButtons.CameraPressed += HardwareButtons_CameraPressed;
            FirstRun();
        }

        private async void FirstRun()
        {
            if (AppSettings.LoadSettings("BootConfigNote", true))
            {
                await Helper.MessageBox("It is recommended to install the Developer Menu and add it to the DisplayOrder or one of the Volume buttons to recover the BCD in case of corruption.", Helper.SoundHelper.Sound.Alert, "Warning");
                await Helper.MessageBox("Some of the settings are disabled by default. You have to press the Camera button from the BootConfig menu once to enable these AppSettings. These settings are only for devices having Camera button.", Helper.SoundHelper.Sound.Alert, "Note");
                AppSettings.SaveSettings("BootConfigNote", false);
            }
        }

        #region Navigation events
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (Helper.IsTelnetConnected() && HomeHelper.IsConnected())
            {
                StatusIndicator();
                DefaultBox.Items.Clear();
                AddFlyMenu.Items.Clear();
                VolUpBox.Items.Clear();
                VolDownBox.Items.Clear();
            }

            DisplayOrderList.Items.Clear();

            if (!AppSettings.LoadSettings("UnlockHidden", false))
            {
                if (!AppSettings.LoadSettings("HaveCamera", false))
                {
                    AdvOptTog.IsEnabled = false;
                    OptEditTog.IsEnabled = false;
                }
                else
                {
                    AdvOptTog.IsEnabled = true;
                    OptEditTog.IsEnabled = true;
                }

                if (Helper.IsSecureBootPolicyInstalled())
                {
                    ManNoIntegrityChecksTog.IsEnabled = false;
                    ManTestSigningTog.IsEnabled = false;
                    LoadNoIntegrityChecksTog.IsEnabled = false;
                    LoadTestSigningTog.IsEnabled = false;
                    DevTestSigningTog.IsEnabled = false;
                    DevNoIntegrityChecksTog.IsEnabled = false;
                }
                else
                {
                    ManNoIntegrityChecksTog.IsEnabled = true;
                    ManTestSigningTog.IsEnabled = true;
                    LoadNoIntegrityChecksTog.IsEnabled = true;
                    LoadTestSigningTog.IsEnabled = true;
                    DevTestSigningTog.IsEnabled = true;
                    DevNoIntegrityChecksTog.IsEnabled = true;
                }
            }
            _ = GetEntries();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            StatusBar statusBar = StatusBar.GetForCurrentView();
            Indicator = statusBar.ProgressIndicator;
            Indicator.ProgressValue = 0;
            Indicator.Text = string.Empty;
        }
        #endregion

        private void HardwareButtons_CameraPressed(object sender, CameraEventArgs e)
        {
            if (!AppSettings.HaveCamera)
            {
                AppSettings.HaveCamera  = true;
                AdvOptTog  .IsEnabled   = true;
                OptEditTog .IsEnabled   = true;
            }
        }

        private async void DefaultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flag == 0 && DefaultBox.SelectedItem != null)
            {
                //DefaultBox.IsEnabled = false;
                //statusProgress();
                //_ = Helper.Send("bcdedit /set {bootmgr} default \"" + Identifier[DefaultBox.SelectedIndex] + $"\"&echo. >{Helper.localFolder.Path}\\BootConfigEnd.txt");
                RegEdit.SetHKLMValue(
                    "BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\23000003",
                    "Element", RegistryType.REG_SZ, Identifier[DefaultBox.SelectedIndex]
                );

                if (Identifier[DefaultBox.SelectedIndex] != "{7619dcc9-fafe-11d9-b411-000476eba25f}")
                {
                    await Helper.MessageBox(
                        "Make sure the Windows Loader is selected in DisplayOrder or any of the Volume buttons." +
                        "Otherwise you WON'T be able to boot to Windows anymore.",
                        Helper.SoundHelper.Sound.Alert, "Warning"
                    );
                }
                //DefaultBox.IsEnabled = true;
            }
        }

        #region Man*Tog_Toggled events
        private void ManTestSigningTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\16000049",
                "Element", RegistryType.REG_BINARY,
                ToBinary(ManTestSigningTog.IsOn ? "01" : "00")
            );
        }

        private void ManNoIntegrityChecksTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\16000048",
                "Element", RegistryType.REG_BINARY,
                ToBinary(ManNoIntegrityChecksTog.IsOn ? "01" : "00")
            );
        }
        #endregion

        #region Timeout* events
        private void TimeoutBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TimeoutBtn.IsEnabled = !TimeoutBox.Text.HasContent() || TimeoutBox.Text.Contains("."); // a dot? FIXME
        }

        private void TimeoutBtn_Click(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\25000004", "Element",
                RegistryType.REG_BINARY, BitConverter.GetBytes(Convert.ToInt32(TimeoutBox.Text))
            );
        }
        #endregion

        private void BootMenuTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\26000020", "Element",
                RegistryType.REG_BINARY, ToBinary(BootMenuTog.IsOn ? "01" : "00")
            );
        }

        #region Vol*Box events
        private async void VolUpBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VolUpBox.IsEnabled = false;
            if (flag == 0 && VolUpBox.SelectedItem != null)
            {
                StatusProgress();
                if (VolUpBox.SelectedIndex != 0)
                {
                    if (VolDownBox.SelectedIndex != 0)
                    {
                        await Helper.Send($"bcdedit /set {{bootmgr}} customactions \"0x1000048000001\" \"0x54000001\" \"0x1000050000001\" \"0x54000002\"&echo. >{Helper.localFolder.Path}\\BootConfigEnd.txt");
                    }
                    else
                    {
                        await Helper.Send($"bcdedit /set {{bootmgr}} customactions \"0x1000048000001\" \"0x54000001\"&echo. >{Helper.localFolder.Path}\\BootConfigEnd.txt");
                    }
                    RegEdit.SetHKLMValue("BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\54000001", "Element", RegistryType.REG_MULTI_SZ, Regex.Replace(Identifier[VolUpBox.SelectedIndex - 1], @"\s+", ""));
                }
                else
                {
                    if (VolDownBox.SelectedIndex != 0)
                    {
                        await Helper.Send($"reg delete \"hklm\\bcd00000001\\objects\\{{9dea862c-5cdd-4e70-acc1-f32b344d4795}}\\elements\\54000001\" /f & bcdedit /set {{bootmgr}} customactions  \"0x1000050000001\" \"0x54000002\"&echo. >{Helper.localFolder.Path}\\BootConfigEnd.txt");
                    }
                    else
                    {
                        await Helper.Send($"reg delete \"hklm\\bcd00000001\\objects\\{{9dea862c-5cdd-4e70-acc1-f32b344d4795}}\\elements\\54000001\" /f & reg delete \"hklm\\bcd00000001\\objects\\{{9dea862c-5cdd-4e70-acc1-f32b344d4795}}\\elements\\27000030\" /f&echo. >{Helper.localFolder.Path}\\BootConfigEnd.txt");
                    }
                }
            }
            VolUpBox.IsEnabled = true;
        }

        private async void VolDownBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VolDownBox.IsEnabled = false;
            if (flag == 0 && VolUpBox.SelectedItem != null)
            {
                StatusProgress();
                if (VolDownBox.SelectedIndex != 0)
                {
                    if (VolUpBox.SelectedIndex != 0)
                    {
                        await Helper.Send($"bcdedit /set {{bootmgr}} customactions \"0x1000048000001\" \"0x54000001\" \"0x1000050000001\" \"0x54000002\"&echo. >{Helper.localFolder.Path}\\BootConfigEnd.txt");
                    }
                    else
                    {
                        await Helper.Send($"bcdedit /set {{bootmgr}} customactions \"0x1000050000001\" \"0x54000002\"&echo. >{Helper.localFolder.Path}\\BootConfigEnd.txt");
                    }
                    RegEdit.SetHKLMValue("BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\54000002", "Element", RegistryType.REG_MULTI_SZ, Regex.Replace(Identifier[VolDownBox.SelectedIndex - 1], @"\s+", ""));
                }
                else
                {
                    if (VolUpBox.SelectedIndex != 0)
                    {
                        await Helper.Send($"reg delete \"hklm\\bcd00000001\\objects\\{{9dea862c-5cdd-4e70-acc1-f32b344d4795}}\\elements\\54000002\" /f & bcdedit /set {{bootmgr}} customactions  \"0x1000048000001\" \"0x54000001\"&echo. >{Helper.localFolder.Path}\\BootConfigEnd.txt");
                    }
                    else
                    {
                        await Helper.Send($"reg delete \"hklm\\bcd00000001\\objects\\{{9dea862c-5cdd-4e70-acc1-f32b344d4795}}\\elements\\54000002\" /f & reg delete \"hklm\\bcd00000001\\objects\\{{9dea862c-5cdd-4e70-acc1-f32b344d4795}}\\elements\\27000030\" /f&echo. >{Helper.localFolder.Path}\\BootConfigEnd.txt");
                    }
                }
            }
            VolDownBox.IsEnabled = true;
        }
        #endregion
        
        #region Description* events
        private async void DescriptionBtn_Click(object sender, RoutedEventArgs e)
        {
            DescriptionBtn.IsEnabled = false;
            RegEdit.SetHKLMValue("BCD00000001\\Objects\\{7619dcc9-fafe-11d9-b411-000476eba25f}\\Elements\\12000004", "Element", RegistryType.REG_SZ, DescriptionBox.Text);
            Indicator.ProgressValue = null;
            Indicator.Text = "Writing";
            DefaultBox.Items.Clear();
            AddFlyMenu.Items.Clear();
            VolUpBox.Items.Clear();
            VolDownBox.Items.Clear();
            DisplayOrderList.Items.Clear();
            await GetEntries();
            DescriptionBtn.IsEnabled = true;
        }

        private void DescriptionBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DescriptionBtn.IsEnabled = (!DescriptionBox.Text.HasContent() || !DescriptionBox.Text.HasContent()).Toggle();
        }
        #endregion

        #region Load*Tog events
        private void LoadTestSigningTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "BCD00000001\\Objects\\{7619dcc9-fafe-11d9-b411-000476eba25f}\\Elements\\16000049",
                "Element", RegistryType.REG_BINARY, ToBinary(LoadTestSigningTog.IsOn ? "01" : "00"));
        }

        private void LoadNoIntegrityChecksTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "BCD00000001\\Objects\\{7619dcc9-fafe-11d9-b411-000476eba25f}\\Elements\\16000048",
                "Element", RegistryType.REG_BINARY, ToBinary(LoadNoIntegrityChecksTog.IsOn ? "01" : "00"));
        }

        private void LoadFlightSignTog_Toggled(object sender, RoutedEventArgs e)
        {
            if (LoadFlightSignTog.IsOn)
            {
                RegEdit.SetHKLMValue(
                    "BCD00000001\\Objects\\{7619dcc9-fafe-11d9-b411-000476eba25f}\\Elements\\1600007e",
                    "Element", RegistryType.REG_BINARY, ToBinary("01")
                );
                Globals.nrpc.FileCopy(
                    $"{Helper.installedLocation.Path}\\Contents\\Certificates\\SbcpFlightToken.p7b",
                    "C:\\EFIESP\\efi\\Microsoft\\boot\\policies", 0
                );
            }
            else
                RegEdit.SetHKLMValue(
                    "BCD00000001\\Objects\\{7619dcc9-fafe-11d9-b411-000476eba25f}\\Elements\\1600007e",
                    "Element", RegistryType.REG_BINARY, ToBinary("00"));
        }
        #endregion

        private void BootMenuPolBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "BCD00000001\\Objects\\{7619dcc9-fafe-11d9-b411-000476eba25f}\\Elements\\250000c2",
                "Element", RegistryType.REG_BINARY, ToBinary(BootMenuPolBox.SelectedIndex == 0 ? "00" : "01"));
        }

        private void AdvOptTog_Toggled(object sender, RoutedEventArgs e)
        {
            if (AdvOptTog.IsOn)
            {
                RegEdit.SetHKLMValue("BCD00000001\\Objects\\{6efb52bf-1766-41db-a6b3-0ee5eff72bd7}\\Elements\\16000040", "Element", RegistryType.REG_BINARY, ToBinary("01"));
                if (flag == 0)
                    Helper.MessageBox("Make sure BootMenuPolicy is set to Legacy, otherwise you WON'T be able to boot to the Windows ANYMORE.", Helper.SoundHelper.Sound.Alert, "Warning");
            }
            else
            {
                RegEdit.SetHKLMValue("BCD00000001\\Objects\\{6efb52bf-1766-41db-a6b3-0ee5eff72bd7}\\Elements\\16000040", "Element", RegistryType.REG_BINARY, ToBinary("00"));
            }
        }

        private void OptEditTog_Toggled(object sender, RoutedEventArgs e)
        {
            RegEdit.SetHKLMValue(
                "BCD00000001\\Objects\\{6efb52bf-1766-41db-a6b3-0ee5eff72bd7}\\Elements\\16000041",
                "Element", RegistryType.REG_BINARY, ToBinary(OptEditTog.IsOn ? "01" : "00"));
        }

        private void MoveUpBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DisplayOrderList.SelectedIndex == 0 || DisplayOrderList.SelectedIndex == -1)
            {
                return;
            }
            int newIndex = DisplayOrderList.SelectedIndex - 1;
            object selectedIndex = DisplayOrderList.SelectedItem;
            DisplayOrderList.Items.Remove(selectedIndex);
            DisplayOrderList.Items.Insert(newIndex, selectedIndex);
            DisplayOrderList.SelectedIndex = newIndex;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            /*SaveBtn.IsEnabled = false;
            statusProgress();*/
            string orderList = string.Empty;
            for (int i = 0; i < DisplayOrderList.Items.Count; i++)
            {
                for (int j = 0; j < DefaultBox.Items.Count; j++)
                {
                    if (RegEdit.GetRegValue(
                            RegistryHive.HKEY_LOCAL_MACHINE,
                            $"BCD00000001\\Objects\\{Identifier[j]}\\Elements\\12000004",
                            "Element", RegistryType.REG_SZ
                        ) == DisplayOrderList.Items[i].ToString())
                    {
                        orderList += /*"\"" +*/ Identifier[j] + " " /*+ "\" "*/;
                        break;
                    }
                }
            }
            //await Helper.Send("bcdedit /set {bootmgr} displayorder " + orderList + $"&echo. >{Helper.localFolder.Path}\\BootConfigEnd.txt");
            RegEdit.SetHKLMValue("BCD00000001\\Objects\\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\\Elements\\24000001", "Element", RegistryType.REG_MULTI_SZ, orderList);
            /*SaveBtn.IsEnabled = true;
            InputPane.GetForCurrentView().TryHide();*/
        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DisplayOrderList.SelectedIndex == -1) // < How? FIXME
            {
                return;
            }

            MenuFlyoutItem Items = new MenuFlyoutItem();
            Items.Click += Items_Click;
            Items.Text = DisplayOrderList.SelectedItem.ToString();
            AddFlyMenu.Items.Add(Items);
            DisplayOrderList.Items.Remove(DisplayOrderList.SelectedItem);

            if (DisplayOrderList.Items.Count <= 1)
            {
                RemoveBtn.IsEnabled = false;
                MoveUpBtn.IsEnabled = false;
            }
        }

        private void Items_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem clickedItem = (MenuFlyoutItem)sender;
            DisplayOrderList.Items.Add(clickedItem.Text);
            AddFlyMenu.Items.Remove(clickedItem);

            if (DisplayOrderList.Items.Count > 1)
            {
                RemoveBtn.IsEnabled = true;
                MoveUpBtn.IsEnabled = true;
            }
            SaveBtn.IsEnabled = true;
        }

        private async void DevMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            DevMenuBtn.IsEnabled = false;
            File.Delete($"{Helper.localFolder.Path}\\BootConfigEnd.txt");
            Indicator.ProgressValue = null;
            Indicator.Text = "Writing...";

            if ((DevMenuBtn.Content as string) == "Install")
            {
                if (Helper.build <= 14393)
                {
                    FilesHelper.CopyFromAppRoot("Contents\\DeveloperMenu\\developermenu1607.efi", "C:\\EFIESP\\Windows\\System32\\Boot");
                    await Helper.Send(
                        "bcdedit /create {dcc0bd7c-ed9d-49d6-af62-23a3d901117b} /d \"Developer Menu\" /application \"bootapp\"" +
                        "&bcdedit /set {dcc0bd7c-ed9d-49d6-af62-23a3d901117b} path \"\\windows\\system32\\BOOT\\developermenu.efi\"" +
                        "&bcdedit /set {dcc0bd7c-ed9d-49d6-af62-23a3d901117b} device \"partition=%SystemDrive%\\Efiesp\"" +
                        "&bcdedit /set {dcc0bd7c-ed9d-49d6-af62-23a3d901117b} inherit \"{bootloadersettings}\"" +
                        //"&bcdedit /set {dcc0bd7c-ed9d-49d6-af62-23a3d901117b} isolatedcontext \"yes\"" +
                        //"&bcdedit /set {dcc0bd7c-ed9d-49d6-af62-23a3d901117b} nointegritychecks \"yes\"" +
                        $"&echo. >{Helper.localFolder.Path}\\BootConfigEnd.txt"
                    );
                }
                else
                {
                    FilesHelper.CopyFromAppRoot(
                        $"Contents\\DeveloperMenu\\developermenu1709.efi",
                        "C:\\EFIESP\\Windows\\System32\\Boot\\developermenu.efi"
                    );
                    await Helper.Send(
                        "bcdedit /create {dcc0bd7c-ed9d-49d6-af62-23a3d901117b} /d \"Developer Menu\" /application \"bootapp\" &" +
                        "bcdedit /set {dcc0bd7c-ed9d-49d6-af62-23a3d901117b} path \"\\windows\\system32\\BOOT\\developermenu.efi\" &" +
                        "bcdedit /set {dcc0bd7c-ed9d-49d6-af62-23a3d901117b} device \"partition=%SystemDrive%\\Efiesp\" &" +
                        "bcdedit /set {dcc0bd7c-ed9d-49d6-af62-23a3d901117b} inherit \"{bootloadersettings}\" &" +
                        "bcdedit /set {dcc0bd7c-ed9d-49d6-af62-23a3d901117b} isolatedcontext \"yes\" &" +
                        $"echo. >{Helper.localFolder.Path}\\BootConfigEnd.txt"
                    );
                }

                FilesHelper.CopyFromAppRoot("Contents\\DeveloperMenu\\ui\\boot.ums.connected.bmpx", "C:\\EFIESP\\Windows\\System32\\Boot\\ui\\");
                FilesHelper.CopyFromAppRoot("Contents\\DeveloperMenu\\ui\\boot.ums.disconnected.bmpx", "C:\\EFIESP\\Windows\\System32\\Boot\\ui\\");
                FilesHelper.CopyFromAppRoot("Contents\\DeveloperMenu\\ui\\boot.ums.waiting.bmpx", "C:\\EFIESP\\Windows\\System32\\Boot\\ui\\");
            }
            else
            {
                for (int i = 0; i < DefaultBox.Items.Count; i++)
                {
                    if (RegEdit.GetRegValue(
                            RegistryHive.HKEY_LOCAL_MACHINE,
                            $"BCD00000001\\Objects\\{Identifier[i]}\\Elements\\12000004",
                            "Element", RegistryType.REG_SZ) == "Developer Menu")
                    {
                        await Helper.Send($"bcdedit /delete \"{Identifier[i]}\"&echo. >{Helper.localFolder.Path}\\BootConfigEnd.txt");
                        break;
                    }
                }
            }

            while (File.Exists($"{Helper.localFolder.Path}\\BootConfigEnd.txt") == false)
            {
                await Task.Delay(200);
            }

            DefaultBox.Items.Clear();
            AddFlyMenu.Items.Clear();
            VolUpBox.Items.Clear();
            VolDownBox.Items.Clear();
            DisplayOrderList.Items.Clear();
            await GetEntries();
            DevMenuBtn.IsEnabled = true;
        }

        private void DevTestSigningTog_Toggled(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < DefaultBox.Items.Count; i++)
            {
                if (RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE,
                                        $"BCD00000001\\Objects\\{Identifier[i]}\\Elements\\12000004",
                                        "Element", RegistryType.REG_SZ
                    ) == "Developer Menu")
                {
                    RegEdit.SetRegValue(
                        RegistryHive.HKEY_LOCAL_MACHINE,
                        $"BCD00000001\\Objects\\{Identifier[i]}\\Elements\\16000049",
                        "Element",
                        RegistryType.REG_BINARY,
                        ToBinary(DevTestSigningTog.IsOn ? "01" : "00")
                    );
                    break;
                }
            }
        }

        private void DevNoIntegrityChecksTog_Toggled(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < DefaultBox.Items.Count; i++)
            {
                if (RegEdit.GetRegValue(RegistryHive.HKEY_LOCAL_MACHINE,
                                        $"BCD00000001\\Objects\\{Identifier[i]}\\Elements\\12000004",
                                        "Element", RegistryType.REG_SZ
                    ) == "Developer Menu")
                {
                    RegEdit.SetHKLMValue(
                        $"BCD00000001\\Objects\\{Identifier[i]}\\Elements\\16000048",
                        "Element", RegistryType.REG_BINARY,
                        ToBinary(DevNoIntegrityChecksTog.IsOn ? "01" : "00")
                    );
                    break;
                }
            }
        }
    }
}
