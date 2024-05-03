using System.ComponentModel;
using System.Drawing;
using System.Media;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CircleClicker.Utils;

namespace CircleClicker.UI.Windows
{
    /// <summary>
    /// Interaction logic for MessageBoxEx.xaml
    /// </summary>
    public partial class MessageBoxEx : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Contains several properties that return a new <see cref="Button"/> instance.
        /// </summary>
        public static class Buttons
        {
            public static Button OK => new() { Content = "OK", IsDefault = true };

            public static Button Cancel => new() { Content = "Cancel", IsCancel = true };

            public static Button Yes => new() { Content = "Yes" };

            public static Button No => new() { Content = "No" };
        }

        /// <summary>
        /// Contains several system icons.
        /// </summary>
        public static class Icons
        {
            public static readonly BitmapSource Question = FromStockIconId(
                Native.SHSTOCKICONID.SIID_HELP
            );

            public static readonly BitmapSource Information = FromStockIconId(
                Native.SHSTOCKICONID.SIID_INFO
            );

            public static readonly BitmapSource Warning = FromStockIconId(
                Native.SHSTOCKICONID.SIID_WARNING
            );

            public static readonly BitmapSource Error = FromStockIconId(
                Native.SHSTOCKICONID.SIID_ERROR
            );

            public static readonly BitmapSource CopySmall = FromFile("shell32.dll", 134, true);

            // Adapted from https://stackoverflow.com/questions/65590783/system-icons-windows-10-style
            // and https://stackoverflow.com/questions/2572734/how-do-i-use-standard-windows-warning-error-icons-in-my-wpf-app
            /// <summary>
            /// Creates a new <see cref="BitmapSource"/> from a <see cref="Native.SHSTOCKICONID"/>.
            /// </summary>
            private static BitmapSource FromStockIconId(
                Native.SHSTOCKICONID siid,
                Native.SHGSI uFlags = 0
            )
            {
                Native.SHSTOCKICONINFO psii = new();
                int hResult = Native.SHGetStockIconInfo(
                    siid,
                    Native.SHGSI.SHGSI_ICON | uFlags,
                    ref psii
                );

                return hResult < 0
                    ? throw new Exception($"SHGetStockIconInfo returned HRESULT 0x{hResult:x8}.")
                    {
                        HResult = hResult
                    }
                    : psii.ToImageSource();
            }

            /// <summary>
            /// Extracts an icon from a file and creates a <see cref="BitmapSource"/> with it.
            /// </summary>
            private static BitmapSource FromFile(string filePath, int id, bool smallIcon = false)
            {
                Icon icon =
                    System.Drawing.Icon.ExtractIcon(filePath, id, smallIcon)
                    ?? throw new NullReferenceException("ExtractIcon returned null.");

                return icon.ToImageSource();
            }
        }

        /// <summary>
        /// Gets and sets the message of the message box.
        /// </summary>
        public string Message
        {
            get => tb_message.Text;
            set => tb_message.Text = value;
        }

