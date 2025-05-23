﻿using CMDInjectorHelper;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace CMDInjector
{
    public sealed partial class SplashAnimation : Page
    {
        internal Rect splashImageRect; // Rect to store splash screen image coordinates
        internal bool dismissed = false; // Variable to track splash screen dismissal status

        private SplashScreen splash; // Variable to hold the splash screen object
        private double ScaleFactor; // Variable to hold the device scale factor

        public SplashAnimation(LaunchActivatedEventArgs e, bool loadState)
        {
            InitializeComponent();

            ExtendedSplashImage.Source = new BitmapImage(
                new Uri(
                    $"ms-appx:///Assets/Images/Splashscreens/{AppSettings.SplashAnim}" +
                     (IsSystemThemeDark() ? "_Dark.gif" : "_Light.gif")
                )
            );
            SplashProgressRing.Foreground = new SolidColorBrush(IsSystemThemeDark() ? Colors.White : Colors.Black);

            DismissExtendedSplash();

            Window.Current.SizeChanged += Current_SizeChanged;

            ScaleFactor = (double)DisplayInformation.GetForCurrentView().ResolutionScale / 100;
            splash = e.SplashScreen;

            if (splash != null)
            {
                splash.Dismissed += Splash_Dismissed;
                splashImageRect = splash.ImageLocation;
                PositionImage();
            }

            RestoreStateAsync(loadState);
        }

        private async void RestoreStateAsync(bool loadState)
        {
            if (loadState)
                await SuspensionManager.RestoreAsync();
        }

        private void PositionImage()
        {
            ExtendedSplashImage.SetValue(Canvas.LeftProperty, splashImageRect.Left);
            ExtendedSplashImage.SetValue(Canvas.TopProperty, splashImageRect.Top);
            ExtendedSplashImage.Width = splashImageRect.Width / ScaleFactor;
            ExtendedSplashImage.Height = splashImageRect.Height / ScaleFactor;
        }

        private void Splash_Dismissed(SplashScreen sender, object args)
        {
            dismissed = true;
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            if (splash != null)
            {
                splashImageRect = splash.ImageLocation;
                PositionImage();
            }
        }

        private async void DismissExtendedSplash()
        {
            // TODO: Bypass this delay using an application setting
            await Task.Delay(TimeSpan.FromSeconds(3));
            Helper.splashScreenDisplayed = true;
        }

        private bool IsSystemThemeDark()
        {
            return AppSettings.ThemeSettings ?
                    AppSettings.Theme == 0 :
                    Application.Current.RequestedTheme != ApplicationTheme.Light;
        }
    }
}
