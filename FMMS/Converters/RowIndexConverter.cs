using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FMMS.Converters
{
    public class RowIndexConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // value должен быть DataGridRow
            if (value is DataGridRow row)
            {
                // Получаем индекс строки в ItemsSource
                int index = row.Index;

                // Возвращаем индекс + 1 для нумерации с 1
                return (index + 1).ToString();
            }
            return "0"; // Возвращаем "0", если не удалось получить индекс
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
