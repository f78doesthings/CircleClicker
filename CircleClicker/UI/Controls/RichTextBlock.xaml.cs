using CircleClicker.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace CircleClicker.UI.Controls
{
    /// <summary>
    /// Displays rich text parsed by <see cref="Helpers.ParseInlines(string)" />.
    /// </summary>
    public partial class RichTextBlock : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(RichTextBlock), new PropertyMetadata(OnTextPropertyChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public RichTextBlock()
        {
            InitializeComponent();
        }

        private static void OnTextPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is RichTextBlock richTextBlock)
            {
                IEnumerable<Inline> inlines = Helpers.ParseInlines((string)e.NewValue);
                richTextBlock.tb.Inlines.Clear();
                richTextBlock.tb.Inlines.AddRange(inlines);
            }
        }
    }
}
