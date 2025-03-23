using System;
using System.IO;
using Windows.UI.Xaml;

namespace CMDInjectorHelper
{
    public static class BoolExtension
    {
        public static int ToInt(this bool value) => value ? 1 : 0;

        public static bool Toggle(this bool value, bool noOverride = false)
        {
            if (!noOverride)
            {
                value = !value;
                return value;
            }
            return !value;
        }

        public static Visibility ToVisibility(this bool value)
            => value ? Visibility.Visible : Visibility.Collapsed;
        // Visibility.Collapsed is set to 1 (true.ToInt()), Visible is set to 0 (false.ToInt()).

        /// <summary>
        /// Returns "00000001" if value is true, "00000000" otherwise.
        /// </summary>
        public static string ToDWORDStr(this bool value) => value ? "00000001" : "00000000";
    }

    public static class StringExtension
    {
        public static int ToInt32(this string value)
        {
            return Convert.ToInt32(value);
        }

        public static bool IsGreaterThan(this string strA, string strB, char separator)
        {
            var result = false;
            var stringA = strA.Trim().Split(separator);
            var stringB = strB.Trim().Split(separator);

            int length = Math.Max(stringA.Length, stringB.Length);

            for (int i = 0; i < length; i++)
            {
                var partOfA = int.Parse(stringA[i]);
                var partOfB = int.Parse(stringB[i]);

                if (partOfA != partOfB)
                {
                    result = partOfA > partOfB;
                    break;
                }
            }
            return result;
        }

        public static bool IsAFileInSystem32(this string self) => File.Exists($"C:\\Windows\\System32\\{self}");
        public static bool IsADirInSystem32(this string self) => Directory.Exists($"C:\\Windows\\System32\\{self}");
        public static string ReadFromDir(this string self, string parent) => File.ReadAllText($"{parent}\\{self}");
    }
}