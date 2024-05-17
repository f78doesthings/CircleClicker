using CircleClicker.Utils.Converters;
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
                """(?s)<(\/?)(?<tag>[\w\.\:]+)(?:\s+(?<name>[\w\.\:]+)\s*=\s*"?(?<value>[^"]*)"?)*\s*(\/?)>"""
            )]
            public static partial Regex ElementRegex();

            [GeneratedRegex(@"\n\s*\n")]
            public static partial Regex DoubleNewLineRegex();

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
                                // Attribute replacements
                                "color" => nameof(TextElement.Foreground),
                                "face" or "family" => nameof(TextElement.FontFamily),
                                "size" => nameof(TextElement.FontSize),
                                "weight" => nameof(TextElement.FontWeight),
                                "title" => nameof(FrameworkContentElement.ToolTip),
                                "href" => nameof(Hyperlink.NavigateUri),
                                var other => other
                            };

                            string newValue = value.Value;
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
        public static IEnumerable<Inline> ParseInlines(string text)
        {
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
        public static void AddRange(this IList collection, IEnumerable elements)
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
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> elements)
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
