using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace XmlGeneratorNew.Converters
{
    public class TreeViewItemLevelToMarginConverter : IValueConverter
    {
        public double Indent { get; set; } = 16;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TreeViewItem item)
            {
                int level = 0;
                DependencyObject current = item;
                while (current != null && !(current is TreeView))
                {
                    if (current is TreeViewItem) level++;
                    current = VisualTreeHelper.GetParent(current);
                }
                return new Thickness(level * Indent, 0, 0, 0);
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}