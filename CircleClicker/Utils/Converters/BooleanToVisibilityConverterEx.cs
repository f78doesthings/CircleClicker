using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CircleClicker.Utils.Converters
{
    /// <summary>
    /// Like <see cref="BooleanToVisibilityConverter"/>, but allows you to specify what visibility to use for <see langword="false"/> in the converter parameter.
    /// </summary>
    public class BooleanToVisibilityConverterEx : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            try
            {
                Visibility on = Visibility.Visible;
                Visibility off = Visibility.Collapsed;
                string[]? parts = parameter?.ToString()?.Split(";");

                if (parts != null)
                {
                    switch (parts.Length)
                    {
                        case 1:
                            off = Enum.Parse<Visibility>(parts[^1]);
                            break;
                        case 2:
                            on = Enum.Parse<Visibility>(parts[0]);
                            goto case 1;
                        default:
                            throw new ArgumentException(
                                "Converter parameter should be a string like this: \"[TrueVisibility;]FalseVisibility\"",
                                nameof(parameter)
                            );
                    }
                }

                return System.Convert.ToBoolean(value) ? on : off;
            }
            catch
            {
                return null;
            }
        }

        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            return value == null ? null : (Visibility)value == Visibility.Visible;
        }
    }
}
