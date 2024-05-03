using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CircleClicker.Utils.Converters
{
    public class StringFormatMultiConverter : IMultiValueConverter
    {
        public object? Convert(
            object?[] values,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            string? format = parameter?.ToString();
            return format != null
                ? string.Format(culture, format, values)
                : DependencyProperty.UnsetValue;
        }

        public object?[] ConvertBack(
            object? value,
            Type[] targetTypes,
            object? parameter,
            CultureInfo culture
        )
        {
            throw new NotSupportedException();
        }
    }
}
