using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CircleClicker.Models;
using CircleClicker.Models.Database;
using CircleClicker.Utils;

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
            Main.CalculateOffline(mainWindow, Dispatcher);
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
