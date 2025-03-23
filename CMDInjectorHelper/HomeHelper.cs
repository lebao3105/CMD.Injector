using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMDInjectorHelper
{
    public static class HomeHelper
    {
        public static bool IsCMDInjected()
        {
            // This versus doing a bunch of File.Exists, which is faster?
            var topFiles = Directory.EnumerateFiles(@"C:\Windows\System32");
            var bootFiles = Directory.EnumerateFiles(@"C:\Windows\System32\Boot");
            var enUSFiles = Directory.EnumerateFiles(@"C:\Windows\System32\en-US");

            var topFiles_needs = new List<string> {
                "cmd.exe", "PowerTool.exe", "bootshsvc.dll", "bcdedit.exe", "CheckNetIsolation.exe", "MinDeployAppX.exe", "more.com",
                "reg.exe", "ScreenCapture.exe", "shutdown.exe", "telnetd.exe", "TestNavigationApi.exe", "ICacls.exe", "takeown.exe",
                "xcopy.exe", "AppXTest.Common.Feature.DeployAppx.dll", "SendKeys.exe", "InputProcessorClient.dll", "sleep.exe"
            };

            var bootFiles_needs = new List<string> {
                "startup.bsc", "bootshsvc.dll.mui"
            };

            var enUSFiles_needs = new List<string> {
                "sort.exe.mui", "CheckNetIsolation.exe.mui", "cmd.exe.mui", "reg.exe.mui"
            };

            return (topFiles_needs.Intersect(topFiles).Count() == topFiles_needs.Count()) &&
                   (bootFiles_needs.Intersect(bootFiles).Count() == bootFiles_needs.Count()) &&
                   (enUSFiles_needs.Intersect(enUSFiles).Count() == enUSFiles_needs.Count());
        }

        public static string GetTelnetTroubleshoot()
        {
            if ("CMDInjectorFirstLaunch.dat".IsAFileInSystem32())
                return "The system isn't rebooted to initialize the App after the first launch, please reboot the device.";

            if ("Boot\\startup.bsc".IsAFileInSystem32())
            {
                if (!string.Equals("Boot\\startup.bsc".ReadFromDir("C:\\Windows\\System32"), "Contents\\Startup\\startup.bsc".ReadFromDir(Globals.installedLocation.Path)))
                {
                    FilesHelper.CopyToSystem32FromAppRoot("Contents\\Startup", "startup.bsc", "Boot\\startup.bsc");
                    return "The Bootsh service component has manually changed, or corrupted. Please reboot the device to fix it.";
                }

                if (!"cmd.exe".IsAFileInSystem32() || !"telnetd.exe".IsAFileInSystem32())
                    return "Make sure you have restored NDTKSvc and reboot the device.";

                RegEdit.GoToKey(RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Services\\Bootsh");
                bool isBootShDisabled = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Services\\Bootsh", "Start", RegistryType.REG_DWORD) == "00000004";

                bool isUMCIAuditModeDisabled = RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Control\\CI", "UMCIAuditMode", RegistryType.REG_DWORD) == "00000000";

                if (isBootShDisabled && isUMCIAuditModeDisabled)
                    return "The Bootsh service & UMCIAuditMode are disabled. Please enable them from the App settings and reboot the device.";
                else if (isBootShDisabled)
                    return "The Bootsh service is disabled. Please enable it from the App settings and reboot the device.";
                else if (isUMCIAuditModeDisabled)
                    return "The UMCIAuditMode is disabled. Please enable it from the App settings and reboot the device.";
            }
            else
            {
                return "Make sure you have restored NDTKSvc and reboot the device.";
            }

            return "Something went wrong, try restarting the App or the device.";
        }

        public static bool IsConnected()
        {
            if ("CMDInjectorFirstLaunch.dat".IsAFileInSystem32())
                return false;

            if ("Boot\\startup.bsc".IsAFileInSystem32())
            {
                if (!string.Equals("Boot\\startup.bsc".ReadFromDir("C:\\Windows\\System32"), "Contents\\Startup\\startup.bsc".ReadFromDir(Globals.installedLocation.Path)))
                {
                    FilesHelper.CopyToSystem32FromAppRoot("Contents\\Startup", "startup.bsc", "Boot\\startup.bsc");
                    return false;
                }

                if (!"cmd.exe".IsAFileInSystem32() || !"telnetd.exe".IsAFileInSystem32())
                    return false;

                RegEdit.GoToKey(RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Services\\Bootsh");
                if (RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Services\\Bootsh", "Start", RegistryType.REG_DWORD) == "00000004") return false;

                if (RegEdit.GetRegValue(
                    RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Control\\CI", "UMCIAuditMode", RegistryType.REG_DWORD) == "00000000") return false;
            }

            return true;
        }
    }
}
