﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

// This code is based on a Roblox number formatter I wrote myself a while ago.
// I couldn't seem to find an existing solution for this, so that's why I used it.
// It's probably not the most efficient, but it works.
namespace CircleClicker.Utils.Converters
{
    /// <summary>
    /// Formats numbers with short suffixes.<br />
    /// Numbers between 1,000 and 999,999 will be formatted with the default formatter.<br />
    /// <br />
    /// Examples:
    /// <list type="table">
    ///     <item>1.234 -> 1.23</item>
    ///     <item>12.34 -> 12.3</item>
    ///     <item>123456 -> 123,456</item>
    ///     <item>1234567 -> 1.23 M</item>
    ///     <item>1e100 -> 10.0 DTg</item>
    /// </list>
    /// </summary>
    public class SuffixFormatter : IFormatProvider, ICustomFormatter, IValueConverter
    {
        public const int MinSignificantDigits = 3;

        /// <summary>
        /// An array of all suffixes up to uncentillion/UCe (since the <see cref="double"/> <see cref="double.MaxValue">limit</see> is around 180 uncentillion).
        /// </summary>
        public static readonly List<string> Suffixes = ["", " K", " M", " B", " T"];

        /// <summary>
        /// Precomputes all required suffixes.
        /// </summary>
        static SuffixFormatter()
        {
            // These suffixes are from the EternityNum 2 (and the older BigNum) Roblox library.
            string[] firstOnes = ["", "U", "D", "T", "Qd", "Qn", "Sx", "Sp", "Oc", "No"];
            string[] secondOnes = ["", "De", "Vg", "Tg", "qg", "Qg", "sg", "Sg", "Og", "Ng", "Ce"];

            for (int i = Suffixes.Count; i < 103; i++)
            {
                int firstIndex = (i - 1) % firstOnes.Length;
                int secondIndex = (i - 1) / firstOnes.Length;
                Suffixes.Add($" {firstOnes[firstIndex]}{secondOnes[secondIndex]}");
            }

            _ = Suffixes;
        }

        public object? GetFormat(Type? formatType)
        {
            return formatType == typeof(ICustomFormatter) ? this : null;
        }

        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            if (arg == null)
            {
                return string.Empty;
            }

            double number;
            try
            {
                number = (double)arg;
            }
            catch
            {
                return arg is IFormattable formattable
                    ? formattable.ToString(format, formatProvider)
                    : arg.ToString() ?? string.Empty;
            }

            if (!double.IsFinite(number))
            {
                // Format NaN and infinities with the default number formatter
                return number.ToString(format, formatProvider);
            }

            double numAbs = Math.Abs(number);
            if (numAbs >= 1_000 && numAbs < 1_000_000)
            {
                // Format these numbers with the default number formatter (and the format N0, which will seperate it with commas)
                return Math.Floor(number).ToString(format ?? "N0", formatProvider);
            }

            int numLog10 = (int)Math.Floor(Math.Max(Math.Log10(numAbs), 0)); // Effectively the number of digits in the given number, minus 1
            int index = numLog10 / 3; // The index into the suffixes list
            string suffix = Suffixes[index]; // The suffix to use

            int numDecimals = Math.Max(MinSignificantDigits - 1 - (numLog10 % 3), 0); // The number of digits to display after the dot
            double numberPart = number / Math.Pow(1000, index);

            if (format == null)
            {
                double precisionFactor = Math.Pow(10, numDecimals);
                numberPart = Math.Floor(numberPart * precisionFactor) / precisionFactor;
            }

            return numberPart.ToString(format ?? ("N" + numDecimals), formatProvider) + suffix;
        }

        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            return Format(parameter?.ToString(), value, culture);
        }

        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            string? input = value?.ToString();
            if (input == null)
            {
                return null;
            }

            string[] parts = input.Split(" ");
            if (parts.Length == 0 || !double.TryParse(parts[0], culture, out double number))
            {
                return null;
            }

            if (parts.Length >= 2)
            {
                string suffixPart = " " + parts[1];
                int index = Suffixes.FindIndex(v => v == (" " + parts[1]));
                _ = index;
                if (index >= 0)
                {
                    number *= Math.Pow(1000, index);
                }
            }

            return number;
        }
    }
}