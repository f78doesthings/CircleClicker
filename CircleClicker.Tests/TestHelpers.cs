using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace CircleClicker.Tests
{
    public static class TestHelpers
    {
        public static void PrintRegexMatches(
            this ITestOutputHelper output,
            Regex regex,
            string input
        )
        {
            output.WriteLine("Matches:");
            foreach (object? item in regex.Matches(input))
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
        }
    }
}
