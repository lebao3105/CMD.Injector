using System;
using System.IO;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CMDInjectorHelper
{
    public static class IntExtension
    {
        /// <summary>
        /// Returns <see cref="true"/> if <paramref name="value"/> is 1.
        /// </summary>
        public static bool ToBoolean(this int value) => value == 1;
    }

    public static class BoolExtension
    {
        /// <summary>
        /// Returns 1 if value is true, 0 otherwise.
        /// </summary>
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

        public static bool RefToggle(this ref bool value, bool noOverride = false)
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

        public static TextWrapping ToTextWrapping(this bool value) => value ? TextWrapping.Wrap : TextWrapping.NoWrap;
    }

    public static class StringExtension
    {
        public static int ToInt32(this string value) => Convert.ToInt32(value);
        public static int ToInt32(this string value, int baseNum) => Convert.ToInt32(value, baseNum);

        public static uint ToUInt32(this string value) => Convert.ToUInt32(value);
        public static uint ToUInt32(this string value, int baseNum) => Convert.ToUInt32(value, baseNum);

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

        /// <summary>
        /// Checks if the string is only "00000000" (without quotes).
        /// Ideal for Registry gets.
        /// </summary>
        public static bool IsDWORDStrFullZeros(this string value) => value == "00000000";

        /// <summary>
        /// Checks if the string is only "00000001" (without quotes).
        /// Ideal for Registry gets.
        /// </summary>
        public static bool DWORDFlagToBool(this string value) => value == "00000001";

        public static bool IsAFileIn(this string self, string dir) => File.Exists($"{dir}\\{self}");
        public static bool IsADirIn(this string self, string dir) => Directory.Exists($"{dir}\\{self}");

        public static bool IsAFileInSystem32(this string self) => self.IsAFileIn("C:\\Windows\\System32");
        public static bool IsADirInSystem32(this string self) => self.IsADirIn("C:\\Windows\\System32");

        public static string ReadFromDir(this string self, string parent) => File.ReadAllText($"{parent}\\{self}");

        public static bool HasContent(this string self) => !string.IsNullOrWhiteSpace(self);
    }

    public static class VisibilityExtensions
    {
        public static Visibility Negate(this ref Visibility self)
        {
            self = self.IsVisible() ? Visibility.Collapsed : Visibility.Visible;
            return self;
        }

        public static bool IsVisible(this Visibility self) => self == Visibility.Visible;
    }

    public static class UIExtensions
    {
        public static Panel AddChildren(this Panel self, FrameworkElement elm)
        {
            self.Children.Add(elm);
            return self;
        }

        public static FrameworkElement Visible(this FrameworkElement self, bool visible = true)
        {
            self.Visibility = visible.ToVisibility();
            return self;
        }

        public static FrameworkElement Collapse(this FrameworkElement self) => Visible(self, false);
    }

    public static class AppExtension
    {
        public static object GetResource(this Application self, string name)
            => Application.Current.Resources[name];
    }
}