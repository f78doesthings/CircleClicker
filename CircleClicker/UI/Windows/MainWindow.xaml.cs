using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CircleClicker.Models;
using CircleClicker.Models.Database;
using CircleClicker.UI.Controls;
using CircleClicker.UI.Particles;
using CircleClicker.Utils.Audio;
using CircleClicker.Utils.Converters;
using Microsoft.EntityFrameworkCore;

namespace CircleClicker.UI.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Properties for use with bindings
#pragma warning disable CA1822 // Mark members as static
        public Main Main => Main.Instance;

        public Save Save =>
            Main.CurrentSave ?? throw new NullReferenceException("Main.CurrentSave is null.");

        /// <summary>
        /// Used to set the visibility of admin-only buttons, such as <see cref="btn_admin"/>.
        /// </summary>
        public bool IsAdmin => Main.CurrentUser?.IsAdmin == true;

        /// <summary>
        /// Used to enable or disable <see cref="btn_reincarnate"/>.
        /// </summary>
        public bool CanReincarnate => Currency.Squares.IsPending;

        /// <summary>
        /// Used to set the visibility of the progress bar in <see cref="btn_reincarnate"/>.
        /// </summary>
        public Visibility ShowReincarnateProgress =>
            Currency.Squares.IsPending ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>
        /// Used by <see cref="btn_reincarnate"/> to show a progress bar for the reincarnation button.
        /// </summary>
        public double ReincarnateProgress =>
            Currency.Squares.IsPending
                ? 100
                : Math.Log10((Save.TotalCircles / 10) + 1)
                    / Math.Log10((Main.ReincarnationCost.Value / 10) + 1)
                    * 100;

        /// <summary>
        /// The text to display on the <see cref="btn_reincarnate"/> button.
        /// </summary>
        public string PendingSquaresText =>
            Currency.Squares.IsPending
                ? "Reincarnate!"
                : $"Earn {Currency.Circles.Format(Main.ReincarnationCost.Value)} to unlock";
