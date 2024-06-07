using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CircleClicker.Models;
using CircleClicker.Models.Database;
using CircleClicker.UI.Controls;
using CircleClicker.Utils;
using Microsoft.EntityFrameworkCore;

namespace CircleClicker.UI.Windows
{
    /// <summary>
    /// Interaction logic for LeaderboardWindow.xaml
    /// </summary>
    public partial class LeaderboardWindow : Window
    {
        /// <summary>
        /// The maximum number of users to display on a leaderboard.
        /// </summary>
        public const int MaxItems = 10;

        private static Main Main => Main.Instance;

        private static IEnumerable<(
            ReadOnlyDependency dependency,
            string header,
            Func<double, string> formatter
        )> Tabs =>
            [
                (
                    ReadOnlyDependency.LifetimeCircles,
                    "Circles",
                    v => Currency.Circles.Format(v, "R+")
                ),
                (
                    ReadOnlyDependency.LifetimeTriangles,
                    "Triangles",
                    v => Currency.Triangles.Format(v, "R+")
                ),
                (
                    ReadOnlyDependency.LifetimeSquares,
                    "Squares",
                    v => Currency.Squares.Format(v, "R+")
                ),
                (ReadOnlyDependency.LifetimeClicks, "Clicks", v => v.ToString("N0", App.Culture))
            ];

        public LeaderboardWindow()
        {
            InitializeComponent();

            Main.DB.Users.Load();
            Main.DB.Saves.Load();

            tc_leaderboards.Items.Clear();
            foreach (var (dependency, header, format) in Tabs)
            {
                TabItem tab = new() { Header = header };
                ItemsControl items = new();

                List<Save> saves = [];
                foreach (User user in Main.DB.Users)
                {
                    Save? save = user.Saves.MaxBy(dependency.GetValue);
                    if (save != null)
                    {
                        saves.Add(save);
                    }
                }
                saves.Sort((a, b) => dependency.GetValue(a).CompareTo(dependency.GetValue(b)));
                saves.Reverse();

                for (int i = 0; i < MaxItems && i < saves.Count; i++)
                {
                    Save save = saves[i];
                    Grid grid = new();
                    if (i % 2 == 0)
                    {
                        grid.Background = TryFindResource("ControlBrush") as Brush;
                    }

                    Grid inner = new() { Margin = new Thickness(15, 8, 15, 8) };
                    inner.ColumnDefinitions.AddRange(
                        [
                            new ColumnDefinition() { Width = new GridLength(25) },
                            new ColumnDefinition(),
                            new ColumnDefinition() { Width = GridLength.Auto }
                        ]
                    );

                    TextBlock position =
                        new()
                        {
                            Text = $"{i + 1:N0}.",
                            FontWeight = FontWeights.Black,
                            Foreground = i switch
                            {
                                0 => TryFindResource("TriangleBrush") as Brush,
                                1 => new LinearGradientBrush(Colors.White, Colors.Silver, 90),
                                2 => TryFindResource("AccentBrush") as Brush,
                                _ => TryFindResource("DimForegroundBrush") as Brush,
                            }
                        };
                    TextBlock username = new() { Text = save.User.Name };
                    RichTextBlock value =
                        new()
                        {
                            Text = format(dependency.GetValue(save)),
                            FontWeight = FontWeights.Bold
                        };

                    inner.Children.AddRange(position, username, value);
                    Grid.SetColumn(username, 1);
                    Grid.SetColumn(value, 2);

                    grid.Children.Add(inner);
                    items.Items.Add(grid);
                }

                tab.Content = items;
                tc_leaderboards.Items.Add(tab);
            }
        }
    }
}
