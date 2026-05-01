using Rewind.Helpers;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Win32;
using System.Linq;

namespace Rewind.Tabs.UsersTabs
{
    public partial class ArtistStudioPage : UserControl
    {
        private string? _selectedCoverPath;
        private string? _selectedAudioPath;
        private string? _selectedAlbumCoverPath;
        private int? _editingAlbumId;

        public ArtistStudioPage()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadStats();
        }

        // ─────────────────────────────────────────────
        //  Tab switching
        // ─────────────────────────────────────────────

        private void TabUpload_Click(object sender, RoutedEventArgs e)
        {
            TabUpload.Style    = (Style)FindResource("ProfileTabActive");
            TabMyTracks.Style  = (Style)FindResource("ProfileTab");
            TabAlbums.Style    = (Style)FindResource("ProfileTab");
            PanelUpload.Visibility   = Visibility.Visible;
            PanelMyTracks.Visibility = Visibility.Collapsed;
            PanelAlbums.Visibility   = Visibility.Collapsed;
        }

        private void TabMyTracks_Click(object sender, RoutedEventArgs e)
        {
            TabUpload.Style    = (Style)FindResource("ProfileTab");
            TabMyTracks.Style  = (Style)FindResource("ProfileTabActive");
            TabAlbums.Style    = (Style)FindResource("ProfileTab");
            PanelUpload.Visibility   = Visibility.Collapsed;
            PanelMyTracks.Visibility = Visibility.Visible;
            PanelAlbums.Visibility   = Visibility.Collapsed;
            LoadMyTracks();
        }

        private void TabAlbums_Click(object sender, RoutedEventArgs e)
        {
            TabUpload.Style    = (Style)FindResource("ProfileTab");
            TabMyTracks.Style  = (Style)FindResource("ProfileTab");
            TabAlbums.Style    = (Style)FindResource("ProfileTabActive");
            PanelUpload.Visibility   = Visibility.Collapsed;
            PanelMyTracks.Visibility = Visibility.Collapsed;
            PanelAlbums.Visibility   = Visibility.Visible;
            LoadAlbumBuilder();
            LoadArtistAlbums();
        }

        private void RefreshTracks_Click(object sender, MouseButtonEventArgs e) => LoadMyTracks();

