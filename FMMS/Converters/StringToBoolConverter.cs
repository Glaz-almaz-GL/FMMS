using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FMMS.Converters
{
    public class StringToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                // Возвращает true, если строка не null и не пустая (или не состоит только из пробелов)
                // Это позволяет скрывать элемент при привязке IsVisible к пустой строке ошибки.
                return !string.IsNullOrWhiteSpace(str);
            }
            // Если значение не строка, можно вернуть false или null
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // ConvertBack обычно не используется для IsVisible, но его нужно реализовать
            throw new NotImplementedException();
        }
    }
}