#pragma warning restore CA1822 // Mark members as static
        #endregion

        private readonly DispatcherTimer _timer;
        private DateTime _lastUpdate;

        public List<Particle> Particles { get; }

        public MainWindow()
        {
            InitializeComponent();
            Particles = [];
            DataContext = this;
            btn_admin.Visibility =
                Main.CurrentUser?.IsAdmin == true ? Visibility.Visible : Visibility.Collapsed;

            if (Main.IsDBAvailable)
            {
                // Load the necessary data from the database
                Main.DB.Variables.Load();
                Main.DB.Purchases.Load();
                Main.DB.OwnedPurchases.Where(v => v.Save == Main.CurrentSave).Load();
            }

            foreach (Currency currency in Currency.Instances)
            {
                CurrencyDisplay display = new() { DataContext = currency };
                wp_stats.Children.Add(display);

                // Creates a tab containing upgrades that are available to purchase with this currency
                CollectionViewSource unlockedCVS =
                    new()
                    {
                        Source = Main.Upgrades,
                        IsLiveFilteringRequested = true,
                        IsLiveSortingRequested = true,
                    };
                unlockedCVS.SortDescriptions.Add(
                    new SortDescription("Cost", ListSortDirection.Ascending)
                );
                unlockedCVS.LiveFilteringProperties.Add(nameof(Upgrade.IsUnlocked));
                unlockedCVS.View.Filter = v =>
                    v is Upgrade u && u.Currency == currency && u.IsUnlocked;

                ItemsControl upgradeContainer =
                    new()
                    {
                        ItemsSource = unlockedCVS.View,
                        ItemTemplate = TryFindResource("UpgradeTemplate") as DataTemplate
                    };

                TabItem tab =
                    new()
                    {
                        Background = currency.Brush,
                        HeaderStringFormat = currency.Icon ?? currency.Name,
                        HeaderTemplate =
                            Application.Current.TryFindResource("TabHeaderWithNotice")
                            as DataTemplate
                    };
                tab.SetBinding(
                    HeaderedContentControl.HeaderProperty,
                    new Binding(nameof(currency.AffordableUpgrades))
                    {
                        Source = currency,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    }
                );
                tab.SetBinding(
                    VisibilityProperty,
                    new Binding(nameof(currency.IsUnlocked))
                    {
                        Source = currency,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                        Converter = new BooleanToVisibilityConverterEx()
                    }
                );
                tab.Content = upgradeContainer;
                tc_upgrades.Items.Add(tab);
            }

            // Creates a tab for purchased upgrades
            CollectionViewSource purchasedCVS =
                new()
                {
                    Source = Main.Upgrades,
                    IsLiveFilteringRequested = true,
                    IsLiveSortingRequested = true,
                };
            purchasedCVS.SortDescriptions.Add(
                new SortDescription("Name", ListSortDirection.Ascending)
            );
            purchasedCVS.LiveFilteringProperties.Add(nameof(Upgrade.IsPurchased));
            purchasedCVS.View.Filter = v => v is Upgrade u && u.IsPurchased;

            MultiBinding headerBinding =
                new()
                {
                    Converter = new StringFormatMultiConverter(),
                    ConverterParameter = " ({0}/{1})",
                };
            headerBinding.Bindings.Add(new Binding("Count") { Source = purchasedCVS.View });
            headerBinding.Bindings.Add(new Binding("Count") { Source = Main.Instance.Upgrades });

            TabItem purchasedTab =
                new()
                {
                    HeaderStringFormat = "Purchased",
                    HeaderTemplate =
                        Application.Current.TryFindResource("TabHeaderWithCount") as DataTemplate,
                };
            purchasedTab.SetBinding(HeaderedContentControl.HeaderProperty, headerBinding);

            ItemsControl purchasedItems =
                new()
                {
                    ItemsSource = purchasedCVS.View,
                    ItemTemplate = TryFindResource("UpgradeTemplate") as DataTemplate
                };
            purchasedTab.Content = purchasedItems;
            tc_upgrades.Items.Add(purchasedTab);

            // Start music playback
            try
            {
                AudioPlaybackEngine.Instance.PlayMusic();
            }
            catch (Exception ex)
            {
                if (ex is not DirectoryNotFoundException) // Ignore when the directory doesn't exist or is empty
                {
                    MessageBoxEx.Show(
                        "An error occured while trying to start music playback.",
                        icon: MessageBoxImage.Error,
                        exception: ex
                    );
                }
            }

            // Timed stuff
            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromSeconds(Main.TickInterval)
            };
            _timer.Tick += Tick;
            _timer.Start();

            CompositionTarget.Rendering += Draw;
        }

        /// <summary>
        /// Updates all particles.
        /// </summary>
        private void Draw(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            TimeSpan deltaTime = now - _lastUpdate;
            _lastUpdate = now;

            Particles.RemoveAll(v => v.Destroyed);
            foreach (var particle in Particles)
            {
                particle.OnUpdate(deltaTime.TotalSeconds);
            }
        }

        private void Tick(object? sender, EventArgs e)
        {
            Main.Tick();
            OnPropertyChanged(
                nameof(CanReincarnate),
                nameof(ShowReincarnateProgress),
                nameof(ReincarnateProgress),
                nameof(PendingSquaresText)
            );
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_timer.IsEnabled || !Main.IsDBAvailable)
            {
                return;
            }

            e.Cancel = true;
            Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    if (Main.IsSaving)
                    {
                        while (Main.IsSaving)
                        {
                            Thread.Sleep(50);
                        }
                    }
                    else
                    {
                        await Main.SaveAsync(true);
                    }

                    if (Main.LastSaveException != null)
                    {
                        throw Main.LastSaveException;
                    }
                    else
                    {
                        _timer.Stop();
                        CompositionTarget.Rendering -= Draw;
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBoxResult result = MessageBoxEx.Show(
                        this,
                        "Something went wrong while saving your data. You may lose progress if you choose to quit now.\nAre you sure you want to quit?",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error,
                        ex
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        _timer.Stop();
                        CompositionTarget.Rendering -= Draw;
                        Close();
                    }
                }
            });
        }

