using Rewind.Helpers;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Rewind.MVVM.Converters
{
    /// <summary>
    /// Принимает относительный CoverPath (например, "Images/TrackCovers/foo.jpg")
    /// и возвращает BitmapImage либо null, если файла нет.
    /// Используется в ItemsControl.DataTemplate для обложек.
    /// Параметр — имя legacy-папки ("TrackCovers", "PlaylistCovers", "AlbumCovers").
    /// </summary>
    public sealed class CoverPathToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string path || string.IsNullOrWhiteSpace(path)) return null;
            string legacy = parameter as string ?? "CoversLibrary";

            try
            {
                string fp = FileStorage.ResolveImagePath(path, legacy);
                if (string.IsNullOrEmpty(fp) || !File.Exists(fp)) return null;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(fp);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
