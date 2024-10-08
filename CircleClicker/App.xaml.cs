using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
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

        public static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

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
                    Message = "Loading...",
                    Progress = -1,
                    ShowLogo = true,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
            progressBox.Show();

            Mouse.OverrideCursor = Cursors.Wait;

            // Preload sounds and initialise static fields
            foreach (Stat instance in Stat.Instances)
            {
                instance.Register();
            }
            _ = ReadOnlyDependency.Clicks;
            _ = Currency.Instances;
            _ = AudioPlaybackEngine.Instance;
            _ = Sounds.Collect;

            Window? startWindow = null;
            bool shouldConnect = true;
            Exception? connectException = null;
            Main? savedData = null;

            // TODO: consider finding a better way to do flags
            bool onlineMode = e.Args.Contains("--online");
            bool testMode = e.Args.Contains("--test");

            if (onlineMode)
            {
                progressBox.Message = "Connecting...";
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
                        await CreateDatabase();
                    }
                    catch (Exception ex)
                    {
                        connectException = ex;
                        Main_.IsDBAvailable = false;
                    }
                }
                //Main_.IsDBAvailable = await Main_.DB.Database.CanConnectAsync();
            }

            if (!Main_.IsDBAvailable)
            {
                MessageBoxResult result = MessageBoxResult.Yes;
                if (onlineMode)
                {
                    result = MessageBoxEx.Show(
                        progressBox,
                        """
                        An error occured while trying to communicate with the MySQL database.
                        You may still play the game in offline mode, but your progress won't be saved.
                        For more info on saving, click <a href="https://github.com/f78doesthings/CircleClicker#saving">here</a>.
                                
                        Click <b>Yes</b> to launch the game in offline mode, with the default data.
                        Click <b>No</b> to launch with no default data.
                        """,
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Error,
                        connectException
                    );

                    if (result is not MessageBoxResult.Yes and not MessageBoxResult.No)
                    {
                        progressBox.Close();
                        return;
                    }
                }

                // Set up a temporary environment
                try
                {
                    using FileStream stream = File.OpenRead("save.json");
                    savedData = await JsonSerializer.DeserializeAsync<Main>(
                        stream,
                        CircleClicker.Main.SerializerOptions
                    );
                }
                catch (FileNotFoundException)
                {
                    // Save data doesn't exist, so ignore
                }
                catch (Exception ex)
                {
                    MessageBoxResult result2 = MessageBoxEx.Show(
                        progressBox,
                        """
                        An error occured while loading the offline mode save data. Circle Clicker will now close.
                        """,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        ex
                    );
                    progressBox.Close();
                    return;
                }

                Main_.Buildings = savedData?.Buildings ?? [];
                Main_.Upgrades = savedData?.Upgrades ?? [];
                Main_.Variables = savedData?.Variables ?? [];
                Main_.CurrentUser = savedData?.CurrentUser ?? new User();
                Main_.CurrentSave = savedData?.CurrentSave ?? new Save(Main_.CurrentUser);
                Main_.CurrentUser.IsAdmin = testMode;

                if ((onlineMode && result == MessageBoxResult.Yes) || savedData == null)
                {
                    Main_.LoadSampleData(true, true);
                }

                startWindow = new MainWindow();
            }
            else
            {
                Main_.Buildings ??= Main_.DB.Buildings.Local.ToObservableCollection();
                Main_.Upgrades ??= Main_.DB.Upgrades.Local.ToObservableCollection();
                Main_.Variables ??= Main_.DB.Variables.Local.ToObservableCollection();
            }

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

            if (savedData != null)
            {
                Main_.CalculateOffline(startWindow, Dispatcher);
            }
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

        private void Button_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = e.IsRepeat;
        }

        private static async Task CreateDatabase(bool deleteFirst = false)
        {
            if (deleteFirst)
            {
                await Main_.DB.Database.EnsureDeletedAsync();
            }

            bool created = await Main_.DB.Database.EnsureCreatedAsync();
            Main_.IsDBAvailable = true;

            if (created)
            {
                MessageBoxResult result = MessageBoxEx.Show(
                    """
                    Circle Clicker has successfully created a database for you.
                    Would you like to add the default buildings and upgrades to this database?

                    If you're unsure, click <b>Yes</b>.
                    <i size="12">You can always import the default purchases at any time from the Admin Panel.</i>
                    """,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    Main_.Buildings = Main_.DB.Buildings.Local.ToObservableCollection();
                    Main_.Upgrades = Main_.DB.Upgrades.Local.ToObservableCollection();
                    Main_.Variables = Main_.DB.Variables.Local.ToObservableCollection();
                    Main_.LoadSampleData();
                    await Main_.DB.SaveChangesAsync();
                }
            }
        }

        private void Application_DispatcherUnhandledException(
            object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e
        )
        {
            e.Handled = true;
            Mouse.OverrideCursor = null;

            MessageBoxResult result = MessageBoxEx.Show(
                """
                Circle Clicker ran into an unhandled exception. <i>That's not good...</i>
                <i size="12">If the error is database related, you may need to delete the database and let Circle Clicker recreate it. Click Yes to do so.</i>
                """,
                MessageBoxButton.YesNo,
                MessageBoxImage.Error,
                e.Exception
            );

            switch (result)
            {
                case MessageBoxResult.Yes:
                    result = MessageBoxEx.Show(
                        "Are you sure you want to <b color=\"res:AccentBrush\">delete</b> the database? <b>There is no way back!</b>",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );
                    if (result == MessageBoxResult.Yes)
                    {
                        _ = CreateDatabase(true);
                    }
                    break;
                case MessageBoxResult.No:
                    break;
                default:
                    Shutdown();
                    break;
            }
        }
    }
}
