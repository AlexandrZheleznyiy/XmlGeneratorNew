using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace XmlGeneratorNew.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value - это значение свойства Type (PropertyType enum)
            // parameter - строка, переданная как ConverterParameter

            string paramStr = parameter as string;
            if (string.IsNullOrEmpty(paramStr) || value == null)
            {
                return Visibility.Collapsed; // Или Visibility.Visible, в зависимости от логики по умолчанию
            }

            string valueStr = value.ToString();

            // Проверяем, начинается ли параметр с "!"
            bool negate = paramStr.StartsWith("!");
            string targetValue = negate ? paramStr.Substring(1) : paramStr;

            bool isVisible = string.Equals(valueStr, targetValue, StringComparison.OrdinalIgnoreCase);

            // Применяем отрицание, если нужно
            if (negate)
            {
                isVisible = !isVisible;
            }

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack обычно не требуется для Visibility Converters
            throw new NotImplementedException();
        }
    }
}