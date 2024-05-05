using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CircleClicker.Models;

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
                    Setting<float>
                        => element.TryFindResource("FloatSettingTemplate") as DataTemplate,
                    _ => null,
                };
            }
            return null;
        }
    }
}
