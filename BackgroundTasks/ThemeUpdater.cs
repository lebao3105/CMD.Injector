using CMDInjectorHelper;
using System;
using Windows.ApplicationModel.Background;

namespace BackgroundTasks
{
    public sealed class ThemeUpdater : IBackgroundTask
    {
        private BackgroundTaskDeferral _Deferral { get; set; }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _Deferral = taskInstance.GetDeferral();

            Helper.Init();

            if (AppSettings.AutoThemeMode)
            {
                var lightTime = AppSettings.LoadSettings("AutoThemeLight", "06:00");
                var darkTime = AppSettings.LoadSettings("AutoThemeDark", "18:00");
                var currentTime = DateTime.Now.ToString("HH:mm");

                string useLightTheme = darkTime.IsGreaterThan(lightTime, ':')
                    ? (currentTime.IsGreaterThan(lightTime, ':') && darkTime.IsGreaterThan(currentTime, ':')).ToDWORDStr()
                    : (currentTime.IsGreaterThan(darkTime, ':') && lightTime.IsGreaterThan(currentTime, ':')).Toggle().ToDWORDStr();

                string controlPanelTheme = useLightTheme.DWORDFlagToBool().Toggle().ToDWORDStr();

                RegEdit.GoToKey(RegistryHive.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");

                RegEdit.SetRegValue("SystemUsesLightTheme", RegistryType.REG_DWORD, useLightTheme);
                RegEdit.SetRegValue("AppsUseLightTheme", RegistryType.REG_DWORD, useLightTheme);
                RegEdit.SetHKLMValue(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\Theme",
                    "CurrentTheme",
                    RegistryType.REG_DWORD,
                    controlPanelTheme
                );
            }

            _Deferral.Complete();
        }
    }
}
