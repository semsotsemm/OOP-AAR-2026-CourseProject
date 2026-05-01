using Rewind.Helpers;
using Rewind.Contols;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rewind.Pages
{
    public partial class NowPlaying : Window
    {
        private readonly MainWindow _host;
        private string _sourcePage;
        private bool _sliderDragging;
        private bool _shuffleEnabled;
        private bool _repeatEnabled;

        public NowPlaying(MainWindow host, string sourcePage)
        {
            InitializeComponent();
            _host = host;
            _sourcePage = sourcePage;
            BackText.Text = $"Назад: {_sourcePage}";

            _host.PlaybackStateChanged += Host_PlaybackStateChanged;
            Closed += (_, _) => _host.PlaybackStateChanged -= Host_PlaybackStateChanged;

            VolumeSlider.Value = _host.Volume;
            RefreshFromHost();
        }

        public void UpdateSourcePage(string sourcePage)
        {
            _sourcePage = sourcePage;
            BackText.Text = $"Назад: {_sourcePage}";
        }

        private void Host_PlaybackStateChanged()
        {
            Dispatcher.Invoke(RefreshFromHost);
        }

        private void RefreshFromHost()
        {
            var track = _host.CurrentTrack;
            if (track == null) return;

            TrackTitleText.Text = track.TrackName;
            TrackArtistText.Text = track.ArtistName;
            NowPlayingPausePlayIcon.Source = IconAssets.LoadBitmap(_host.IsPlaying ? "player_pause.png" : "player_play.png");
            CurrentTimeText.Text = FormatTime(_host.CurrentSeconds);
            TotalTimeText.Text = FormatTime(_host.TotalSeconds);

            if (!_sliderDragging)
            {
                ProgressSlider.Maximum = Math.Max(_host.TotalSeconds, 1);
                ProgressSlider.Value = Math.Clamp(_host.CurrentSeconds, 0, ProgressSlider.Maximum);
            }

            LikeIcon.Source = IconAssets.LoadBitmap(Session.IsLiked(track.TrackId) ? "like_filled.png" : "like_outline.png");
            ApplyCover(track.CoverPath);
            RenderQueue();
        }

        private void RenderQueue()
        {
            QueuePanel.Children.Clear();
            var context = _host.CurrentContext.ToList();
            int currentId = _host.CurrentTrack?.TrackId ?? -1;

            for (int i = 0; i < context.Count; i++)
            {
                var item = context[i];
                bool isCurrent = item.TrackId == currentId;

                var row = new Border
                {
                    Background = isCurrent
                        ? new SolidColorBrush(Color.FromArgb(95, 42, 232, 118))
                        : new SolidColorBrush(Color.FromArgb(70, 20, 20, 20)),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 0, 0, 6),
                    Cursor = Cursors.Hand
                };

                var rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                // Track info (left)
                var meta = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                var titleBlock = new TextBlock
                {
                    Text = item.TrackName,
                    Foreground = isCurrent ? new SolidColorBrush(Color.FromRgb(42, 232, 118)) : Brushes.White,
                    FontWeight = isCurrent ? FontWeights.Bold : FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                meta.Children.Add(titleBlock);
                meta.Children.Add(new TextBlock
                {
                    Text = item.ArtistName,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 172)),
                    FontSize = 11
                });
                Grid.SetColumn(meta, 0);

                // Actions (right)
                var btnPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                };

                if (!isCurrent)
                {
                    // Remove button
                    var removeBtn = new Border
                    {
                        Width = 26, Height = 26, CornerRadius = new CornerRadius(13),
                        Background = new SolidColorBrush(Color.FromArgb(90, 220, 60, 60)),
                        Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 4, 0),
                        ToolTip = "Удалить из очереди"
                    };
                    removeBtn.Child = new TextBlock
                    {
                        Text = "×", FontSize = 15, FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    int capturedId = item.TrackId;
                    removeBtn.MouseLeftButtonDown += (_, ev) =>
                    {
                        ev.Handled = true;
                        _host.RemoveFromQueue(capturedId);
                    };
                    btnPanel.Children.Add(removeBtn);

                    // Right-click context menu on the row
                    var ctxMenu = new ContextMenu();
                    var removeMenuItem = new MenuItem { Header = "×  Удалить из очереди" };
                    int rid = item.TrackId;
                    removeMenuItem.Click += (_, __) => _host.RemoveFromQueue(rid);
                    ctxMenu.Items.Add(removeMenuItem);

                    var playNextMenuItem = new MenuItem { Header = "⇥  Играть следующим" };
                    var capturedItem2 = item;
                    playNextMenuItem.Click += (_, __) => _host.PlayNext(capturedItem2);
                    ctxMenu.Items.Add(playNextMenuItem);

                    row.ContextMenu = ctxMenu;
                }

                Grid.SetColumn(btnPanel, 1);

                rowGrid.Children.Add(meta);
                rowGrid.Children.Add(btnPanel);
                row.Child = rowGrid;

                var capturedItem = item;
                row.MouseLeftButtonDown += (_, _) =>
                {
                    if (!isCurrent)
                        _host.PlayTrackFromContext(capturedItem, _host.CurrentContext.ToList());
                };

                QueuePanel.Children.Add(row);
            }

            if (context.Count == 0)
            {
                QueuePanel.Children.Add(new TextBlock
                {
                    Text = "Очередь пуста",
                    Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 120)),
                    FontSize = 13, HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
            }
        }

        private void ApplyCover(string? coverPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(coverPath))
                {
                    CoverImage.Source = null;
                    CoverPlaceholder.Visibility = Visibility.Visible;
                    return;
                }

                string fullPath = coverPath.Contains(":")
                    ? coverPath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoversLibrary", coverPath);
                if (!File.Exists(fullPath))
                {
                    CoverImage.Source = null;
                    CoverPlaceholder.Visibility = Visibility.Visible;
                    return;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(fullPath);
                bitmap.EndInit();
                bitmap.Freeze();
                CoverImage.Source = bitmap;
                CoverPlaceholder.Visibility = Visibility.Collapsed;

                var dominant = GetDominantColor(bitmap);
                var dark = Color.FromRgb((byte)(dominant.R * 0.45), (byte)(dominant.G * 0.45), (byte)(dominant.B * 0.45));
                var light = Color.FromRgb((byte)Math.Min(dominant.R + 40, 255), (byte)Math.Min(dominant.G + 40, 255), (byte)Math.Min(dominant.B + 40, 255));
                BackgroundBrush.GradientStops[0].Color = light;
                BackgroundBrush.GradientStops[1].Color = dark;
            }
            catch
            {
                CoverImage.Source = null;
                CoverPlaceholder.Visibility = Visibility.Visible;
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
                if (alpha < 35) continue;
                r += red;
                g += green;
                b += blue;
                count++;
            }
            if (count == 0) return Color.FromRgb(45, 45, 45);
            return Color.FromRgb((byte)(r / count), (byte)(g / count), (byte)(b / count));
        }

        private static string FormatTime(double sec)
        {
            if (sec <= 0) return "0:00";
            var ts = TimeSpan.FromSeconds(sec);
            return ts.Hours > 0 ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}" : $"{ts.Minutes}:{ts.Seconds:D2}";
        }

        private void ArtistName_Click(object sender, MouseButtonEventArgs e)
        {
            var artistName = TrackArtistText.Text;
            if (string.IsNullOrWhiteSpace(artistName)) return;
            Close();
            _host.OpenArtistProfileByName(artistName);
            _host.Activate();
        }

        private void BackButton_Click(object sender, MouseButtonEventArgs e)
        {
            Close();
            _host.Activate();
        }

        private void PrevBtn_Click(object sender, RoutedEventArgs e) => _host.PreviousTrack();
        private void NextBtn_Click(object sender, RoutedEventArgs e) => _host.NextTrack();
        private void PlayPauseBtn_Click(object sender, RoutedEventArgs e) => _host.TogglePlayPause();

        private void ShuffleBtn_Click(object sender, RoutedEventArgs e)
        {
            _shuffleEnabled = !_shuffleEnabled;
            ShuffleBtn.Background = new SolidColorBrush(_shuffleEnabled ? Color.FromRgb(42, 232, 118) : Color.FromRgb(42, 42, 40));
            ShuffleBtn.Foreground = Brushes.White;
        }

        private void RepeatBtn_Click(object sender, RoutedEventArgs e)
        {
            _repeatEnabled = !_repeatEnabled;
            RepeatBtn.Background = new SolidColorBrush(_repeatEnabled ? Color.FromRgb(42, 232, 118) : Color.FromRgb(42, 42, 40));
            RepeatBtn.Foreground = Brushes.White;
        }

        private void LikeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            var track = _host.CurrentTrack;
            if (track == null) return;
            Session.ToggleLike(track.TrackId);
            RefreshFromHost();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            _host.Volume = VolumeSlider.Value;
        }

        private void ProgressSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e) => _sliderDragging = true;

        private void ProgressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _sliderDragging = false;
            _host.SeekTo(ProgressSlider.Value);
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_sliderDragging)
                CurrentTimeText.Text = FormatTime(ProgressSlider.Value);
        }
    }
}