        /// <summary>
        /// Gets and sets the value of the progress bar.<br />
        /// <br />
        /// Set to -1 for an indeterminate progress bar.<br />
        /// Set to -2 to hide the progress bar.
        /// </summary>
        public double Progress
        {
            get => pb_progress.Value;
            set
            {
                pb_progress.Value = value;
                pb_progress.IsIndeterminate = value < 0;
                pb_progress.Visibility = value >= -1 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Gets and sets the visibility of the Circle Clicker logo.
        /// </summary>
        public bool ShowLogo
        {
            get => img_logo.Visibility == Visibility.Visible;
            set => img_logo.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Gets and sets the image source for the message box icon.<br />
        /// Set to <see langword="null"/> to hide the icon.<br />
        /// <br />
        /// Some stock icons are available under <see cref="Icons"/>.
        /// </summary>
        public ImageSource? Image
        {
            get => img_icon.Source;
            set
            {
                img_icon.Source = value;
                img_icon.Visibility = value != null ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private Exception? _exception;

        /// <summary>
        /// Gets and sets the exception to display on the message box.
        /// </summary>
        public Exception? Exception
        {
            get => _exception;
            set
            {
                _exception = value;
                if (value != null)
                {
                    exp_exception.Visibility = Visibility.Visible;
                    run_exceptionType.Text = $"{value.GetType()}:";
                    run_exceptionMessage.Text = value.Message;
                    tb_exceptionStackTrace.Text = value.StackTrace;
                }
                else
                {
                    exp_exception.Visibility = Visibility.Collapsed;
                }
            }
        }

        private bool _areButtonsEnabled;

        public bool AreButtonsEnabled
        {
            get => _areButtonsEnabled;
            set
            {
                _areButtonsEnabled = value;
                OnPropertyChanged(nameof(AreButtonsEnabled));
            }
        }

        /// <summary>
        /// The button that was clicked, or <see langword="null"/> if the message box was closed without clicking a button.
        /// </summary>
        public Button? ClickedButton { get; private set; }

        public MessageBoxEx(IEnumerable<Button>? buttons = null)
        {
            InitializeComponent();
            ShowLogo = false;
            Progress = -2;
            Exception = null;
            AreButtonsEnabled = true;

            img_copy.Source = Icons.CopySmall;
            wp_buttons.Children.Clear();

            if (buttons != null)
            {
                wp_buttons.Visibility = Visibility.Visible;
                foreach (Button button in buttons)
                {
                    if (!button.IsCancel)
                    {
                        button.DataContext = this;
                        button.SetBinding(IsEnabledProperty, new Binding("AreButtonsEnabled"));
                    }

                    wp_buttons.Children.Add(button);
                    button.Click += (_, _) =>
                    {
                        ClickedButton = button;
                        DialogResult = !button.IsCancel;
                    };
                }
            }
            else
            {
                wp_buttons.Visibility = Visibility.Collapsed;
            }
        }

        /// <inheritdoc cref="Show(Window?, string, MessageBoxButton, MessageBoxImage, Exception?)"/>
        public static MessageBoxResult Show(
            string message,
            MessageBoxButton button = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            Exception? exception = null
        )
        {
            return Show(null, message, button, icon, exception);
        }

        /// <summary>
        /// Shows a message box with a message and an optional icon, buttons and exception, and returns the button that was clicked.
        /// </summary>
        public static MessageBoxResult Show(
            Window? owner,
            string message,
            MessageBoxButton button = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            Exception? exception = null
        )
        {
            Dictionary<Button, MessageBoxResult> buttons = [];
            switch (button)
            {
                case MessageBoxButton.OK:
                    buttons.Add(Buttons.OK, MessageBoxResult.OK);
                    break;

                case MessageBoxButton.YesNo:
                    buttons.Add(Buttons.Yes, MessageBoxResult.Yes);
                    buttons.Add(Buttons.No, MessageBoxResult.No);
                    break;

                case MessageBoxButton.OKCancel:
                    buttons.Add(Buttons.OK, MessageBoxResult.OK);
                    buttons.Add(Buttons.Cancel, MessageBoxResult.Cancel);
                    break;

                case MessageBoxButton.YesNoCancel:
                    buttons.Add(Buttons.Yes, MessageBoxResult.Yes);
                    buttons.Add(Buttons.No, MessageBoxResult.No);
                    buttons.Add(Buttons.Cancel, MessageBoxResult.Cancel);
                    break;
            }

            MessageBoxEx messageBox =
                new(buttons.Keys)
                {
                    Message = message,
                    Exception = exception,
                    Owner = owner
                };

            switch (icon)
            {
                case MessageBoxImage.Error:
                    messageBox.Image = Icons.Error;
                    SystemSounds.Hand.Play();
                    break;

                case MessageBoxImage.Warning:
                    messageBox.Image = Icons.Warning;
                    SystemSounds.Exclamation.Play();
                    break;

                case MessageBoxImage.Information:
                    messageBox.Image = Icons.Information;
                    SystemSounds.Asterisk.Play();
                    break;

                case MessageBoxImage.Question:
                    messageBox.Image = Icons.Question;
                    SystemSounds.Question.Play();
                    break;
            }

            messageBox.ShowDialog();

            return messageBox.ClickedButton != null
                ? buttons.GetValueOrDefault(messageBox.ClickedButton)
                : MessageBoxResult.None;
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (Exception != null)
            {
                Clipboard.SetText(Exception.ToString());
            }
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public void OnPropertyChanged(bool _ = false, [CallerMemberName] string propName = "")
        {
            OnPropertyChanged(propName);
        }
        #endregion
    }
}
