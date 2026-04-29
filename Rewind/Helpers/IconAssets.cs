using System;
using System.Globalization;
using System.IO;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace Rewind.Helpers
{
    public static class IconAssets
    {
        public static string GetAbsolutePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            return Path.Combine(AppContext.BaseDirectory, "Images", fileName);
        }

        public static BitmapImage? LoadBitmap(string fileName)
        {
            string path = GetAbsolutePath(fileName);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
    }

    [MarkupExtensionReturnType(typeof(BitmapImage))]
    public class IconExtension : MarkupExtension
    {
        public string File { get; set; } = string.Empty;

        public IconExtension()
        {
        }

        public IconExtension(string file)
        {
            File = file;
        }

        public override object? ProvideValue(IServiceProvider serviceProvider)
        {
            return IconAssets.LoadBitmap(File);
        }
    }
}
