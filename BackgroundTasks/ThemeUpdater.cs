using CMDInjectorHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Popups;

namespace BackgroundTasks
{
    public sealed class ThemeUpdater : IBackgroundTask
    {
        private BackgroundTaskDeferral _Deferral { get; set; }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _Deferral = taskInstance.GetDeferral();

            Helper.Init();

            if (Helper.LocalSettingsHelper.LoadSettings("AutoThemeMode", 0) != 0)
            {
                var lightTime = Helper.LocalSettingsHelper.LoadSettings("AutoThemeLight", "06:00");
                var darkTime = Helper.LocalSettingsHelper.LoadSettings("AutoThemeDark", "18:00");
                var currentTime = DateTime.Now.ToString("HH:mm");

                string useLightTheme;
                string controlPanelTheme;

                if (Helper.IsStrAGraterThanStrB(darkTime, lightTime, ':'))
                {
                    useLightTheme = (Helper.IsStrAGraterThanStrB(currentTime, lightTime, ':') && Helper.IsStrAGraterThanStrB(darkTime, currentTime, ':')) ? "00000001" : "00000000";
                    controlPanelTheme = (useLightTheme == "00000001") ? "00000000" : "00000001";
                }
                else
                {
                    useLightTheme = (Helper.IsStrAGraterThanStrB(currentTime, darkTime, ':') && Helper.IsStrAGraterThanStrB(lightTime, currentTime, ':')) ? "00000000" : "00000001";
                    controlPanelTheme = (useLightTheme == "00000001") ? "00000000" : "00000001";
                }

                Helper.RegistryHelper.SetRegValue(
                    Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE,
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize",
                    "SystemUsesLightTheme",
                    Helper.RegistryHelper.RegistryType.REG_DWORD,
                    useLightTheme
                );
                Helper.RegistryHelper.SetRegValue(
                    Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE,
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize",
                    "AppsUseLightTheme",
                    Helper.RegistryHelper.RegistryType.REG_DWORD,
                    useLightTheme
                );
                Helper.RegistryHelper.SetRegValue(
                    Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE,
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\Theme",
                    "CurrentTheme",
                    Helper.RegistryHelper.RegistryType.REG_DWORD,
                    controlPanelTheme
                );
            }

            _Deferral.Complete();
        }
    }
}
