using System;
using Windows.UI.Xaml;

namespace CMDInjectorHelper
{
    public static class AppSettings
    {
        #region Setters
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
        #endregion

        #region Getters
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

        private static int GetIntValue(string key, int defaultValue = 0) => LoadSettings(key, defaultValue);

        private static bool GetBooleanValue(string key, bool defaultValue = false) => LoadSettings(key, defaultValue);

        private static string GetStringValue(string key, string defaultValue) => LoadSettings(key, defaultValue);
        #endregion

        public static bool AutoThemeMode
        {
            get => GetBooleanValue("AutoThemeMode");
            set => SaveSettings("AutoThemeMode", value);
        }

        public static bool GlanceAutoColorEnabled
        {
            get => GetBooleanValue("GlanceAutoColorEnabled");
            set => SaveSettings("GlanceAutoColorEnabled", value);
        }

        public static int GlanceColorIndex
        {
            get => GetIntValue("GlanceColorIndex");
            set => SaveSettings("GlanceColorIndex", value);
        }

        #region Automatic wallpaper change
        public static bool StartWallSwitch
        {
            get => GetBooleanValue("StartWallSwitch");
            set => SaveSettings("StartWallSwitch", value);
        }

        public static int StartWallTrigger
        {
            get => GetIntValue("StartWallTrigger");
            set => SaveSettings("StartWallTrigger", value);
        }

        public static int StartWallInterval
        {
            get => GetIntValue("StartWallInterval", 15);
            set => SaveSettings("StartWallInterval", value);
        }

        public static string StartWallTime
        {
            get => GetStringValue(
                "StartWallTime",
                DateTime.Now.Subtract(TimeSpan.FromMinutes(StartWallInterval)).ToString("dd/MM/yy HH:mm:ss")
            );
            set => LoadSettings("StartWallTime", value);
        }

        public static int StartWallImagePosition
        {
            get => GetIntValue("StartWallImagePosition");
            set => SaveSettings("StartWallImagePosition", value);
        }
        #endregion

        public static bool CommandsTextWrap
        {
            get => GetBooleanValue("CommandsTextWrap");
            set => SaveSettings("CommandsTextWrap", value);
        }

        public static string InitialLaunch
        {
            get => GetStringValue("InitialLaunch", "0.0.0.0");
            set => SaveSettings("InitialLaunch", value);
        }

        public static bool TempInjection
        {
            get => GetBooleanValue("TempInjection");
            set => SaveSettings("TempInjection", value);
        }


        public static bool AskCapPermission
        {
            get => GetBooleanValue("AskCapPermission");
            set => SaveSettings("AskCapPermission", value);
        }

        public static bool FirstLaunch
        {
            get => GetBooleanValue("FirstLaunch", true);
            set => SaveSettings("FirstLaunch", value);
        }

        public static string AutoThemeLight
        {
            get => GetStringValue("AutoThemeLight", "06:00");
            set => SaveSettings("AutoThemeLight", value);
        }

        public static string AutoThemeDark
        {
            get => GetStringValue("AutoThemeDark", "18:00");
            set => SaveSettings("AutoThemeDark", value);
        }

        public static bool ThemeSettings
        {
            get => GetBooleanValue("ThemeSettings");
            set => SaveSettings("ThemeSettings", value);
        }

        public static int Theme
        {
            get => GetIntValue("Theme");
            set => SaveSettings("Theme", value);
        }

        #region Splash screen
        public static string SplashAnim
        {
            get => GetStringValue("SplashAnim", "Glitch");
            set => SaveSettings("SplashAnim", value);
        }

        public static bool SplashScreen
        {
            get => GetBooleanValue("SlashScreen", true);
            set => SaveSettings("SplashScreen", value);
        }

        public static int SplashAnimIndex
        {
            get => GetIntValue("SplashAnimIndex");
            set => SaveSettings("SplashAnimIndex", 0);
        }
        #endregion

        public static bool LoginTogReg // <- pretty much a typo?
        {
            get => GetBooleanValue("LoginTogReg", true);
            set => SaveSettings("LoginTogReg", value);
        }

        public static bool HaveCamera
        {
            get => GetBooleanValue("HaveCamera", false);
            set => SaveSettings("HaveCamera", value);
        }
    }
}
