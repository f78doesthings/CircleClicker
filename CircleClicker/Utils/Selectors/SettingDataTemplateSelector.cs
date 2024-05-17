using CircleClicker.Models;
using System.Windows;
using System.Windows.Controls;

namespace CircleClicker.Utils.Selectors
{
    public class SettingDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object? item, DependencyObject? container)
        {
            if (container is FrameworkElement element)
            {
                return item switch
                {
                    FloatSetting => element.TryFindResource("FloatSettingTemplate") as DataTemplate,
                    _ => null,
                };
            }
            return null;
        }
    }
}
