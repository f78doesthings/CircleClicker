using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CircleClicker.Models;
using CircleClicker.Models.Database;
using CircleClicker.Utils;
using CircleClicker.Utils.Converters;
using Microsoft.EntityFrameworkCore;

namespace CircleClicker.UI.Windows
{
    /// <summary>
    /// Interaction logic for SavePickerWindow.xaml
    /// </summary>
    public partial class SavePickerWindow : Window, INotifyPropertyChanged
    {
        private static Main Main => Main.Instance;
        public ObservableCollection<Save> Saves { get; }

        public bool IsSaveSelected => dg_saves.SelectedItem is Save;
        public bool CanCreateSave => Saves.Count < 3;
        public bool NoSavesExist => Saves.Count == 0;
        public bool SaveExists => Saves.Count > 0;

        public SavePickerWindow()
        {
            if (Main.CurrentUser == null)
            {
                throw new NullReferenceException("Main.CurrentUser is null.");
            }

            InitializeComponent();
            DataContext = this;
            tb_welcome.Text =
                $"Welcome{(Main.CurrentUser.Saves.Count > 0 ? " back" : "")}, {Main.CurrentUser.Name}!";

            Saves = Main.CurrentUser.Saves;
            dg_saves.ItemsSource = Saves;
            Saves.CollectionChanged += (_, _) =>
            {
                NotifyPropertyChanged(
                    nameof(CanCreateSave),
                    nameof(NoSavesExist),
                    nameof(SaveExists)
                );
            };
        }

#pragma warning disable IDE1006 // Naming Styles
        private void btn_new_Click(object sender, RoutedEventArgs e)
        {
            if (Main.CurrentUser == null)
            {
                return;
            }

            if (!CanCreateSave)
            {
                MessageBoxEx.Show(
                    this,
                    "You can only have 3 save files at once.\nPlease delete a save file to be able to make a new one.",
                    icon: MessageBoxImage.Error
                );
                return;
            }

            Save save = new(Main.CurrentUser);
            Saves.Add(save);
            Main.DB.SaveChanges();

            Main.CurrentSave = save;
            new MainWindow().Show();
            Close();
        }

        private void btn_load_Click(object sender, RoutedEventArgs e)
        {
            if (dg_saves.SelectedItem is not Save save)
            {
                if (sender is Button)
                {
                    MessageBoxEx.Show(
                        this,
                        "No save was selected. Please select the save you want to load first.",
                        icon: MessageBoxImage.Error
                    );
                }
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            Main.CurrentSave = save;
            MainWindow mainWindow = new();
            mainWindow.Show();
            Close();

            // Calculate offline production
            DateTime startTime = DateTime.Now;
            TimeSpan offlineTime = startTime - save.LastSaveDate;
            TimeSpan maxOfflineTime = TimeSpan.FromHours(Stat.MaxOfflineTime.Value);
            TimeSpan actualOfflineTime =
                offlineTime > maxOfflineTime ? maxOfflineTime : offlineTime;
            int i = 0;
            bool isCancelled = false;

            MessageBoxEx progressBox =
                new()
                {
                    Message = "Calculating offline progress...",
                    Progress = 0,
                    Owner = mainWindow,
                };

            void Draw(object? sender, EventArgs e)
            {
                Main.IsAutosavingEnabled = null; // Allow MainWindow to update once every frame
                progressBox.Progress = (double)i / Main.MaxOfflineTicks * 100;
            }

            CompositionTarget.Rendering += Draw;
            Task.Run(() =>
            {
                double circlesProduced = 0;

                for (; i < Main.MaxOfflineTicks; i++)
                {
                    Main.IsAutosavingEnabled = false; // Temporary disable some methods, speeds up this loop a bit

                    if (isCancelled)
                    {
                        TimeSpan finalDeltaTime =
                            actualOfflineTime * (1 - (i / Main.MaxOfflineTicks));
                        Main.TickOffline(
                            finalDeltaTime.TotalSeconds,
                            out double circlesProducedFinalTick
                        );
                        circlesProduced += circlesProducedFinalTick;
                        break;
                    }

                    TimeSpan deltaTime = actualOfflineTime / Main.MaxOfflineTicks;
                    Main.TickOffline(deltaTime.TotalSeconds, out double circlesProducedThisTick);
                    circlesProduced += circlesProducedThisTick;
                }

                Main.IsAutosavingEnabled = true;
                Dispatcher.Invoke(() =>
                {
                    CompositionTarget.Rendering -= Draw;
                    Mouse.OverrideCursor = null;
                    progressBox.Close();

                    string message =
                        $"Welcome back!\n"
                        + $"While you were gone, your buildings produced ⚫ {circlesProduced.FormatSuffixes()}.\n"
                        + $"You were offline for {offlineTime.ToString(@"d'd 'h'h 'm'm 's's'", App.Culture)} / {maxOfflineTime.ToString(@"d'd 'h'h 'm'm 's's'", App.Culture)}.";

                    if (circlesProduced == 0)
                    {
                        message += "\n\n(Perhaps you should get some buildings...)";
                    }
                    message += $"\n\n(Calculating took {DateTime.Now - startTime})";
                    MessageBoxEx.Show(mainWindow, message, icon: MessageBoxImage.Information);
                });
            });

            progressBox.ShowDialog();
            isCancelled = true;
        }

        private void btn_delete_Click(object sender, RoutedEventArgs e)
        {
            if (dg_saves.SelectedItem is not Save save)
            {
                MessageBoxEx.Show(
                    this,
                    "No save was selected. Please select the save you want to delete first.",
                    icon: MessageBoxImage.Error
                );
                return;
            }

            var result = MessageBoxEx.Show(
                this,
                "Are you sure you want to delete the selected save? There is no way back!",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                Saves.Remove(save);
                Main.DB.SaveChanges();
            }
        }

        private void dg_saves_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(IsSaveSelected));
        }
#pragma warning restore IDE1006 // Naming Styles

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        public void NotifyPropertyChanged(params string[] propNames)
        {
            foreach (string prop in propNames)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            }
        }
        #endregion
    }
}
