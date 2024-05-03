using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CircleClicker.UI.Controls
{
    /// <summary>
    /// Interaction logic for NumericEntry.xaml
    /// </summary>
    public partial class IntEntryControl : UserControl, INotifyPropertyChanged
    {
        #region Dependency properties
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(int),
            typeof(IntEntryControl),
            new PropertyMetadata(0)
        );

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
            nameof(MaxValue),
            typeof(int),
            typeof(IntEntryControl),
            new PropertyMetadata(100)
        );

        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
            nameof(MinValue),
            typeof(int),
            typeof(IntEntryControl),
            new PropertyMetadata(0)
        );

        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set
            {
                SetValue(ValueProperty, Math.Clamp(value, MinValue, MaxValue));
                NotifyPropertyChanged();
            }
        }

        // TODO: rename these to Minimum and Maximum
        public int MaxValue
        {
            get => (int)GetValue(MaxValueProperty);
            set
            {
                SetValue(MaxValueProperty, Math.Max(value, MinValue));
                NotifyPropertyChanged();
            }
        }

        public int MinValue
        {
            get => (int)GetValue(MinValueProperty);
            set
            {
                SetValue(MinValueProperty, Math.Min(value, MaxValue));
                NotifyPropertyChanged();
            }
        }
        #endregion

        private int _previousValue;

        public IntEntryControl()
        {
            InitializeComponent();
        }

#pragma warning disable IDE1006 // Naming Styles
        private void tb_GotFocus(object sender, RoutedEventArgs e)
        {
            _previousValue = Value;
        }

        private void tb_LostFocus(object sender, RoutedEventArgs e)
        {
            Value = int.TryParse(tb.Text, App.Culture, out int newValue)
                ? newValue
                : _previousValue;
        }
#pragma warning restore IDE1006 // Naming Styles

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        public void NotifyPropertyChanged(
            IEnumerable<string>? propNames = null,
            [CallerMemberName] string propName = ""
        )
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

            if (propNames != null)
            {
                foreach (string prop in propNames)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
                }
            }
        }
        #endregion
    }
}
