using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
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
        public const string ErrorCategory = "Error";

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
                    connectException = new Exception("The mysqld process is not running.");
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
                        MessageBoxResult result = MessageBoxEx.Show(
                            progressBox,
                            "Circle Clicker has successfully created a database for you.\n"
                                + "Would you like to add the default buildings and upgrades to this database?\n"
                                + "\n"
                                + "If you're unsure, click <Bold>Yes</Bold>.\n"
                                + "<Span FontSize=\"10\">You can always import the default purchases at any time from the Admin Panel.</Span>",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );

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
                MessageBoxResult result = MessageBoxEx.Show(
                    progressBox,
                    "An error occured while trying to communicate with the MySQL database.\n"
                        + "You may still play the game in offline mode, but your progress won't be saved.\n"
                        + "For more info on saving, click <Hyperlink NavigateUri=\"https://github.com/f78doesthings/CircleClicker#saving\">here</Hyperlink>.\n"
                        + "\n"
                        + "Click <Bold>Yes</Bold> to launch the game in offline mode, with the default data.\n"
                        + "Click <Bold>No</Bold> to launch with no default data.",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Error,
                    connectException
                );

                if (result is not MessageBoxResult.Yes and not MessageBoxResult.No)
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

                if (result == MessageBoxResult.Yes)
                {
                    Main_.LoadSampleData();
                }

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

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Error while trying to launch {e.Uri.AbsoluteUri}:\n{ex}",
                    ErrorCategory
                );
            }
        }
    }
}
