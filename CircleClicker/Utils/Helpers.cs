using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircleClicker.Utils.Converters;

namespace CircleClicker.Utils
{
    /// <summary>
    /// Provides various helper methods as well as extensions to existing types.
    /// </summary>
    public static partial class Helpers
    {
        private static readonly SuffixFormatter _suffixFormatter = new();

        #region Helper methods
        /// <summary>
        /// Internal helper methods. Should not be used in code outside of the Helpers class; they are only exposed to be used in tests.
        /// </summary>
        public static partial class Internal
        {
            [GeneratedRegex(
                """(?s)<(\/?)(?<tag>\w+\:?\w*)(?:\.\w+)?(?:\s+(?<name>[\w\.\:]+)\s*=\s*"?(?<value>[^"]*)"?)*\s*(\/?)>"""
            )]
            public static partial Regex ElementRegex();

            [GeneratedRegex(@"\n\s*\n")]
            public static partial Regex DoubleNewLineRegex();

            [GeneratedRegex("""res:(\w+)""")]
            public static partial Regex StaticResourceRegex();

            [GeneratedRegex(
                @"(?i)^(?:\s*(?<number>\d+)\s*(?<unit>d(ays?)?|h(?:(?:ou)?rs?)?|m(?:in(?:ute)?s?)?|s(?:ec(?:ond)?s?)?|m(?:illi)?s(?:ec(?:ond)?s?)?)\s*)+$"
            )]
            public static partial Regex TimeSpanRegex();

            public static string EvaluateElement(Match match)
            {
                if (match.Groups.TryGetValue("tag", out Group? tag))
                {
                    string newTag = tag.Value switch
                    {
                        // Tag replacements
                        "b" => nameof(Bold),
                        "i" => nameof(Italic),
                        "u" => nameof(Underline),
                        "span" or "font" => nameof(Span),
                        "br" => nameof(LineBreak),
                        "a" => nameof(Hyperlink),
                        "div" => nameof(InlineUIContainer),
                        var other => other
                    };

                    string newAttributes = "";
                    if (
                        match.Groups.TryGetValue("name", out Group? names)
                        && match.Groups.TryGetValue("value", out Group? values)
                    )
                    {
                        foreach (
                            (Capture name, Capture value) in names.Captures.Zip(values.Captures)
                        )
                        {
                            string newName = name.Value switch
                            {
                                // Attribute name replacements
                                "color" => nameof(TextElement.Foreground),
                                "face" or "family" => nameof(TextElement.FontFamily),
                                "size" => nameof(TextElement.FontSize),
                                "weight" => nameof(TextElement.FontWeight),
                                "title" => nameof(FrameworkContentElement.ToolTip),
                                "href" => nameof(Hyperlink.NavigateUri),
                                var other => other
                            };

                            // Replaces res:... with {StaticResource ...}
                            string newValue = StaticResourceRegex()
                                .Replace(value.Value, "{StaticResource $1}");
                            if (newName == nameof(TextElement.FontWeight))
                            {
                                newValue = newValue switch
                                {
                                    // Replaces font weight numbers with names as the FontWeight property does not support numbers
                                    "100" => nameof(FontWeights.Thin),
                                    "200" => nameof(FontWeights.ExtraLight),
                                    "300" => nameof(FontWeights.Light),
                                    "400" => nameof(FontWeights.Regular),
                                    "500" => nameof(FontWeights.Medium),
                                    "600" => nameof(FontWeights.SemiBold),
                                    "700" => nameof(FontWeights.Bold),
                                    "800" => nameof(FontWeights.ExtraBold),
                                    "900" => nameof(FontWeights.Black),
                                    "950" => nameof(FontWeights.ExtraBlack),
                                    var other => other
                                };
                            }

                            newAttributes += $" {newName}=\"{newValue}\"";
                        }
                    }

                    return match.Result($"<${{1}}{newTag}{newAttributes}${{2}}>");
                }

                return match.Value;
            }

            public static string ReplaceElements(string text)
            {
                return DoubleNewLineRegex()
                    .Replace(
                        ElementRegex().Replace(text, EvaluateElement),
                        "<LineBreak /><InlineUIContainer><Grid Height=\"10\" /></InlineUIContainer><LineBreak />" // Hacky paragraph break
                    )
                    .Replace("\n", "<LineBreak />");
            }
        }

