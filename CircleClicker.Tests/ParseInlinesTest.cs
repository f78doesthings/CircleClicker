using CircleClicker.Utils;
using System.Text.RegularExpressions;
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
            output.WriteLine("Matches:");
            foreach (object? item in Helpers.Internal.ElementRegex().Matches(input))
            {
                if (item is Match match)
                {
                    output.WriteLine($"- {item}");
                    foreach (object? groupItem in match.Groups)
                    {
                        if (groupItem is Group group && group.Name != "0")
                        {
                            output.WriteLine(
                                $"  - {group.Name}:\n    - {string.Join("\n    - ", group.Captures)}"
                            );
                        }
                    }
                }
            }

            var result = Helpers.Internal.ReplaceElements(input);
            Assert.Equal(expectedResult, result);
        }
    }
}
