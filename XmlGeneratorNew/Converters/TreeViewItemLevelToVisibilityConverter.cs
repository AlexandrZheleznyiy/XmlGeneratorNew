using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace XmlGeneratorNew.Converters
{
    public class TreeViewItemLevelToVisibilityConverter : IValueConverter
    {
        public int Level { get; set; } // Позиция уровня, например 1, 2, 3

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value будет TreeViewItem — контрол элемента дерева
            if (value is TreeViewItem tvi)
            {
                int level = 0;
                DependencyObject current = tvi;

                // Подсчёт вложенности относительно TreeView
                while (current != null)
                {
                    if (current is TreeView)
                        break;

                    if (current is TreeViewItem)
                        level++;

                    current = VisualTreeHelper.GetParent(current);
                }

                // Если уровень вложенности >= Level, показываем линию
                return level >= Level ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
