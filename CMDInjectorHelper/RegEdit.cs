using System;
using System.Diagnostics;
using System.Text;

namespace CMDInjectorHelper
{
    public enum RegistryHive
    {
        HKEY_CLASSES_ROOT = 0,
        HKEY_LOCAL_MACHINE = 1,
        HKEY_CURRENT_USER = 2,
        HKEY_CURRENT_CONFIG = 3,
        HKEY_USERS = 4,
        HKEY_PERFORMANCE_DATA = 5,
        HKEY_DYN_DATA = 6,
        HKEY_CURRENT_USER_LOCAL_SETTINGS = 7
    }

    public enum RegistryType
    {
        REG_SZ = 1,
        REG_EXPAND_SZ = 2,
        REG_BINARY = 3,
        REG_DWORD = 4,
        REG_DWORD_BIG_ENDIAN = 5,
        REG_LINK = 6,
        REG_MULTI_SZ = 7,
        REG_RESOURCE_LIST = 8,
        REG_FULL_RESOURCE_DESCRIPTOR = 9,
        REG_RESOURCE_REQUIREMENTS_LIST = 10,
        REG_QWORD = 11,
    }

    public sealed class RegEdit
    {
        public static string CurrentKeyPath { get; private set; }
        public static RegistryHive CurrentHive { get; private set; }

        public static void GoToKey(RegistryHive hive, string path)
        {
            CurrentKeyPath = path;
            CurrentHive = hive;
        }

        #region Get Registry Values
        public static string GetRegValue(RegistryHive hKey, string subKey, string value, RegistryType type)
        {
            return Globals.oem.rget((uint)hKey, subKey, value, (uint)type);
        }
        
        public static string GetRegValue(string value, RegistryType type)
        {
            return GetRegValue(CurrentHive, CurrentKeyPath, value, type);
        }
        #endregion

        #region Set Registry Values
        public static void SetHKLMValue(string subkey, string value, RegistryType type, string buffer)
        {
            SetRegValue(RegistryHive.HKEY_LOCAL_MACHINE, subkey, value, type, buffer);
        }

        public static void SetHKLMValueEx(string subKey, string value, RegistryType type, string buffer)
        {
            SetRegValueEx(RegistryHive.HKEY_LOCAL_MACHINE, subKey, value, type, buffer);
        }

        public static void SetRegValue(string value, RegistryType type, string buffer)
        {
            Debug.Assert(
                !string.IsNullOrWhiteSpace(CurrentKeyPath),
                "RegEdit.GoToKey() is not used. Can not use the short overload of SetRegValue.");
            SetRegValue(CurrentHive, CurrentKeyPath, value, type, buffer);
        }

        public static void SetRegValue(RegistryHive hKey, string subKey, string value, RegistryType type, string buffer)
        {
            Globals.oem.rset((uint)hKey, subKey, value, (uint)type, buffer, 0);
        }

        public static uint SetRegValueEx(RegistryHive hKey, string subKey, string value, RegistryType type, string buffer)
        {
            byte[] byteArr = null;
            switch (type)
            {
                case RegistryType.REG_SZ:
                case RegistryType.REG_EXPAND_SZ:
                    byteArr = Encoding.Unicode.GetBytes(buffer + '\0');
                    break;
                case RegistryType.REG_BINARY:
                    byteArr = StringToByteArrayFastest(buffer);
                    break;
                case RegistryType.REG_DWORD:
                    byteArr = BitConverter.GetBytes(uint.Parse(buffer));
                    break;
                case RegistryType.REG_MULTI_SZ:
                    byteArr = Encoding.Unicode.GetBytes(buffer + '\0');
                    break;
            }
            return Globals.rpc.RegSetValue((uint)hKey, subKey, value, (uint)type, byteArr);
        }

        public static uint SetRegValueEx(string value, RegistryType type, string buffer)
        {
            Debug.Assert(
                !string.IsNullOrWhiteSpace(CurrentKeyPath),
                "RegEdit.GoToKey() is not used. Can not use the short overload of SetRegValueEx.");
            return SetRegValueEx(CurrentHive, CurrentKeyPath, value, type, buffer);
        }
        #endregion

        #region Helpers
        private static int GetHexVal(char hex)
        {
            var val = hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : val < 97 ? 55 : 87);
        }

        private static byte[] StringToByteArrayFastest(string hex)
        {
            try
            {
                if (hex.Length % 2 == 1)
                {
                    throw new Exception("The binary key cannot have an odd number of digits");
                }
                var arr = new byte[hex.Length >> 1];
                for (int i = 0; i < hex.Length >> 1; ++i)
                {
                    arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));
                }
                return arr;
            }
            catch (Exception ex)
            {
                Helper.ThrowException(ex);
                return null;
            }
        }
        #endregion
    }
}
