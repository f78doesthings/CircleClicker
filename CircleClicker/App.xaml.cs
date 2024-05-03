using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using CircleClicker.Models;
using CircleClicker.Models.Database;
using CircleClicker.UI.Windows;
using CircleClicker.Utils.Audio;

namespace CircleClicker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

        private static Main Main_ => CircleClicker.Main.Instance;

        // Making methods async will make progress bars freeze less
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
#if false
            try
            {
                static void a(int i = 0)
                {
                    if (++i >= 10)
                        throw new Exception();
                    a(i);
                }
                a();
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show("MessageBoxEx test", icon: MessageBoxImage.Error, exception: ex);
            }
            return;
#endif
            MessageBoxEx progressBox =
                new()
                {
                    Message = "Connecting...",
                    Progress = -1,
                    ShowLogo = true
                };
            progressBox.Show();

            Mouse.OverrideCursor = Cursors.Wait;
            CultureInfo.DefaultThreadCurrentUICulture = Culture;

            Window? startWindow = null;
            bool shouldConnect = true;
            Exception? connectException = null;
#if DEBUG
            // Check if MySQL is running locally
            try
            {
                Process[] processes = Process.GetProcessesByName("mysqld");
                if (processes.Length == 0)
                {
                    shouldConnect = false;
                    Main_.IsDBAvailable = false;
                }
            }
            catch (Exception ex)
            {
                progressBox.Exception = ex;
            }
#endif
            if (shouldConnect)
            {
                try
                {
                    // Create the database if it does not already exist
                    bool created = await Main_.DB.Database.EnsureCreatedAsync();
                    if (created)
                    {
                        Mouse.OverrideCursor = null;
                        MessageBoxResult result = MessageBoxEx.Show(
                            progressBox,
                            "Circle Clicker has set up a new database for you.\n"
                                + "Would you like to add the default purchases and upgrades to this database?",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );
                        Mouse.OverrideCursor = Cursors.Wait;

                        if (result == MessageBoxResult.Yes)
                        {
                            _ = Stat.Instances;
                            Main_.Buildings = Main_.DB.Buildings.Local.ToObservableCollection();
                            Main_.Upgrades = Main_.DB.Upgrades.Local.ToObservableCollection();
                            Main_.Variables = Main_.DB.Variables.Local.ToObservableCollection();
                            Main_.LoadSampleData();
                        }
                    }

                    Main_.IsDBAvailable = true;
                }
                catch (Exception ex)
                {
                    connectException = ex;
                    Main_.IsDBAvailable = false;
                }
            }
            //Main_.IsDBAvailable = await Main_.DB.Database.CanConnectAsync();

            if (!Main_.IsDBAvailable)
            {
                Mouse.OverrideCursor = null;
                MessageBoxResult result = MessageBoxEx.Show(
                    progressBox,
                    "An error occured while trying to communicate with the MySQL database.\n"
                        + "You may still try out the game, but your progress will not be saved.",
                    icon: MessageBoxImage.Error,
                    exception: connectException
                );
                Mouse.OverrideCursor = Cursors.Wait;

                if (result != MessageBoxResult.OK)
                {
                    progressBox.Close();
                    return;
                }

                // Set up a temporary environment
                Main_.Buildings = [];
                Main_.Upgrades = [];
                Main_.Variables = [];
                Main_.CurrentUser = new User("<Guest>", "") { IsAdmin = true };
                Main_.CurrentSave = new Save(Main_.CurrentUser);
                startWindow = new MainWindow { Title = "Circle Clicker - Offline Mode" };
            }
            else
            {
                Main_.Buildings ??= Main_.DB.Buildings.Local.ToObservableCollection();
                Main_.Upgrades ??= Main_.DB.Upgrades.Local.ToObservableCollection();
                Main_.Variables ??= Main_.DB.Variables.Local.ToObservableCollection();
            }

            // Preload sounds and initialize static fields
            progressBox.Message = "Loading...";
            _ = Stat.Instances;
            _ = ReadOnlyDependency.Clicks;
            _ = Currency.Instances;
            _ = AudioPlaybackEngine.Instance;
            _ = Sounds.Collect;

#if false
            if (Main_.IsDBAvailable)
            {
                Main_.DB.Buildings.Load();
                Main_.DB.Upgrades.Load();
            }
            startWindow ??= new AdminWindow();
#else
            startWindow ??= new LoginWindow();
#endif
            startWindow.Show();
            progressBox.Close();
            Mouse.OverrideCursor = null;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            AudioPlaybackEngine.Instance.Dispose();
            Main_.DB.Dispose();
        }
    }
}
