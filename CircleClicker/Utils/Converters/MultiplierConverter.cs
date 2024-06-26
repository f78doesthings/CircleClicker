﻿using System.Globalization;
using System.Windows.Data;

namespace CircleClicker.Utils.Converters
{
    public class MultiplierConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            return value == null
                ? null
                : (double)value * System.Convert.ToDouble(parameter, culture);
        }

        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            throw new NotSupportedException();
        }
    }
}
