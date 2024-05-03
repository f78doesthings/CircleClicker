using System.Globalization;
using CircleClicker.Utils.Converters;
using Xunit.Abstractions;

namespace CircleClicker.Tests
{
    public class SuffixFormatterTest(ITestOutputHelper output)
    {
        private static CultureInfo Culture => CultureInfo.InvariantCulture;
        private readonly SuffixFormatter _suffixFormatter = new();

        public static TheoryData<double, string> GetFormatTestData()
        {
            TheoryData<double, string> data =
                new()
                {
                    { 0, 0.ToString("N2", Culture) },
                    { 1, 1.ToString("N2", Culture) },
                    { 10, 10.ToString("N1", Culture) },
                    { 100, 100.ToString("N0", Culture) },
                    { 1000, 1000.ToString("N0", Culture) },
                    { 10000, 10000.ToString("N0", Culture) },
                    { 100000, 100000.ToString("N0", Culture) },
                };

            for (int e = 6; e <= 308; e++)
            {
                int i = e / 3;
                int precision = SuffixFormatter.MinSignificantDigits - 1 - (e % 3);
                double number = Math.Pow(10, e);
                double numberPart = Math.Pow(10, e % 3);
                data.Add(
                    number,
                    numberPart.ToString("N" + precision, Culture) + SuffixFormatter.Suffixes[i]
                );
            }

            return data;
        }

        public static TheoryData<double> GetConvertBackTestData()
        {
            var data = Enumerable.Range(0, 308).Select(v => Math.Pow(10, v));
            return new TheoryData<double>(data);
        }

        [Theory]
        [MemberData(nameof(GetFormatTestData))]
        public void TestFormat(double number, string expectedResult)
        {
            string result = _suffixFormatter.Format(null, number, Culture);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [MemberData(nameof(GetConvertBackTestData))]
        public void TestConvertBack(double number)
        {
            string formatted = _suffixFormatter.Format(null, number, Culture);
            double result =
                (double?)_suffixFormatter.ConvertBack(formatted, typeof(double?), null, Culture)
                ?? double.NaN;
            output.WriteLine($"{number} -> {formatted} -> {result}");
            Assert.Equal(number, result);
        }
    }
}
