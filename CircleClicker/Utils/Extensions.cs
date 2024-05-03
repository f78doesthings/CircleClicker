using System.Drawing;
using System.Numerics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircleClicker.Utils.Converters;

namespace CircleClicker.Utils
{
    /// <summary>
    /// Provides various extensions to existing types.
    /// </summary>
    public static class Extensions
    {
        private static readonly SuffixFormatter _suffixFormatter = new();

        /// <summary>
        /// Formats a number using <see cref="SuffixFormatter"/>.
        /// </summary>
        public static string FormatSuffixes<T>(
            this T number,
            string? format = null,
            IFormatProvider? formatProvider = null
        )
            where T : INumber<T>
        {
            return _suffixFormatter.Format(format, number, formatProvider ?? App.Culture);
        }

        /// <summary>
        /// Converts this stock icon to an <see cref="ImageSource"/> for use with <see cref="System.Windows.Controls.Image"/>.
        /// </summary>
        public static BitmapSource ToImageSource(this Native.SHSTOCKICONINFO psii)
        {
            return Imaging.CreateBitmapSourceFromHIcon(
                psii.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );
        }

        /// <summary>
        /// Converts this <see cref="Icon"/> to an <see cref="ImageSource"/> for use with <see cref="System.Windows.Controls.Image"/>.
        /// </summary>
        public static BitmapSource ToImageSource(this Icon icon)
        {
            return Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );
        }
    }
}
