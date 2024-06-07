using CircleClicker.Utils;
using Xunit.Abstractions;

namespace CircleClicker.Tests
{
    public class ParseInlinesTest(ITestOutputHelper output)
    {
        [Theory]
        [InlineData(
            """<b size=24 FontWeight="Black">bold</b> <i>italic</i> <br />""",
            """<Bold FontSize="24" FontWeight="Black">bold</Bold> <Italic>italic</Italic> <LineBreak/>"""
        )]
        public void TestReplaceElements(string input, string expectedResult)
        {
            output.PrintRegexMatches(Helpers.Internal.ElementRegex(), input);

            string result = Helpers.Internal.ReplaceElements(input);
            Assert.Equal(expectedResult, result);
        }
    }
}