        private void LoadAlbumBuilder()
        {
            AlbumTrackChecks.Children.Clear();
            List<Track> tracks;
            try { tracks = TrackService.GetByArtistAll(Session.UserId).Where(t => t.PublishStatus == "Published").ToList(); }
            catch { tracks = new List<Track>(); }

            if (tracks.Count == 0)
            {
                AlbumTrackChecks.Children.Add(new TextBlock
                {
                    Text = "Опубликованных треков пока нет",
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 128)),
                    FontSize = 12
                });
                return;
            }

            foreach (var track in tracks)
            {
                var cb = new CheckBox
                {
                    Content = $"🎵 {track.Title}",
                    Tag = track.TrackID,
                    Margin = new Thickness(0, 0, 10, 8),
                    Padding = new Thickness(8, 4, 8, 4),
                    FontSize = 12,
                    Foreground = (Brush)Application.Current.Resources["TextPrimary"]
                };
                AlbumTrackChecks.Children.Add(cb);
            }
        }

        private void CreateAlbum_Click(object sender, RoutedEventArgs e)
        {
            string title = AlbumTitleBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Введите название альбома.", "Rewind");
                return;
            }

            var selectedTrackIds = AlbumTrackChecks.Children
                .OfType<CheckBox>()
                .Where(cb => cb.IsChecked == true && cb.Tag is int)
                .Select(cb => (int)cb.Tag)
                .ToList();

            if (selectedTrackIds.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один трек для альбома.", "Rewind");
                return;
            }

            string? albumCover = null;
            if (!string.IsNullOrWhiteSpace(_selectedAlbumCoverPath))
                albumCover = FileStorage.CopyAlbumCover(_selectedAlbumCoverPath);

            string genre = (AlbumGenreSelector.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Other";
            bool wasEditing = _editingAlbumId.HasValue;
            int albumId;
            if (_editingAlbumId.HasValue)
            {
                albumId = _editingAlbumId.Value;
                AlbumService.Update(albumId, title, genre, albumCover);
            }
            else
            {
                albumId = AlbumService.Create(title, Session.UserId, genre, albumCover);
            }
            foreach (int trackId in selectedTrackIds)
                AlbumService.AddTrack(albumId, trackId);

            ResetAlbumForm();
            LoadArtistAlbums();
            MessageBox.Show(wasEditing ? "Альбом обновлён." : "Альбом создан.", "Rewind");
        }

        private void PickAlbumCover_Click(object sender, MouseButtonEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.webp" };
            if (dlg.ShowDialog() == true)
            {
                _selectedAlbumCoverPath = dlg.FileName;
                AlbumCoverPreview.Source = new BitmapImage(new Uri(_selectedAlbumCoverPath));
                AlbumCoverPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadArtistAlbums()
        {
            ArtistAlbumsContainer.Children.Clear();
            List<Album> albums;
            try { albums = AlbumService.GetByArtist(Session.UserId); }
            catch { albums = new List<Album>(); }

            if (albums.Count == 0)
            {
                ArtistAlbumsContainer.Children.Add(MakeEmptyCard("Альбомов пока нет"));
                return;
            }

            foreach (var album in albums)
                ArtistAlbumsContainer.Children.Add(MakeAlbumCard(album));
        }

        private UIElement MakeAlbumCard(Album album)
        {
            var card = new Border
            {
                Width = 160,
                Height = 190,
                CornerRadius = new CornerRadius(16),
                Background = (Brush)Application.Current.Resources["BgSidebar"],
                BorderBrush = (Brush)Application.Current.Resources["BorderColor"],
                BorderThickness = new Thickness(1.5),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 12, 12)
            };
            card.MouseLeftButtonDown += (_, _) => SelectAlbumForEdit(album);
            var stack = new StackPanel();
            stack.Children.Add(new Border
            {
                Height = 100,
                CornerRadius = new CornerRadius(12),
                Background = new LinearGradientBrush(Color.FromRgb(42, 232, 118), Color.FromRgb(0, 77, 64), new Point(0, 0), new Point(1, 1)),
                Child = new TextBlock { Text = "💿", FontSize = 32, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            });
            stack.Children.Add(new TextBlock { Text = album.Title, FontWeight = FontWeights.Bold, FontSize = 13, Margin = new Thickness(0, 10, 0, 2), TextTrimming = TextTrimming.CharacterEllipsis, Foreground = (Brush)Application.Current.Resources["TextPrimary"] });
            stack.Children.Add(new TextBlock { Text = $"{album.AlbumTracks?.Count ?? 0} треков • {album.Genre}", FontSize = 11, Foreground = (Brush)Application.Current.Resources["TextSecondary"] });
            card.Child = stack;
            return card;
        }

        private void SelectAlbumForEdit(Album album)
        {
            _editingAlbumId = album.AlbumId;
            AlbumTitleBox.Text = album.Title;
            CreateAlbumBtn.Content = "💾  Сохранить изменения";

            foreach (ComboBoxItem item in AlbumGenreSelector.Items)
                item.IsSelected = string.Equals(item.Content?.ToString(), album.Genre, StringComparison.OrdinalIgnoreCase);

            foreach (var cb in AlbumTrackChecks.Children.OfType<CheckBox>())
                cb.IsChecked = album.AlbumTracks?.Any(at => at.TrackId == (int)cb.Tag) == true;

            if (!string.IsNullOrWhiteSpace(album.CoverPath))
            {
                try
                {
                    string fp = album.CoverPath.Contains(":") ? album.CoverPath : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoversLibrary", album.CoverPath);
                    if (File.Exists(fp))
                    {
                        AlbumCoverPreview.Source = new BitmapImage(new Uri(fp));
                        AlbumCoverPlaceholder.Visibility = Visibility.Collapsed;
                    }
                }
                catch { }
            }
        }

        private void ResetAlbumForm()
        {
            _editingAlbumId = null;
            AlbumTitleBox.Clear();
            AlbumCoverPreview.Source = null;
            AlbumCoverPlaceholder.Visibility = Visibility.Visible;
            _selectedAlbumCoverPath = null;
            CreateAlbumBtn.Content = "💿  Создать альбом";
            foreach (var cb in AlbumTrackChecks.Children.OfType<CheckBox>()) cb.IsChecked = false;
        }

        // ─────────────────────────────────────────────
        //  Stats
        // ─────────────────────────────────────────────

        private void LoadStats()
        {
            try
            {
                var tracks     = TrackService.GetByArtistAll(Session.UserId);
                int totalPlays = tracks.Sum(t => t.Statistics?.PlayCount  ?? 0);
                int totalLikes = tracks.Sum(t => t.Statistics?.LikesCount ?? 0);
                StatsTrackCount.Text  = tracks.Count.ToString();
                StatsTotalPlays.Text  = totalPlays.ToString();
                StatsTotalLikes.Text  = totalLikes.ToString();
                try
                {
                    int subs = SubscriptionService.GetFollowers(Session.UserId).Count;
                    StatsSubscriberCount.Text = subs.ToString();
                }
                catch { StatsSubscriberCount.Text = "0"; }
            }
            catch { }
            DrawSubscriberChart();
        }

        // ─────────────────────────────────────────────
        //  My Tracks list
        // ─────────────────────────────────────────────

        private void LoadMyTracks()
        {
            MyTracksContainer.Children.Clear();
            LoadStats();

            List<Track> tracks;
            try   { tracks = TrackService.GetByArtistAll(Session.UserId); }
            catch { tracks = new List<Track>(); }

            MyTracksHeader.Text = $"Мои треки ({tracks.Count})";

            if (tracks.Count == 0)
            {
                MyTracksContainer.Children.Add(MakeEmptyCard("Треков пока нет. Загрузите первый!"));
                return;
            }

            foreach (var track in tracks)
                MyTracksContainer.Children.Add(MakeTrackRow(track));
        }

        private UIElement MakeTrackRow(Track track)
        {
            var card = new Border
            {
                Background   = new SolidColorBrush(Color.FromRgb(245, 244, 240)),
                CornerRadius = new CornerRadius(12),
                Padding      = new Thickness(12),
                Margin       = new Thickness(0, 0, 0, 8),
                Cursor       = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Cover
            var coverBorder = new Border
            {
                Width        = 44,
                Height       = 44,
                CornerRadius = new CornerRadius(8),
                Background   = new SolidColorBrush(Color.FromRgb(180, 200, 185)),
                Margin       = new Thickness(0, 0, 10, 0)
            };
            TrySetCover(coverBorder, track.CoverPath);
            Grid.SetColumn(coverBorder, 0);

            // Status colour + label
            (Color statusColor, string statusLabel) = track.PublishStatus switch
            {
                "Published" => (Color.FromRgb(42, 140, 84),   "✓ Опубликован"),
                "Pending"   => (Color.FromRgb(180, 130, 40),  "⏳ На проверке"),
                "Rejected"  => (Color.FromRgb(190, 50,  50),  "✗ Отклонён"),
                "Banned"    => (Color.FromRgb(100, 100, 100), "🚫 Заблокирован"),
                _           => (Color.FromRgb(100, 100, 100), track.PublishStatus)
            };

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleStack.Children.Add(new TextBlock
            {
                Text          = track.Title,
                FontSize      = 13,
                FontWeight    = FontWeights.SemiBold,
                Foreground    = new SolidColorBrush(Color.FromRgb(26, 26, 24)),
                TextTrimming  = TextTrimming.CharacterEllipsis
            });
            titleStack.Children.Add(new TextBlock
            {
                Text       = statusLabel,
                FontSize   = 10,
                Foreground = new SolidColorBrush(statusColor),
                Margin     = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(titleStack, 1);

            var playsStack = new StackPanel
            {
                Margin            = new Thickness(12, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            playsStack.Children.Add(new TextBlock
            {
                Text                = (track.Statistics?.PlayCount ?? 0).ToString(),
                FontSize            = 14,
                FontWeight          = FontWeights.Bold,
                Foreground          = new SolidColorBrush(Color.FromRgb(26, 26, 24)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            playsStack.Children.Add(new TextBlock
            {
                Text                = "▶ прослуш.",
                FontSize            = 9,
                Foreground          = new SolidColorBrush(Color.FromRgb(136, 136, 128)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            Grid.SetColumn(playsStack, 2);

            var likesStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            likesStack.Children.Add(new TextBlock
            {
                Text                = (track.Statistics?.LikesCount ?? 0).ToString(),
                FontSize            = 14,
                FontWeight          = FontWeights.Bold,
                Foreground          = new SolidColorBrush(Color.FromRgb(26, 26, 24)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            likesStack.Children.Add(new TextBlock
            {
                Text                = "♥ лайков",
                FontSize            = 9,
                Foreground          = new SolidColorBrush(Color.FromRgb(136, 136, 128)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            Grid.SetColumn(likesStack, 3);

            grid.Children.Add(coverBorder);
            grid.Children.Add(titleStack);
            grid.Children.Add(playsStack);
            grid.Children.Add(likesStack);

            card.Child = grid;
            card.MouseLeftButtonDown += (s, ev) => ShowTrackDetail(track);
            return card;
        }

        // ─────────────────────────────────────────────
        //  Track detail panel
        // ─────────────────────────────────────────────

        private void ShowTrackDetail(Track track)
        {
            PanelRightDefault.Visibility = Visibility.Collapsed;
            PanelTrackDetail.Visibility  = Visibility.Visible;

            // Cover
            TrackDetailCover.Background = new SolidColorBrush(Color.FromRgb(180, 200, 185));
            TrackDetailCover.Child = null;
            TrySetCoverOnBorder(TrackDetailCover, track.CoverPath);

            TrackDetailTitle.Text  = track.Title;
            TrackDetailArtist.Text = $"by {Session.UserName}";

            // Status badge colours
            (Color bgC, Color fgC, string stText) = track.PublishStatus switch
            {
                "Published" => (Color.FromRgb(230, 250, 240), Color.FromRgb(42, 140, 84),   "✓ Опубликован"),
                "Pending"   => (Color.FromRgb(255, 248, 225), Color.FromRgb(180, 130, 40),  "⏳ На проверке"),
                "Rejected"  => (Color.FromRgb(255, 230, 230), Color.FromRgb(190, 50,  50),  "✗ Отклонён"),
                "Banned"    => (Color.FromRgb(238, 238, 238), Color.FromRgb(100, 100, 100), "🚫 Заблокирован"),
                _           => (Color.FromRgb(238, 238, 238), Color.FromRgb(100, 100, 100), track.PublishStatus)
            };
            TrackStatusBadge.Background    = new SolidColorBrush(bgC);
            TrackStatusText.Foreground     = new SolidColorBrush(fgC);
            TrackStatusText.Text           = stText;

            TrackDetailPlays.Text = (track.Statistics?.PlayCount  ?? 0).ToString("N0");
            TrackDetailLikes.Text = (track.Statistics?.LikesCount ?? 0).ToString("N0");

            DrawChart(track.TrackID);
        }

        private void BackToDefault_Click(object sender, MouseButtonEventArgs e)
        {
            PanelTrackDetail.Visibility  = Visibility.Collapsed;
            PanelRightDefault.Visibility = Visibility.Visible;
        }

        // ─────────────────────────────────────────────
        //  Bar chart
        // ─────────────────────────────────────────────

        private void DrawChart(int trackId)
        {
            ChartCanvas.Children.Clear();

            const int    days   = 14;
            const double chartH = 88.0;

            Dictionary<DateTime, int> data;
            try   { data = HistoryService.GetListensByDay(trackId, days); }
            catch { data = new Dictionary<DateTime, int>(); }

            var since = DateTime.UtcNow.Date.AddDays(-(days - 1));
            var dates = Enumerable.Range(0, days).Select(i => since.AddDays(i)).ToList();
            int maxVal = data.Values.Any() ? Math.Max(1, data.Values.Max()) : 1;

            Dispatcher.BeginInvoke(() =>
            {
                double cw     = ChartCanvas.ActualWidth > 20 ? ChartCanvas.ActualWidth : 250;
                double gap    = 2.0;
                double barW   = (cw - (days - 1) * gap) / days;

                var accentBrush = new SolidColorBrush(Color.FromRgb(42, 232, 118));
                var mutedBrush  = new SolidColorBrush(Color.FromRgb(218, 218, 213));
                var gridBrush   = new SolidColorBrush(Color.FromRgb(228, 228, 223));
                var labelBrush  = new SolidColorBrush(Color.FromRgb(160, 160, 155));

                // Horizontal grid lines at 25 / 50 / 75 %
                for (int g = 1; g <= 3; g++)
                {
                    double y = chartH - (g / 4.0) * chartH;
                    ChartCanvas.Children.Add(new System.Windows.Shapes.Line
                    {
                        X1              = 0,
                        X2              = cw,
                        Y1              = y,
                        Y2              = y,
                        Stroke          = gridBrush,
                        StrokeThickness = 0.8,
                        StrokeDashArray = new DoubleCollection { 4, 3 }
                    });
                }

                for (int i = 0; i < days; i++)
                {
                    int count = data.TryGetValue(dates[i], out int c) ? c : 0;
                    double bh = count > 0
                        ? Math.Max(4.0, count / (double)maxVal * chartH)
                        : 3.0;

                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Width   = Math.Max(1, barW),
                        Height  = bh,
                        Fill    = count > 0 ? accentBrush : mutedBrush,
                        RadiusX = 3,
                        RadiusY = 3
                    };
                    Canvas.SetLeft(rect, i * (barW + gap));
                    Canvas.SetTop(rect,  chartH - bh);
                    ChartCanvas.Children.Add(rect);

                    // Date labels on first, last, and every 3rd bar
                    if (i == 0 || i == days - 1 || i % 3 == 0)
                    {
                        var lbl = new TextBlock
                        {
                            Text       = dates[i].ToString("dd.MM"),
                            FontSize   = 7.5,
                            Foreground = labelBrush
                        };
                        Canvas.SetLeft(lbl, i * (barW + gap));
                        Canvas.SetTop(lbl,  chartH + 3);
                        ChartCanvas.Children.Add(lbl);
                    }

                    // Value label above the bar (only when non-zero)
                    if (count > 0)
                    {
                        var cntLbl = new TextBlock
                        {
                            Text                = count.ToString(),
                            FontSize            = 7,
                            Foreground          = labelBrush,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        Canvas.SetLeft(cntLbl, i * (barW + gap));
                        Canvas.SetTop(cntLbl,  chartH - bh - 12);
                        ChartCanvas.Children.Add(cntLbl);
                    }
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void DrawSubscriberChart()
        {
            SubscriberChartCanvas.Children.Clear();
            const int days = 14;
            const double chartH = 72.0;

            // GetSubscriptionsByDay comes from SubscriptionService — returns Dictionary<DateTime,int>
            Dictionary<DateTime, int> data;
            try { data = SubscriptionService.GetSubscriptionsByDay(Session.UserId, days); }
            catch { data = new Dictionary<DateTime, int>(); }

            var since = DateTime.UtcNow.Date.AddDays(-(days - 1));
            var dates = Enumerable.Range(0, days).Select(i => since.AddDays(i)).ToList();
            int maxVal = data.Values.Any() ? Math.Max(1, data.Values.Max()) : 1;

            Dispatcher.BeginInvoke(() =>
            {
                double cw = SubscriberChartCanvas.ActualWidth > 20 ? SubscriberChartCanvas.ActualWidth : 250;
                double gap = 2.0;
                double barW = (cw - (days - 1) * gap) / days;

                var accentBrush = new SolidColorBrush(Color.FromRgb(102, 126, 234));   // purple-ish for subs
                var mutedBrush  = new SolidColorBrush(Color.FromRgb(218, 218, 213));
                var gridBrush   = new SolidColorBrush(Color.FromRgb(228, 228, 223));
                var labelBrush  = new SolidColorBrush(Color.FromRgb(160, 160, 155));

                for (int g = 1; g <= 3; g++)
                {
                    double y = chartH - (g / 4.0) * chartH;
                    SubscriberChartCanvas.Children.Add(new System.Windows.Shapes.Line
                    {
                        X1 = 0, X2 = cw, Y1 = y, Y2 = y,
                        Stroke = gridBrush, StrokeThickness = 0.8,
                        StrokeDashArray = new DoubleCollection { 4, 3 }
                    });
                }

                for (int i = 0; i < days; i++)
                {
                    int count = data.TryGetValue(dates[i], out int c) ? c : 0;
                    double bh = count > 0 ? Math.Max(4.0, count / (double)maxVal * chartH) : 3.0;

                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Width = Math.Max(1, barW), Height = bh,
                        Fill = count > 0 ? accentBrush : mutedBrush,
                        RadiusX = 3, RadiusY = 3
                    };
                    Canvas.SetLeft(rect, i * (barW + gap));
                    Canvas.SetTop(rect, chartH - bh);
                    SubscriberChartCanvas.Children.Add(rect);

                    if (i == 0 || i == days - 1 || i % 4 == 0)
                    {
                        var lbl = new TextBlock
                        {
                            Text = dates[i].ToString("dd.MM"), FontSize = 7.5,
                            Foreground = labelBrush
                        };
                        Canvas.SetLeft(lbl, i * (barW + gap));
                        Canvas.SetTop(lbl, chartH + 3);
                        SubscriberChartCanvas.Children.Add(lbl);
                    }
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // ─────────────────────────────────────────────
        //  Upload form handlers
        // ─────────────────────────────────────────────

        private void SelectCover_Click(object sender, MouseButtonEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Images|*.jpg;*.png;*.jpeg" };
            if (dlg.ShowDialog() == true)
            {
                _selectedCoverPath = dlg.FileName;
                NewTrackCoverPreview.Source = new BitmapImage(new Uri(_selectedCoverPath));
                CoverPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void SelectAudio_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Audio Files|*.mp3;*.wav" };
            if (dlg.ShowDialog() == true)
            {
                _selectedAudioPath = dlg.FileName;
                AudioFileName.Text = Path.GetFileName(dlg.FileName);
            }
        }

        private void UploadTrack_Click(object sender, RoutedEventArgs e)
        {
            string trackName = NewTrackName.Text;
            if (string.IsNullOrWhiteSpace(trackName) || string.IsNullOrEmpty(_selectedAudioPath))
            {
                MessageBox.Show("Введите название и выберите аудиофайл!");
                return;
            }

            try
            {
                string uniqueFileName = FileStorage.CopyTrackAudio(_selectedAudioPath, trackName);
                string destPath       = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", uniqueFileName);
                int    duration       = GetTrackDuration(destPath);

                string? finalCoverPath = null;
                if (!string.IsNullOrWhiteSpace(_selectedCoverPath))
                    finalCoverPath = FileStorage.CopyTrackCover(_selectedCoverPath);

                string genre = (GenreSelector.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

                TrackService.AddTrack(new Track
                {
                    Title         = trackName,
                    FilePath      = uniqueFileName,
                    CoverPath     = finalCoverPath,
                    Duration      = duration,
                    UploadDate    = DateTime.UtcNow,
                    ArtistID      = Session.UserId,
                    Genre         = genre,
                    PublishStatus = "Pending"
                });

                MessageBox.Show("Трек отправлен на проверку администратору.", "Заявка отправлена");

                // Оповещаем MainWindow, чтобы он показал тоаст подписчикам исполнителя
                TrackService.NotifyNewTrackUploaded(Session.UserId, Session.UserName, trackName);

                // Reset form
                NewTrackName.Clear();
                AudioFileName.Text          = "Файл не выбран";
                NewTrackCoverPreview.Source = null;
                CoverPlaceholder.Visibility = Visibility.Visible;
                _selectedAudioPath          = null;
                _selectedCoverPath          = null;

                LoadStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────

        /// <summary>Sets the Border background to an ImageBrush loaded from coverPath.</summary>
        private static void TrySetCover(Border border, string? coverPath)
        {
            if (string.IsNullOrEmpty(coverPath)) return;
            try
            {
                string fp = coverPath.Contains(':')
                    ? coverPath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoversLibrary", coverPath);
                if (File.Exists(fp))
                    border.Background = new ImageBrush(new BitmapImage(new Uri(fp)))
                    {
                        Stretch = Stretch.UniformToFill
                    };
            }
            catch { }
        }

        /// <summary>Same as TrySetCover but targets a Border that may host child content.</summary>
        private static void TrySetCoverOnBorder(Border border, string? coverPath)
        {
            if (string.IsNullOrEmpty(coverPath)) return;
            try
            {
                string fp = coverPath.Contains(':')
                    ? coverPath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoversLibrary", coverPath);
                if (File.Exists(fp))
                    border.Background = new ImageBrush(new BitmapImage(new Uri(fp)))
                    {
                        Stretch = Stretch.UniformToFill
                    };
            }
            catch { }
        }

        private UIElement MakeEmptyCard(string text) =>
            new Border
            {
                Background   = new SolidColorBrush(Color.FromRgb(245, 244, 240)),
                CornerRadius = new CornerRadius(12),
                Padding      = new Thickness(16),
                Child        = new TextBlock
                {
                    Text       = text,
                    FontSize   = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 128))
                }
            };

        private static int GetTrackDuration(string path)
        {
            using var f = TagLib.File.Create(path);
            return (int)f.Properties.Duration.TotalSeconds;
        }

        private static string CopyAudioToMusicLibrary(string src, string trackName)
        {
            string ext    = Path.GetExtension(src);
            string safe   = SanitizeFileName(trackName);
            if (string.IsNullOrWhiteSpace(safe)) safe = "track";

            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary");
            Directory.CreateDirectory(folder);

            string fn   = $"{safe}{ext}";
            string dest = Path.Combine(folder, fn);
            int    n    = 1;
            while (File.Exists(dest))
            {
                fn   = $"{safe}_{n++}{ext}";
                dest = Path.Combine(folder, fn);
            }

            File.Copy(src, dest, overwrite: false);
            return fn;
        }

        private static string CopyImageToProjectFolder(string src, string folder, bool keepName, bool absPath)
        {
            string dir      = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folder);
            Directory.CreateDirectory(dir);

            string ext      = Path.GetExtension(src);
            string baseName = keepName ? Path.GetFileNameWithoutExtension(src) : Guid.NewGuid().ToString();
            string fn       = $"{baseName}{ext}";
            string dest     = Path.Combine(dir, fn);
            int    n        = 1;
            while (File.Exists(dest))
            {
                fn   = $"{baseName}_{n++}{ext}";
                dest = Path.Combine(dir, fn);
            }

            File.Copy(src, dest, overwrite: false);
            return absPath ? dest : fn;
        }

        private static string SanitizeFileName(string name) =>
            new string(name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch)
                           .ToArray())
            .Trim();
    }
}
