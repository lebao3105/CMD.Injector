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
            if (!File.Exists(@"C:\Windows\System32\Boot\startup.bsc") || !File.Exists(@"C:\Windows\System32\cmd.exe") || !File.Exists(@"C:\Windows\System32\telnetd.exe"))
            {
                return "Make sure you have restored NDTKSvc and reboot the device.";
            }
            else if (File.Exists(@"C:\Windows\System32\Boot\startup.bsc") && !string.Equals(File.ReadAllText(@"C:\Windows\System32\Boot\startup.bsc"), File.ReadAllText($"{Helper.installedLocation.Path}\\Contents\\Startup\\startup.bsc")))
            {
                Helper.CopyFromAppRoot("Contents\\Startup\\startup.bsc", @"C:\Windows\System32\Boot\startup.bsc");
                return "The Bootsh service component has manually changed, or corrupted. Please reboot the device to fix it.";
            }
            else if (File.Exists(@"C:\Windows\System32\CMDInjectorFirstLaunch.dat"))
            {
                return "The system isn't rebooted to initialize the App after the first launch, please reboot the device.";
            }
            else if (Helper.RegistryHelper.GetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Services\\Bootsh", "Start", Helper.RegistryHelper.RegistryType.REG_DWORD) == "00000004" && Helper.RegistryHelper.GetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Control\\CI", "UMCIAuditMode", Helper.RegistryHelper.RegistryType.REG_DWORD) == "00000000")
            {
                return "The Bootsh service & UMCIAuditMode is disabled. Please enable it from the App settings and reboot the device.";
            }
            else if (Helper.RegistryHelper.GetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Services\\Bootsh", "Start", Helper.RegistryHelper.RegistryType.REG_DWORD) == "00000004")
            {
                return "The Bootsh service is disabled. Please enable it from the App settings and reboot the device.";
            }
            else if (Helper.RegistryHelper.GetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Control\\CI", "UMCIAuditMode", Helper.RegistryHelper.RegistryType.REG_DWORD) == "00000000")
            {
                return "The UMCIAuditMode is disabled. Please enable it from the App settings and reboot the device.";
            }
            else
            {
                return "Something went wrong, try restarting the App or the device.";
            }
        }

        public static bool IsConnected()
        {
            if (!File.Exists(@"C:\Windows\System32\Boot\startup.bsc") || !File.Exists(@"C:\Windows\System32\cmd.exe") || !File.Exists(@"C:\Windows\System32\telnetd.exe"))
            {
                return false;
            }
            else if (File.Exists(@"C:\Windows\System32\Boot\startup.bsc") && !string.Equals(File.ReadAllText(@"C:\Windows\System32\Boot\startup.bsc"), File.ReadAllText($"{Helper.installedLocation.Path}\\Contents\\Startup\\startup.bsc")))
            {
                Helper.CopyFromAppRoot("Contents\\Startup\\startup.bsc", @"C:\Windows\System32\Boot\startup.bsc");
                return false;
            }
            else if (File.Exists(@"C:\Windows\System32\CMDInjectorFirstLaunch.dat"))
            {
                return false;
            }
            else if (Helper.RegistryHelper.GetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Services\\Bootsh", "Start", Helper.RegistryHelper.RegistryType.REG_DWORD) == "00000004" && Helper.RegistryHelper.GetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Control\\CI", "UMCIAuditMode", Helper.RegistryHelper.RegistryType.REG_DWORD) == "00000000")
            {
                return false;
            }
            else if (Helper.RegistryHelper.GetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Services\\Bootsh", "Start", Helper.RegistryHelper.RegistryType.REG_DWORD) == "00000004")
            {
                return false;
            }
            else if (Helper.RegistryHelper.GetRegValue(Helper.RegistryHelper.RegistryHive.HKEY_LOCAL_MACHINE, "SYSTEM\\CurrentControlSet\\Control\\CI", "UMCIAuditMode", Helper.RegistryHelper.RegistryType.REG_DWORD) == "00000000")
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
