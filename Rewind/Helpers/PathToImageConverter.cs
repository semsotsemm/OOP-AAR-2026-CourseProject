using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Rewind.Converters
{
    public class PathToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string fileName = value as string;

            // Если в БД пусто, возвращаем null (покажется нота)
            if (string.IsNullOrEmpty(fileName)) return null;

            try
            {
                // Собираем полный путь к папке с обложками в твоем приложении
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoversLibrary", fileName);

                if (File.Exists(fullPath))
                {
                    return new BitmapImage(new Uri(fullPath));
                }
            }
            catch { }

            return null; // Если файла нет, возвращаем null
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}