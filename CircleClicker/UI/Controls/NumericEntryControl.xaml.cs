using System.Numerics;
using System.Windows;
using System.Windows.Controls;

namespace CircleClicker.UI.Controls
{
    /// <summary>
    /// The base class for number boxes.
    /// </summary>
    public abstract partial class NumericEntryControl : UserControl
    {
        public NumericEntryControl()
        {
            InitializeComponent();
        }

#pragma warning disable IDE1006 // Naming Styles
        protected abstract void tb_GotFocus(object sender, RoutedEventArgs e);
        protected abstract void tb_LostFocus(object sender, RoutedEventArgs e);
#pragma warning restore IDE1006 // Naming Styles
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
            new PropertyMetadata(T.Zero, OnPropertyChanged)
        );

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            nameof(Minimum),
            typeof(T),
            typeof(NumericEntryControl<T>),
            new PropertyMetadata(T.Zero, OnPropertyChanged)
        );

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum),
            typeof(T),
            typeof(NumericEntryControl<T>),
            new PropertyMetadata(T.Parse("100", null), OnPropertyChanged)
        );

        private T _previousValue;

        public T Value
        {
            get => (T)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public T Minimum
        {
            get => (T)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public T Maximum
        {
            get => (T)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }
        #endregion

        protected static void OnPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is not NumericEntryControl<T> numericEntryControl)
            {
                return;
            }

            if (e.Property == ValueProperty)
            {
                T newValue = (T)e.NewValue;
                T clampedValue = T.Clamp(
                    newValue,
                    numericEntryControl.Minimum,
                    numericEntryControl.Maximum
                );
                if (clampedValue != newValue)
                {
                    numericEntryControl.Value = clampedValue;
                }

                numericEntryControl.Update(clampedValue);
            }
            else if (e.Property == MinimumProperty || e.Property == MaximumProperty)
            {
                numericEntryControl.Value = T.Clamp(
                    numericEntryControl.Value,
                    numericEntryControl.Minimum,
                    numericEntryControl.Maximum
                );
            }
        }

        protected override void tb_GotFocus(object sender, RoutedEventArgs e)
        {
            _previousValue = Value;
        }

        protected override void tb_LostFocus(object sender, RoutedEventArgs e)
        {
            Value = T.TryParse(tb.Text, App.Culture, out T newValue) ? newValue : _previousValue;
        }

        protected void Update(T value)
        {
            tb.Text = value.ToString(null, App.Culture);
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
