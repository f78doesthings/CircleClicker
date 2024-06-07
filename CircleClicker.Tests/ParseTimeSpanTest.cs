using CircleClicker.Utils;
using Xunit.Abstractions;

namespace CircleClicker.Tests
{
    public class ParseTimeSpanTest(ITestOutputHelper output)
    {
        public static readonly TheoryData<string, TimeSpan> TestData =
            new()
            {
                { "7d", new TimeSpan(7, 0, 0, 0) },
                { "1 day 2H 3m4s", new TimeSpan(1, 2, 3, 4) }
            };

        [Theory]
        [MemberData(nameof(TestData))]
        public void TestParseTimeSpan(string input, TimeSpan expectedResult)
        {
            output.PrintRegexMatches(Helpers.Internal.TimeSpanRegex(), input);

            TimeSpan result = Helpers.ParseTimeSpan(input);
            Assert.Equal(expectedResult, result);
        }
    }
}