        /// <summary>
        /// Parses the given text and returns a collection of <see cref="Inline"/>s for use with <see cref="TextBlock"/>.<br />
        /// If the parsing fails, the raw text will be returned.
        /// </summary>
        /// <remarks>
        /// This method replaces some HTML-like tags and attributes with WPF ones (see <see cref="Internal.EvaluateElement"/> for a full list).<br />
        /// <c>\n</c> is also replaced with <see cref="LineBreak"/>, and <c>\n\n</c> is replaced with a paragraph break.
        /// </remarks>
        /// <returns>
        /// A collection of <see cref="Inline"/>s, or if the parsing failed, the input text.
        /// </returns>
        public static IEnumerable<Inline> ParseInlines(string? text)
        {
            if (text == "" || text == null)
            {
                return [];
            }

            try
            {
                text = Internal.ReplaceElements(text);

                // https://stackoverflow.com/questions/53508956/wpf-c-sharp-how-to-set-formatted-text-in-textblock-using-text-property
                TextBlock textBlock = (TextBlock)
                    XamlReader.Parse(
                        $"""
                        <TextBlock xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                            {text}
                        </TextBlock>
                        """
                    );
                return [.. textBlock.Inlines]; // Must be enumerated in order to work.
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Could not parse inlines:\n{ex}\n\nText:\n{text}",
                    App.ErrorCategory
                );
                return
                [
#if DEBUG
                    new Run("(Could not parse inlines) ")
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.Red,
                    },
#endif
                    new Run(text)
                ];
            }
        }

        /// <summary>
        /// Parses the given string as a time span. Throws on failure.
        /// </summary>
        public static TimeSpan ParseTimeSpan(string? input)
        {
            ArgumentNullException.ThrowIfNull(input, nameof(input));

            try
            {
                var match = Internal.TimeSpanRegex().Match(input);
                if (
                    match.Groups.TryGetValue("number", out Group? numbers)
                    && match.Groups.TryGetValue("unit", out Group? units)
                )
                {
                    int days = 0;
                    int hours = 0;
                    int minutes = 0;
                    int seconds = 0;
                    int milliseconds = 0;
                    foreach (
                        (Capture numberCapture, Capture unit) in numbers.Captures.Zip(
                            units.Captures
                        )
                    )
                    {
                        int number = int.Parse(numberCapture.Value);
                        switch (unit.Value.ToLower())
                        {
                            case "d"
                            or "day"
                            or "days":
                                days += number;
                                break;
                            case "h"
                            or "hr"
                            or "hrs"
                            or "hour"
                            or "hours":
                                hours += number;
                                break;
                            case "m"
                            or "min"
                            or "mins"
                            or "minute"
                            or "minutes":
                                minutes += number;
                                break;
                            case "s"
                            or "sec"
                            or "secs"
                            or "second"
                            or "seconds":
                                seconds += number;
                                break;
                            case "ms"
                            or "millis"
                            or "msec"
                            or "msecs"
                            or "msecond"
                            or "mseconds"
                            or "millisec"
                            or "millisecs"
                            or "millisecond"
                            or "milliseconds":
                                milliseconds += number;
                                break;
                        }
                    }

                    return new TimeSpan(days, hours, minutes, seconds, milliseconds);
                }

                throw new ArgumentException(
                    "The input text is not in a valid format for Helpers.ParseTimeSpan.",
                    nameof(input)
                );
            }
            catch (Exception inner)
            {
                try
                {
                    return TimeSpan.Parse(input, App.Culture);
                }
                catch (Exception ex)
                {
                    throw new AggregateException(
                        "Could not parse the input text as a TimeSpan.",
                        ex,
                        inner
                    );
                }
            }
        }

        /// <summary>
        /// Returns the given time span in the format "<c>&lt;days&gt;</c>d <c>&lt;hours&gt;</c>h <c>&lt;minutes&gt;</c>m <c>&lt;seconds&gt;</c>s".
        /// </summary>
        public static string PrettyPrint(this TimeSpan timeSpan)
        {
            return timeSpan.ToString(@"d'd 'h'h 'm'm 's's'", App.Culture);
        }
        #endregion

        #region Extensions
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
        /// Adds the elements of the given collection to the end of the <see cref="IList"/>.
        /// </summary>
        public static void AddRange(this IList collection, params object[] elements)
        {
            foreach (object element in elements)
            {
                collection.Add(element);
            }
        }

        /// <summary>
        /// Adds the elements of the given collection to the end of the <see cref="ICollection{T}"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">The <see cref="ICollection{T}"/> is read-only.</exception>
        public static void AddRange<T>(this ICollection<T> collection, params T[] elements)
        {
            foreach (T element in elements)
            {
                collection.Add(element);
            }
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
        #endregion
    }
}
