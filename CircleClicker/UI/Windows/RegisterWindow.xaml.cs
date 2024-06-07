using CircleClicker.Models.Database;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Input;

namespace CircleClicker.UI.Windows
{
    /// <summary>
    /// Interaction logic for RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        private static Main Main => Main.Instance;

        public RegisterWindow()
        {
            InitializeComponent();
            tb_error.Visibility = Visibility.Collapsed;
        }

#pragma warning disable IDE1006 // Naming Styles
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            if (tbx_username.Text == "")
            {
                tb_error.Content = "The username must not be empty.";
                tb_error.Visibility = Visibility.Visible;
            }
            else if (pwdbx.Password == "")
            {
                tb_error.Content = "The password must not be empty.";
                tb_error.Visibility = Visibility.Visible;
            }
            else if (pwdbx.Password != pwdbx_confirm.Password)
            {
                tb_error.Content = "The passwords do not match.";
                tb_error.Visibility = Visibility.Visible;
            }
            else
            {
                User? user = User.CreateUser(tbx_username.Text, pwdbx.Password, out string errorMessage);
                if (user == null)
                {
                    tb_error.Content = errorMessage;
                    tb_error.Visibility = Visibility.Visible;
                }
                else
                {
                    Main.DB.Saves.Load();
                    Main.CurrentUser = user;

                    new SavePickerWindow().Show();
                    Close();
                }
            }

            Mouse.OverrideCursor = null;
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }
#pragma warning restore IDE1006 // Naming Styles
    }
}
