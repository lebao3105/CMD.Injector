using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace CMDInjectorHelper
{
    public class Settings
    {
        public static bool AutoThemeMode
        {
            get => GetBooleanValue("AutoThemeMode");
            set => Helper.LocalSettingsHelper.SaveSettings("AutoThemeMode", value);
        }

        public static bool GlanceAutoColorEnabled
        {
            get => GetBooleanValue("GlanceAutoColorEnabled");
            set => Helper.LocalSettingsHelper.SaveSettings("GlanceAutoColorEnabled", value);
        }

        public static int GlanceColorIndex
        {
            get => GetIntValue("GlanceColorIndex");
            set => Helper.LocalSettingsHelper.SaveSettings("GlanceColorIndex", value);
        }

        public static bool StartWallSwitch
        {
            get => GetBooleanValue("StartWallSwitch");
            set => Helper.LocalSettingsHelper.SaveSettings("StartWallSwitch", value);
        }

        public static bool StartWallTrigger
        {
            get => GetBooleanValue("StartWallTrigger");
            set => Helper.LocalSettingsHelper.SaveSettings("StartWallTrigger", value);
        }

        public static TextWrapping CommandsTextWrap
        {
            get => GetBooleanValue("CommandsTextWrap") ? TextWrapping.Wrap : TextWrapping.NoWrap;
            set => Helper.LocalSettingsHelper.SaveSettings("CommandsTextWrap", value == TextWrapping.Wrap);
        }

        public static string InitialLaunch
        {
            get => GetStringValue("InitialLaunch", "0.0.0.0");
            set => Helper.LocalSettingsHelper.SaveSettings("InitialLaunch", value);
        }

        public static bool TempInjection
        {
            get => GetBooleanValue("TempInjection");
            set => Helper.LocalSettingsHelper.SaveSettings("TempInjection", value);
        }


        public static bool AskCapPermission
        {
            get => GetBooleanValue("AskCapPermission");
            set => Helper.LocalSettingsHelper.SaveSettings("AskCapPermission", value);
        }

        public static bool FirstLaunch
        {
            get => GetBooleanValue("FirstLaunch", true);
            set => Helper.LocalSettingsHelper.SaveSettings("FirstLaunch", value);
        }

        private static int GetIntValue(string key, int defaultValue = 0) => Helper.LocalSettingsHelper.LoadSettings(key, defaultValue);

        private static bool GetBooleanValue(string key, bool defaultValue = false) => Helper.LocalSettingsHelper.LoadSettings(key, defaultValue);

        private static string GetStringValue(string key, string defaultValue) => Helper.LocalSettingsHelper.LoadSettings(key, defaultValue);
    }
}
