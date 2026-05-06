using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Rewind.MVVM.Converters
{
    /// <summary>null / пустая строка / 0 → Collapsed, остальное → Visible.
    /// Параметр "invert" инвертирует.</summary>
    public sealed class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool has = value switch
            {
                null => false,
                string s => !string.IsNullOrWhiteSpace(s),
                int i => i != 0,
                System.Collections.ICollection c => c.Count > 0,
                _ => true
            };

            if (parameter is string p && p.Equals("invert", StringComparison.OrdinalIgnoreCase))
                has = !has;

            return has ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
