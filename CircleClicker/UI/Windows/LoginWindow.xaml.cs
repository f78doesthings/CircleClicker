﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CircleClicker.Models.Database;
using CircleClicker.Utils;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace CircleClicker.UI.Windows
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private static Main Main => Main.Instance;

        public LoginWindow()
        {
            InitializeComponent();
            tb_error.Visibility = Visibility.Collapsed;

#if DEBUG
            tbx_username.Text = "admin";
            pwdbx.Password = "admin";
#endif
        }

#pragma warning disable IDE1006 // Naming Styles
        private void btn_login_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            User? user = Main.DB.Users.FirstOrDefault(user => user.Name == tbx_username.Text);
            if (user == null)
            {
                tb_error.Content = "Could not find a user with that name.";
                tb_error.Visibility = Visibility.Visible;
            }
            else if (!BC.Verify(pwdbx.Password, user.Password))
            {
                tb_error.Content = "The password is invalid.";
                tb_error.Visibility = Visibility.Visible;
            }
            else if (user.IsBanned)
            {
                string? reason = user.BanReason;
                if (reason is null or "")
                {
                    reason = "No reason given.";
                }

                TextBlock tb = new();
                tb.Inlines.AddRange(
                    Helpers.ParseInlines(
                        $"""
                        This account has been banned.

                        <b>Reason:</b> {reason}
                        <b>Expires:</b> {user.BannedUntil:g} <i>(in {(
                            DateTime.Now - user.BannedUntil!.Value
                        ).PrettyPrint()})</i>
                        """
                    )
                );
                tb_error.Content = tb;
                tb_error.Visibility = Visibility.Visible;
            }
            else
            {
                Main.DB.Saves.Where(v => v.User == user).Load(); // Load the user's saves
                Main.CurrentUser = user;
                new SavePickerWindow().Show();
                Close();
            }

            Mouse.OverrideCursor = null;
        }

        private void btn_register_Click(object sender, RoutedEventArgs e)
        {
            new RegisterWindow().Show();
            Close();
        }
#pragma warning restore IDE1006 // Naming Styles
    }
}
