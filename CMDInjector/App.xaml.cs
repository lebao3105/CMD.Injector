using CMDInjectorHelper;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Security.Credentials.UI;
using Windows.Storage.AccessCache;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using XamlBrewer.Uwp.Controls;

namespace CMDInjector
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        Frame rootFrame;
        //TelnetConnection tc;
        static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        bool isRootFrame = false;

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;

            //Connect();
            Helper.Init();
            Helper.SoundHelper.Init();

#pragma warning disable CS4014
            if (!Helper.BackgroundTaskHelper.IsThemeTaskActivated())
                Helper.BackgroundTaskHelper.RegisterThemeTask(15, new SystemTrigger(SystemTriggerType.UserAway, false));

            if (!Helper.BackgroundTaskHelper.IsWallpaperTaskActivated())
                Helper.BackgroundTaskHelper.RegisterWallpaperTask(15, new SystemTrigger(SystemTriggerType.UserPresent, false));
#pragma warning restore CS4014
        }

        // FIXME: Do something when a while loop stops
//        private void Connect()
//        {
//#pragma warning disable CS4014
//            string IP = "127.0.0.1";
//            int Port = 9999;

//            tc = new TelnetConnection(IP, Port);
//            long i = 0;

//            while (tc.IsConnected == false && i < 1000000)
//            {
//                i++;
//            }

//            tClient.Connect();
//            i = 0;

//            while (Helper.IsTelnetConnected() == false && i < 1000000)
//            {
//                i++;
//            }
//#pragma warning restore CS4014
//        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            switch (e.Arguments)
            {
                case "Restart":
                    Helper.RebootSystem();
                    break;

                case "Shutdown":
                case "Lockscreen":
                case "FFULoader":
                case "VolUp":
                case "VolDown":
                case "VolMute":
                    if (Helper.IsTelnetConnected() && HomeHelper.IsConnected())
                    {
#pragma warning disable CS4014
                        switch (e.Arguments)
                        {
                            case "Shutdown":
                                Helper.Send("shutdown /s /t 0");
                                break;

                            case "Lockscreen":
                                Helper.SoundHelper.PlaySound(Helper.SoundHelper.Sound.Lock);
                                Helper.Send("powertool -screenoff");
                                break;

                            case "FFULoader":
                                Helper.Send("start " + Helper.installedLocation.Path + "\\Contents\\BatchScripts\\RebootToFlashingMode.bat");
                                await Helper.MessageBox("Rebooting to FFU Loader...");
                                break;

                            case "VolUp":
                                Helper.Send("SendKeys -v \"0xAF 0xAF\"");
                                break;

                            case "VolDown":
                                Helper.Send("SendKeys -v \"0xAE 0xAE\"");
                                break;

                            case "VolMute":
                                Helper.Send("SendKeys -v \"0xAD\"");
                                break;
                        }

                        if (e.PreviousExecutionState == ApplicationExecutionState.NotRunning)
                        {
                            CoreApplication.Exit();
                        }
                        else if (e.PreviousExecutionState != ApplicationExecutionState.Running)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(2));
                            Helper.Send("SendKeys -v \"0x5B\"");
                        }
                        return;
                    }
                    else
                    {
                        Helper.MessageBox(HomeHelper.GetTelnetTroubleshoot(), Helper.SoundHelper.Sound.Error, "Error");
                    }
                    break;

                default:
                    if (StorageApplicationPermissions.FutureAccessList.ContainsItem(e.Arguments))
                    {
                        try
                        {
                            await Launcher.LaunchFolderAsync(await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(e.Arguments));
                            if (e.PreviousExecutionState == ApplicationExecutionState.NotRunning) CoreApplication.Exit();
                        }
                        catch (Exception) { Helper.MessageBox("The folder you are trying to open is no longer exist.", Helper.SoundHelper.Sound.Error, "Missing Folder"); }
                        return;
                    }
                    break;
            }
