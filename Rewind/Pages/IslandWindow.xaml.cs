using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rewind
{
    public partial class IslandWindow : Window
    {
        private MainWindow? _playerHost;

        public IslandWindow()
        {
            InitializeComponent();
        }

        public void AttachPlayer(MainWindow playerHost)
        {
            _playerHost = playerHost;
        }

        public void SetPlayPauseIcon(bool isPlaying)
        {
            PlayPauseBtn.Content = isPlaying ? "⏸" : "▶";
        }

        public void UpdateTrackInfo(string trackTitle, string artistName, string? coverPath)
        {
            TrackTitle.Text = trackTitle;
            ArtistName.Text = artistName;
            ApplyCover(coverPath);
        }

        private void ApplyCover(string? coverPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(coverPath))
                {
                    AlbumArt.Source = null;
                    return;
                }

                string fullPath = coverPath.Contains(":")
                    ? coverPath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoversLibrary", coverPath);
                if (!File.Exists(fullPath))
                {
                    AlbumArt.Source = null;
                    return;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(fullPath);
                bitmap.EndInit();
                bitmap.Freeze();
                AlbumArt.Source = bitmap;

                var dominant = GetDominantColor(bitmap);
                var dark = Color.FromArgb(220, (byte)(dominant.R * 0.45), (byte)(dominant.G * 0.45), (byte)(dominant.B * 0.45));
                var light = Color.FromArgb(220, (byte)Math.Min(dominant.R + 35, 255), (byte)Math.Min(dominant.G + 35, 255), (byte)Math.Min(dominant.B + 35, 255));

                RootBorder.Background = new LinearGradientBrush(light, dark, 45);
                AlbumArtContainer.Background = new SolidColorBrush(Color.FromArgb(110, 255, 255, 255));
            }
            catch
            {
                AlbumArt.Source = null;
            }
        }

        private static Color GetDominantColor(BitmapSource bitmap)
        {
            BitmapSource source = bitmap;
            if (source.Format != PixelFormats.Bgra32)
                source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            int stride = source.PixelWidth * 4;
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);

            long r = 0;
            long g = 0;
            long b = 0;
            long count = 0;

            for (int i = 0; i < pixels.Length; i += 40)
            {
                byte blue = pixels[i];
                byte green = pixels[i + 1];
                byte red = pixels[i + 2];
                byte alpha = pixels[i + 3];
                if (alpha < 40) continue;
                r += red;
                g += green;
                b += blue;
                count++;
            }

            if (count == 0) return Color.FromRgb(102, 126, 234);

            return Color.FromRgb((byte)(r / count), (byte)(g / count), (byte)(b / count));
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
            if (key == System.Windows.Input.Key.Q &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) == System.Windows.Input.ModifierKeys.Alt)
            {
                Hide();
                e.Handled = true;
            }
        }

        private void Prev_Click(object sender, RoutedEventArgs e) => _playerHost?.PreviousTrack();

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            _playerHost?.TogglePlayPause();
        }

        private void Next_Click(object sender, RoutedEventArgs e) => _playerHost?.NextTrack();
    }
}