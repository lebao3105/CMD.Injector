using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using CMDInjectorHelper;


namespace CMDInjector
{
    public sealed partial class Startup : Page
    {
        public Startup()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Initialize();
            CommandBox.TextWrapping = AppSettings.CommandsTextWrap.ToTextWrapping();
        }

        private async void Initialize()
        {
            try
            {
                if (HomeHelper.IsCMDInjected())
                {
                    FilesHelper.CopyFile(
                        "Startup.bat".IsAFileInSystem32() ?
                            "C:\\Windows\\System32\\Startup.bat" :
                            $"{Globals.installedLocation.Path}\\Contents\\BatchScripts\\Startup.bat",
                        $"{Helper.localFolder.Path}\\Startup.bat"
                    );
                    var text = await FileIO.ReadTextAsync(await Helper.localFolder.GetFileAsync("Startup.bat"), Windows.Storage.Streams.UnicodeEncoding.Utf8);
                    CommandBox.Text = CommandBox.Text.Remove($"{text}\r".LastIndexOf("\r")); // ?
                }
                else
                {
                    CommandBox.IsReadOnly = true;
                    _ = Helper.MessageBox(HomeHelper.GetTelnetTroubleshoot(), Helper.SoundHelper.Sound.Error, "Error");
                }
            }
            catch (Exception ex)
            {
                CommandBox.IsReadOnly = false;
                Helper.ThrowException(ex);
            }
        }

        private async void CommandBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await FileIO.WriteTextAsync(await Helper.localFolder.GetFileAsync("Startup.bat"), CommandBox.Text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n"));
                FilesHelper.CopyFile(Helper.localFolder.Path + "\\Startup.bat", @"C:\Windows\System32\Startup.bat");
                _ = Helper.MessageBox("The changes applied successfully.", Helper.SoundHelper.Sound.Alert, "Completed");
            }
            catch (Exception ex)
            {
                Helper.ThrowException(ex);
            }
        }
    }
}