#pragma warning restore CS4014

            Helper.rect = e.SplashScreen.ImageLocation;

            rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                /*if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }*/

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
                
                // Do we exactly NEED these when the Window already has content?
                if (AppSettings.ThemeSettings)
                {
                    if (AppSettings.Theme == 0)
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
                    ((Frame)Window.Current.Content).RequestedTheme = ElementTheme.Default;

                    Helper.color = RegEdit.GetRegValue(
                        RegistryHive.HKEY_LOCAL_MACHINE,
                        "Software\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\Theme",
                        "CurrentTheme",
                        RegistryType.REG_DWORD
                    ).IsDWORDStrFullZeros() ? Colors.White : Colors.Black;
                }

                try
                {
                    await Task.Run(async () =>
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            if (AppSettings.LoadSettings("AccentSettings", false))
                            {
                                var accentColor = (Color)typeof(Colors).GetRuntimeProperties()
                                        .Where(obj => !Globals.CursedColors.Contains(obj.Name))
                                        .ElementAt(AppSettings.LoadSettings("Accent", 11)).GetValue(null);

                                //int count = 0;
                                //foreach (var color in typeof(Colors).GetRuntimeProperties())
                                //{
                                //    if (!Globals.CursedColors.Contains(color.Name))
                                //    {
                                //        if (AppSettings.LoadSettings("Accent", 11) == count)
                                //        {
                                //            var accentColor = (Color)color.GetValue(null);

                                (this.GetResource("AppAccentColor") as SolidColorBrush).Color = accentColor;
                                (this.GetResource("ToggleSwitchFillOn") as SolidColorBrush).Color = accentColor;
                                (this.GetResource("TextControlBorderBrushFocused") as SolidColorBrush).Color = accentColor;
                                (this.GetResource("RadioButtonOuterEllipseCheckedStroke") as SolidColorBrush).Color = accentColor;
                                (this.GetResource("SliderTrackValueFill") as SolidColorBrush).Color = accentColor;
                                (this.GetResource("SliderThumbBackground") as SolidColorBrush).Color = accentColor;
                                (this.GetResource("SystemControlHighlightAccentBrush") as SolidColorBrush).Color = accentColor;

                                (this.GetResource("SystemControlHighlightListAccentLowBrush") as SolidColorBrush).Color = Color.FromArgb(204, accentColor.R, accentColor.G, accentColor.B);
                                (this.GetResource("SystemControlHighlightListAccentHighBrush") as SolidColorBrush).Color = Color.FromArgb(242, accentColor.R, accentColor.G, accentColor.B);

                                if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                                {
                                    var statusBar = StatusBar.GetForCurrentView();
                                    if (statusBar != null)
                                    {
                                        statusBar.ForegroundColor = accentColor;
                                    }
                                }
                                //            break;
                                //        }
                                //        count++;
                                //    }
                                //}
                            }
                            else if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                            {
                                var statusBar = StatusBar.GetForCurrentView();
                                if (statusBar != null)
                                {
                                    var accentColor = new UISettings().GetColorValue(UIColorType.Accent);
                                    statusBar.ForegroundColor = accentColor;
                                }
                            }
                        });
                    });
                }
                catch (Exception ex) { /*CommonHelper.ThrowException(ex);*/ }

                #region Check for updates
                bool flag = false;

                try
                {
                    if (!Helper.NotificationHelper.IsToastAlreadyThere("CheckUpdateTag"))
                    {
                        var latestRelease = await AboutHelper.IsNewUpdateAvailable();
                        if (latestRelease != null)
                        {
                            flag = true;
                            Helper.NotificationHelper.PushNotification("A new version of CMD Injector is available.", "Update Available", "DownloadUpdate", "CheckUpdateTag", 0);
                            AppSettings.SaveSettings("UpdateNotifyTime", DateTime.Now.AddHours(12));
                        }
                    }
                }
                catch (Exception ex)
                {
                    //CommonHelper.ThrowException(ex);
                }

                if (!flag && "CMDInjectorVersion.dat".IsAFileInSystem32())
                {
                    if (!Helper.NotificationHelper.IsToastAlreadyThere("Re-InjectTag") && Globals.InjectedBatchVersion.ToInt32() < Globals.currentBatchVersion)
                    {
                        if (File.Exists(@"C:\Windows\System32\CMDInjector.dat"))
                            Helper.NotificationHelper.PushNotification("The App has been updated, a re-injection is required.", "Re-injection required", "ReinjectionRequired", "Re-InjectTag", 1800);
                        else
                            Helper.NotificationHelper.PushNotification("The App has been updated, a re-injection is required.", "Re-injection required", "ReinjectionRequired", "Re-InjectTag", 1800, "Re-Inject", "InjectCMD", "Re-Inject");
                    }
                }
                #endregion
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);

                    if (AppSettings.LoginTogReg && AppSettings.SplashScreen && Helper.build >= 10572)
                    {
                        ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
                        (rootFrame.Content as Page).OpenFromSplashScreen(e.SplashScreen.ImageLocation, Helper.color, new Uri("ms-appx:///Assets/SplashScreen.png"));
                    }
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }

            Type frame = null;

            if (!string.IsNullOrEmpty(e.Arguments))
            {
                isRootFrame = true;

                //var rootFrame = new Frame();
                if (e.Arguments.EndsWith("Page"))
                    frame = Type.GetType($"CMDInjector.{e.Arguments.Replace("Page", "")}");
                //switch (e.Arguments)
                //{
                //    case "HomePage":
                //        frame = typeof(Home);
                //        break;

                //    case "TerminalPage":
                //        frame = typeof(Terminal);
                //}
                //if (e.Arguments == "HomePage")
                //{
                //    frame = typeof(Home);
                //}
                //else if (e.Arguments.Contains("TerminalPage"))
                //{
                //    frame = typeof(Terminal);
                //}
                //else if (e.Arguments == "StartupPage")
                //{
                //    frame = typeof(Startup);
                //}
                //else if (e.Arguments == "PacManPage")
                //{
                //    frame = typeof(PacMan);
                //}
                //else if (e.Arguments == "SnapperPage")
                //{
                //    frame = typeof(Snapper);
                //}
                //else if (e.Arguments == "BootConfigPage")
                //{
                //    frame = typeof(BootConfig);
                //}
                //else if (e.Arguments == "TweakBoxPage")
                //{
                //    frame = typeof(TweakBox);
                //}
                //else if (e.Arguments == "SettingsPage")
                //{
                //    frame = typeof(Settings);
                //}
                //else if (e.Arguments == "HelpPage")
                //{
                //    frame = typeof(Help);
                //}
                //else if (e.Arguments == "AboutPage")
                //{
                //    frame = typeof(About);
                //}
            }

            if (AppSettings.SplashScreen && !Helper.splashScreenDisplayed)
            {
                if (e.PreviousExecutionState != ApplicationExecutionState.Running)
                {
                    bool loadState = (e.PreviousExecutionState == ApplicationExecutionState.Terminated);
                    SplashAnimation extendedSplash = new SplashAnimation(e, loadState);
                    rootFrame.Content = extendedSplash;
                    Window.Current.Content = rootFrame;
                    while (true)
                    {
                        await Task.Delay(200);
                        if (Helper.splashScreenDisplayed) break;
                    }
                    ApplicationView.GetForCurrentView().ExitFullScreenMode();
                }
            }
            if (!await CallLoginPage())
            {
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            if (frame != null)
            {
                if (frame.Name == "Home" || frame.Name == "Terminal") rootFrame.Navigate(frame, e.Arguments);
                else rootFrame.Navigate(frame);
            }
            if (!AppSettings.LoginTogReg && AppSettings.SplashScreen && Helper.build >= 10572) (rootFrame.Content as Page).OpenFromSplashScreen(e.SplashScreen.ImageLocation, Helper.color, new Uri("ms-appx:///Assets/SplashScreen.png"));

            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }

        protected override async void OnActivated(IActivatedEventArgs e)
        {
            base.OnActivated(e);
            rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    if (e.Kind == ActivationKind.ToastNotification)
                    {
                        var toastActivationArgs = e as ToastNotificationActivatedEventArgs;
                        if (toastActivationArgs.Argument.Contains("Snapper") || toastActivationArgs.Argument == "OpenImage" || toastActivationArgs.Argument == "OpenVideo")
                        {
                            rootFrame.Navigate(typeof(MainPage));
                            rootFrame.Navigate(typeof(Snapper));
                        }
                        else if (toastActivationArgs.Argument.Contains("Updat")) // Updation and DownloadUpdate
                        {
                            rootFrame.Navigate(typeof(MainPage));
                            rootFrame.Navigate(typeof(About), toastActivationArgs.Argument);
                        }
                        else if (toastActivationArgs.Argument.Contains("ReinjectionRequired"))
                        {
                            rootFrame.Navigate(typeof(MainPage));
                            rootFrame.Navigate(typeof(Home), toastActivationArgs.Argument);
                        }
                        else
                        {
                            rootFrame.Navigate(typeof(MainPage), toastActivationArgs.Argument);
                        }
                    }
                    else if (e.Kind == ActivationKind.Protocol)
                    {
                        var protocolArgs = e as ProtocolActivatedEventArgs;
                        var AbsoluteURI = protocolArgs.Uri.AbsoluteUri;
                        if (AbsoluteURI == "cmdinjector:")
                        {
                            rootFrame.Navigate(typeof(MainPage));
                        }
                        else
                        {
                            var cleanToken = AbsoluteURI.Replace("cmdinjector::", "").Replace("cmdinjector:", ""); // ?
                            var strFilePath = await SharedStorageAccessManager.RedeemTokenForFileAsync(cleanToken);
                            if (Path.GetExtension(strFilePath.Path.ToString().ToLower()) == ".xap" || Path.GetExtension(strFilePath.Path.ToString().ToLower()) == ".appx" || Path.GetExtension(strFilePath.Path.ToString().ToLower()) == ".appxbundle" || Path.GetExtension(strFilePath.Path.ToString().ToLower()) == ".xml" || Path.GetExtension(strFilePath.Path).ToLower() == ".pmlog")
                            {
                                rootFrame.Navigate(typeof(MainPage));
                                rootFrame.Navigate(typeof(PacMan), cleanToken);
                            }
                            else
                            {
                                rootFrame.Navigate(typeof(MainPage), protocolArgs);
                            }
                        }
                    }
                }

                await CallLoginPage();

                // Ensure the current window is active
                Window.Current.Activate();
            }
            else
            {
                if (isRootFrame)
                {
                    if (e.Kind == ActivationKind.ToastNotification)
                    {
                        var toastActivationArgs = e as ToastNotificationActivatedEventArgs;

                        if (toastActivationArgs.Argument.Contains("Snapper") || toastActivationArgs.Argument == "OpenImage" || toastActivationArgs.Argument == "OpenVideo")
                        {
                            rootFrame.Navigate(typeof(Snapper), toastActivationArgs.Argument);
                        }
                        else if (toastActivationArgs.Argument.Contains("Updat"))
                        {
                            rootFrame.Navigate(typeof(About), toastActivationArgs.Argument);
                        }
                        else if (toastActivationArgs.Argument.Contains("ReinjectionRequired"))
                        {
                            rootFrame.Navigate(typeof(Home), toastActivationArgs.Argument);
                        }
                    }
                    else if (e.Kind == ActivationKind.Protocol)
                    {
                        var protocolArgs = e as ProtocolActivatedEventArgs;
                        var AbsoluteURI = protocolArgs.Uri.AbsoluteUri;
                        if (AbsoluteURI == "cmdinjector:")
                        {
                            rootFrame.Navigate(typeof(MainPage), protocolArgs);
                        }
                        else
                        {
                            var cleanToken = AbsoluteURI.Replace("cmdinjector::", "").Replace("cmdinjector:", "");
                            var strFilePath = await SharedStorageAccessManager.RedeemTokenForFileAsync(cleanToken);
                            var fileExt = Path.GetExtension(strFilePath.Path).ToLower();

                            if (fileExt == ".xap" || fileExt == ".appx" || fileExt == ".appxbundle" || fileExt == ".xml" || fileExt == ".pmlog")
                            {
                                rootFrame.Navigate(typeof(PacMan), cleanToken);
                            }
                            else
                            {
                                rootFrame.Navigate(typeof(MainPage), protocolArgs);
                            }
                        }
                    }
                }
                else
                {
                    if (e.Kind == ActivationKind.ToastNotification)
                    {
                        var toastActivationArgs = e as ToastNotificationActivatedEventArgs;
                        rootFrame.Navigate(typeof(MainPage), toastActivationArgs.Argument);
                    }
                    else if (e.Kind == ActivationKind.Protocol)
                    {
                        var protocolArgs = e as ProtocolActivatedEventArgs;
                        rootFrame.Navigate(typeof(MainPage), protocolArgs);
                    }
                }
                await CallLoginPage();
            }
        }

        protected override async void OnFileActivated(FileActivatedEventArgs args)
        {
            base.OnFileActivated(args);
            rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage));
                    await CallLoginPage();
                    rootFrame.Navigate(typeof(PacMan), args.Files[0]);
                }

                // Ensure the current window is active
                Window.Current.Activate();
            }
            else
            {
                await CallLoginPage();
                if (isRootFrame)
                {
                    rootFrame.Navigate(typeof(PacMan), args.Files[0]);
                }
                else
                {
                    rootFrame.Navigate(typeof(MainPage), args);
                }
            }
        }

        private async Task<bool> CallLoginPage()
        {
            if (Helper.build >= 10586 && (await UserConsentVerifier.CheckAvailabilityAsync()) == UserConsentVerifierAvailability.Available && AppSettings.LoadSettings("LoginTogReg", true) && !Globals.userVerified)
            {
                rootFrame.Navigate(typeof(Login));
                while (true)
                {
                    await Task.Delay(200);
                    if (Globals.userVerified) break;
                }
                return true;
            }
            return false;
        }
    }
}