#pragma warning disable IDE1006 // Naming Styles
        private void btn_clicker_Click(object sender, RoutedEventArgs e)
        {
            Point mousePos = Mouse.GetPosition(cnvs);
            double circlesGained = Stat.CirclesPerClick.Value;
            Save.Circles += circlesGained;
            Save.ManualCircles += circlesGained;
            Save.Clicks++;

            if (Main.RNG.NextDouble() < Stat.TriangleChance.Value)
            {
                double trianglesGained = Stat.TrianglesPerClick.Value;
                Save.Triangles += trianglesGained;
                Save.TriangleClicks++;

                TextParticle triangleParticle =
                    new(cnvs, $"+{Currency.Triangles.Format(trianglesGained)}", mousePos);
                triangleParticle.Velocity *= 1.5;
                triangleParticle.MaxLifetime += 0.6;

                triangleParticle.Element.Foreground = Currency.Triangles.Brush;
                triangleParticle.Element.FontSize = 22;
                triangleParticle.Element.FontWeight = FontWeights.Black;
                Panel.SetZIndex(triangleParticle.Element, 1);

                Particles.Add(triangleParticle);
                AudioPlaybackEngine.Instance.PlaySound(Sounds.Collect, variation: 0.1);
            }

            TextParticle particle =
                new(cnvs, $"+{Currency.Circles.Format(circlesGained)}", mousePos);
            Particles.Add(particle);
        }

        private void btn_admin_Click(object sender, RoutedEventArgs e)
        {
            if (IsAdmin)
            {
                AdminWindow adminWindow = new() { Owner = this };
                adminWindow.ShowDialog();
            }
        }

        private void btn_buy_Click(object sender, RoutedEventArgs e)
        {
            if (
                sender is not Button button
                || button.DataContext is not Purchase purchase
                || !purchase.CanAfford
            )
            {
                return;
            }

            purchase.Currency!.Value -= purchase.Cost;
            purchase.Amount++;
        }

        private void btn_removeUpgrade_Click(object sender, RoutedEventArgs e)
        {
            if (
                !IsAdmin
                || sender is not Button button
                || button.DataContext is not Upgrade upgrade
                || !upgrade.IsPurchased
            )
            {
                return;
            }

            //for (int i = 0; i < upgrade.Amount; i++)
            {
                upgrade.Amount--;
                upgrade.Currency!.Value += upgrade.Cost;
            }
        }

        private void btn_reincarnate_Click(object sender, RoutedEventArgs e)
        {
            if (!Currency.Squares.IsPending)
            {
                return;
            }

            MessageBoxResult result = MessageBoxEx.Show(
                this,
                $"By reincarnating, you will lose all of your circles, triangles, buildings, circle upgrades and triangle upgrades.\n"
                    + $"In return, you'll receive {Currency.Squares.Format(Currency.Squares.Pending)}, which you can spend on powerful, permanent upgrades.\n"
                    + $"Are you sure you want to do this?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                Main.IsAutosavingEnabled = null;
                Save.Squares += Currency.Squares.Pending;

                foreach (Building building in Main.Buildings)
                {
                    building.Amount = 0;
                }

                foreach (Upgrade upgrade in Main.Upgrades)
                {
                    if (upgrade.Currency != Currency.Squares)
                    {
                        upgrade.Amount = 0;
                    }
                }

                Save.Circles = 0;
                Save.Triangles = 0;
                Save.Clicks = 0;
                Save.TriangleClicks = 0;
                Save.TotalCircles = 0;
                Save.ManualCircles = 0;
                Save.TotalTriangles = 0;

                Main.IsAutosavingEnabled = true;
            }
        }

        private void cvs_buildings_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = ((Building)e.Item).IsUnlocked;
        }

        private void btn_clicker_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AudioPlaybackEngine.Instance.PlaySound(Sounds.ClickOn, variation: 0.1);
        }

        private void btn_clicker_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AudioPlaybackEngine.Instance.PlaySound(Sounds.ClickOff, variation: 0.1);
        }
#pragma warning restore IDE1006 // Naming Styles

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(params string[] propNames)
        {
            if (Main.Instance.IsAutosavingEnabled == false)
            {
                return;
            }

            foreach (string prop in propNames)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            }
        }
        #endregion
    }
}
