using CircleClicker.Models;
using CircleClicker.Models.Database;
using CircleClicker.UI.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace CircleClicker.UI.Windows
{
    /// <summary>
    /// Interaction logic for AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        public static class Columns
        {
            public static DataGridTextColumn Text(string prop, string? name = null)
            {
                return new DataGridTextColumn()
                {
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                    Header = name ?? prop,
                    Binding = new Binding(prop),
                };
            }

            public static DataGridComboBoxColumn ComboBox(
                string prop,
                IEnumerable<object?> items,
                bool optional = false,
                string? name = null
            )
            {
                return new DataGridComboBoxColumn()
                {
                    DisplayMemberPath = null,
                    Header = name ?? prop,
                    Width = 150,
                    ItemsSource = optional ? ["", .. items] : items,
                    SelectedItemBinding = new Binding(prop)
                };
            }

            public static DataGridCheckBoxColumn CheckBox(string prop, string? name = null)
            {
                return new DataGridCheckBoxColumn()
                {
                    Header = name ?? prop,
                    Binding = new Binding(prop)
                };
            }

            public static DataGridTemplateColumn Number(
                string prop,
                bool integer = false,
                string? name = null
            )
            {
                // Adapted from https://www.codeproject.com/articles/444371/creating-wpf-data-templates-in-code-the-right-way
                string xaml = $$"""
                        <DataTemplate>
                            <controls:{{(integer ? nameof(IntEntryControl) : nameof(DoubleEntryControl))}}
                                Maximum="{{(
                                    integer ? int.MaxValue.ToString(App.Culture) : double.MaxValue.ToString(App.Culture)
                                )}}"
                                Value="{Binding {{prop}}, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
                        </DataTemplate>
                        """;

                ParserContext ctx = new() { XamlTypeMapper = new XamlTypeMapper([]) };
                ctx.XamlTypeMapper.AddMappingProcessingInstruction(
                    "controls",
                    typeof(NumericEntryControl).Namespace,
                    typeof(NumericEntryControl).Assembly.FullName
                );
                ctx.XmlnsDictionary.Add(
                    "",
                    "http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                );
                //ctx.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
                ctx.XmlnsDictionary.Add("controls", "controls");

                DataTemplate? template = XamlReader.Parse(xaml, ctx) as DataTemplate;
                return new DataGridTemplateColumn()
                {
                    Header = name ?? prop,
                    CellTemplate = template
                };
            }
        }

        /// <summary>
        /// Stores the most recently used tab this session.
        /// </summary>
        private static int _lastTab;

#pragma warning disable CA1822 // Mark members as static
        public int CurrentTab
        {
            get => _lastTab;
            set => _lastTab = value;
        }
#pragma warning restore CA1822 // Mark members as static

        public static Main Main => Main.Instance;

        public static ObservableCollection<DataGridColumn> PurchaseColumns =>
            [
                Columns.Text("Name"),
                Columns.ComboBox("Requires", IReadOnlyDependency.Instances, true),
                Columns.Number("BaseRequirement", name: "Base Req."),
                Columns.Number("RequirementScaling", name: "Req. Scaling"),
                Columns.CheckBox("RequirementAdditive", name: "Req. Additive"),
                Columns.ComboBox("Currency", IDependency.Instances),
                Columns.Number("BaseCost", name: "Base Cost"),
                Columns.Number("CostScaling", name: "Cost Mult."),
                Columns.Number("MaxAmount", true, name: "Max"),
            ];

        public static ObservableCollection<DataGridColumn> BuildingColumns =>
            [.. PurchaseColumns, Columns.Number("BaseProduction", name: "Production"),];

        public static ObservableCollection<DataGridColumn> UpgradeColumns =>
            [
                .. PurchaseColumns,
                Columns.ComboBox("Affects", IStat.Instances),
                Columns.Number("BaseEffect", name: "Effect"),
            ];

        public AdminWindow()
        {
            InitializeComponent();
            Main.IsAutosavingEnabled = false;
            DataContext = this;

            dg_variables.ItemsSource = VariableReference.Instances;

            dg_buildings.ItemsSource = Main.Buildings;
            dg_buildings.Columns.Clear();
            foreach (DataGridColumn column in BuildingColumns)
            {
                dg_buildings.Columns.Add(column);
            }

            dg_upgrades.ItemsSource = Main.Upgrades;
            dg_upgrades.Columns.Clear();
            foreach (DataGridColumn column in UpgradeColumns)
            {
                dg_upgrades.Columns.Add(column);
            }

            void AddSaveProperty(string prop, bool integer = false)
            {
                if (Main.CurrentSave == null)
                {
                    return;
                }

                StackPanel panel = new();
                Label label = new() { Content = prop };
                dynamic input = integer
                    ? new IntEntryControl() { Maximum = int.MaxValue }
                    : new DoubleEntryControl() { Maximum = double.MaxValue };
                input.DataContext = Main.CurrentSave;

                input.SetBinding(
                    integer ? IntEntryControl.ValueProperty : DoubleEntryControl.ValueProperty,
                    new Binding(prop)
                    {
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                        Mode = BindingMode.TwoWay
                    }
                );

                panel.Children.Add(label);
                panel.Children.Add(input);
                wp_save.Children.Add(panel);
            }

            AddSaveProperty(nameof(Save.Circles));
            AddSaveProperty(nameof(Save.Triangles));
            AddSaveProperty(nameof(Save.Squares));
            AddSaveProperty(nameof(Save.TotalCircles));
            AddSaveProperty(nameof(Save.ManualCircles));
            AddSaveProperty(nameof(Save.TotalTriangles));
            AddSaveProperty(nameof(Save.LifetimeCircles));
            AddSaveProperty(nameof(Save.LifetimeManualCircles));
            AddSaveProperty(nameof(Save.LifetimeTriangles));
            AddSaveProperty(nameof(Save.LifetimeSquares));
            AddSaveProperty(nameof(Save.Clicks), true);
            AddSaveProperty(nameof(Save.TriangleClicks), true);
            AddSaveProperty(nameof(Save.LifetimeClicks), true);
            AddSaveProperty(nameof(Save.LifetimeTriangleClicks), true);
        }

        private void DataGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            if (e.Source is DataGrid dataGrid)
            {
                dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (Main.IsDBAvailable)
            {
                try
                {
                    Main.DB.SaveChanges();
                }
                catch (Exception ex)
                {
                    e.Cancel = true;
                    MessageBoxEx.Show(
                        this,
                        "An error occured while saving changes.",
                        icon: MessageBoxImage.Error,
                        exception: ex
                    );
                    return;
                }
            }

            Main.IsAutosavingEnabled = true;
        }

#pragma warning disable IDE1006 // Naming Styles
        private void btn_sampleData_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBoxEx.Show(
                this,
                """
                Are you sure you want to load the default buildings and upgrades? This may impact save data.
                Click <b>Yes</b> to update the existing purchases with the default data.
                Click <b>No</b> to first delete any existing purchases. <font weight="500">This will remove all owned purchases from saves.</font>
                """,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question
            );

            if (result is MessageBoxResult.Yes or MessageBoxResult.No)
            {
                try
                {
                    Main.LoadSampleData(result == MessageBoxResult.No);
                }
                catch (Exception ex)
                {
                    MessageBoxEx.Show(
                        this,
                        "An error occured while saving changes.",
                        icon: MessageBoxImage.Error,
                        exception: ex
                    );
                }
            }
        }

        private void btn_test_showMsgBox_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBoxEx.Show(
                this,
                tbx_test_msgBoxMessage.Text,
                (MessageBoxButton)cbx_test_msgBoxButtons.SelectedValue,
                (MessageBoxImage)cbx_test_msgBoxIcon.SelectedValue
            );
            tb_test_msgBoxResult.Text = result.ToString();
        }
#pragma warning restore IDE1006 // Naming Styles
    }
}
