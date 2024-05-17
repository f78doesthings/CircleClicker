using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace CircleClicker.UI.Controls
{
    /// <summary>
    /// The base class for number boxes.
    /// </summary>
    public abstract partial class NumericEntryControl : UserControl, INotifyPropertyChanged
    {
        public NumericEntryControl()
        {
            InitializeComponent();
        }

#pragma warning disable IDE1006 // Naming Styles
        protected abstract void tb_GotFocus(object sender, RoutedEventArgs e);
        protected abstract void tb_LostFocus(object sender, RoutedEventArgs e);
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

    /// <summary>
    /// The generic base class for number boxes.
    /// </summary>
    public abstract class NumericEntryControl<T> : NumericEntryControl
        where T : struct, INumber<T>
    {
        #region Dependency properties
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(T),
            typeof(NumericEntryControl<T>),
            new PropertyMetadata(T.Zero)
        );

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            nameof(Minimum),
            typeof(T),
            typeof(NumericEntryControl<T>),
            new PropertyMetadata(T.Zero)
        );

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum),
            typeof(T),
            typeof(NumericEntryControl<T>),
            new PropertyMetadata(T.Parse("100", null))
        );

        private T _previousValue;

        public T Value
        {
            get => (T)GetValue(ValueProperty);
            set
            {
                SetValue(ValueProperty, T.Clamp(value, Minimum, Maximum));
                tb.Text = value.ToString(null, App.Culture);
                NotifyPropertyChanged();
            }
        }

        public T Minimum
        {
            get => (T)GetValue(MinimumProperty);
            set
            {
                SetValue(MinimumProperty, T.Min(value, Maximum));
                NotifyPropertyChanged();
            }
        }

        public T Maximum
        {
            get => (T)GetValue(MaximumProperty);
            set
            {
                SetValue(MaximumProperty, T.Max(value, Minimum));
                NotifyPropertyChanged();
            }
        }
        #endregion

        protected override void tb_GotFocus(object sender, RoutedEventArgs e)
        {
            _previousValue = Value;
        }

        protected override void tb_LostFocus(object sender, RoutedEventArgs e)
        {
            Value = T.TryParse(tb.Text, App.Culture, out T newValue) ? newValue : _previousValue;
        }
    }

    /// <summary>
    /// A number box that works with <see cref="int"/>s.
    /// </summary>
    public class IntEntryControl : NumericEntryControl<int>;

    /// <summary>
    /// A number box that works with <see cref="float"/>s.
    /// </summary>
    public class FloatEntryControl : NumericEntryControl<float>;

    /// <summary>
    /// A number box that works with <see cref="double"/>s.
    /// </summary>
    public class DoubleEntryControl : NumericEntryControl<double>;
}
