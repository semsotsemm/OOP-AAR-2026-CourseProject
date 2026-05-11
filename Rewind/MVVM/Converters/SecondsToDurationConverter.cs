using System.Globalization;
using System.Windows.Data;

namespace Rewind.MVVM.Converters
{
    /// <summary>int секунды → строка "M:SS".</summary>
    public sealed class SecondsToDurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int s && s > 0) return $"{s / 60}:{s % 60:D2}";
            return "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
