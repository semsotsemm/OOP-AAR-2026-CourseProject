using System.Windows;
using Rewind.Contols;
using System.Windows.Input;
using System.Windows.Controls;
using Rewind.Helpers;
using System.Windows.Media;

namespace Rewind.Tabs.UsersTabs
{
    public partial class MainPage : UserControl
    {
        private readonly List<TrackItem> _trackItems = new();
        private bool _showAllTracks;
        private bool _settingsInitialized;
        private Track? _featuredTrack;

        public MainPage()
        {
            InitializeComponent();

            UpdateGreeting();
            
            TrackService.OnPlayCountUpdated += Global_OnPlayCountUpdated;

            Loaded += (_, _) =>
            {
                LoadMusicFromFolder();
                LoadPopularPlaylists();
                LoadFeaturedTrack();
            };

            OpacityLabel.Text = $"{IslandOpacitySlider.Value * 100:F0}%";

            Unloaded += (_, _) => TrackService.OnPlayCountUpdated -= Global_OnPlayCountUpdated;
        }

        private void Global_OnPlayCountUpdated(int trackId, int newCount)
        {
            // Если обновился именно тот трек, который сейчас "в топе" на экране
            if (_featuredTrack != null && _featuredTrack.TrackID == trackId)
            {
                Dispatcher.Invoke(() =>
                {
                    // Обновляем данные в объекте и UI
                    if (_featuredTrack.Statistics != null)
                        _featuredTrack.Statistics.PlayCount = newCount;

                    UpdateFeaturedTrackUI();
                });
            }
        }

        private void LoadFeaturedTrack()
        {
            try
            {

                _featuredTrack = TrackService.GetMostPopularTrack();
                if (_featuredTrack == null) return;


                var artistName = UserService.GetUserById(_featuredTrack.ArtistID)?.Nickname ?? "Неизвестен";

                FeaturedTrackTitle.Text = _featuredTrack.Title;

                UpdateFeaturedTrackUI();


                if (!string.IsNullOrWhiteSpace(_featuredTrack.CoverPath))
                {
                    var cover = IconAssets.LoadBitmap(_featuredTrack.CoverPath);
                    if (cover != null)
                    {
                        FeaturedTrackCover.Source = cover;
                        FeaturedTrackCover.Width = 120;
                        FeaturedTrackCover.Height = 120;
                    }
                }
            }
            catch { }
        }

        private void UpdateFeaturedTrackUI()
        {
            Dispatcher.Invoke(() =>
            {
                if (_featuredTrack == null) return;

                var artistName = UserService.GetUserById(_featuredTrack.ArtistID)?.Nickname ?? "Неизвестен";

                FeaturedTrackArtist.Text = $"{artistName} · {_featuredTrack.Statistics?.PlayCount ?? 0} прослушиваний";
            });
        }


        private void UpdateGreeting()
        {
            var h = DateTime.Now.Hour;
            string time = h < 6 ? "Доброй ночи" : h < 12 ? "Доброе утро" :
                          h < 18 ? "Добрый день" : "Добрый вечер";
            GreetingText.Text = $"{time}, {Session.UserName}";
        }

        private void LoadMusicFromFolder()
        {
            try
            {
                MusicContainer.Children.Clear();
                _trackItems.Clear();

                List<Track> tracks = TrackService.GetAllTracks();
                if (!_showAllTracks)
                    tracks = tracks.Take(7).ToList();

                foreach (var track in tracks)
                {
                    string durStr = FormatDuration(track.Duration);
                    string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", track.FilePath);
                    var item = new TrackItem(track.TrackID, track.Title, UserService.GetUserById(track.ArtistID).Nickname, durStr, fullPath,  track.CoverPath , track.Duration);
                    item.PlayClicked += TrackItem_PlayClicked;
                    _trackItems.Add(item);
                    MusicContainer.Children.Add(item);
                }
            }
            catch { }
        }

        private void TrackItem_PlayClicked(object sender, RoutedEventArgs e)
        {
            var artistName = UserService.GetUserById(_featuredTrack.ArtistID)?.Nickname ?? "Неизвестен";

            FeaturedTrackArtist.Text = $"{artistName} · {_featuredTrack.Statistics?.PlayCount ?? 0} прослушиваний";
        }

        private void LoadPopularPlaylists()
        {
            PopularPlaylistsContainer.Children.Clear();
            var top = PlaylistAnalyticsService.GetTopPlaylists(8);

            foreach (var playlist in top)
            {
                var likes = PlaylistAnalyticsService.GetLikesCount(playlist.PlaylistID);
                var listens = PlaylistAnalyticsService.GetListenersCount(playlist.PlaylistID);

                // Создаем карточку
                var card = new AlbumCard
                {
                    AlbumTitle = playlist.Title,
                    Artist = $"♥ {likes}  •  ▶ {listens}",
                    StartColor = (Color)ColorConverter.ConvertFromString("#11998e"),
                    EndColor = (Color)ColorConverter.ConvertFromString("#38ef7d"),
                    Cursor = Cursors.Hand
                };

                if (!string.IsNullOrEmpty(playlist.CoverPath))
                {
                    try
                    {
                        card.CoverImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(playlist.CoverPath, UriKind.RelativeOrAbsolute));
                    }
                    catch (Exception)
                    {
                        card.CoverImageSource = null;
                    }
                }

                card.MouseLeftButtonDown += (_, _) =>
                {
                    if (Window.GetWindow(this) is MainWindow mainWindow)
                        mainWindow.OpenPlaylistDetails(playlist);
                };

                PopularPlaylistsContainer.Children.Add(card);
            }
        }

        private static string FormatDuration(double seconds)
        {
            if (seconds <= 0) return "0:00";
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.Hours > 0 ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}" : $"{ts.Minutes}:{ts.Seconds:D2}";
        }

        private void FeaturedPlay_Click(object sender, MouseButtonEventArgs e)
        {
            if (_featuredTrack == null)
            {
                if (_trackItems.Count > 0) PlayFirst();
                return;
            }

            string fullPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", _featuredTrack.FilePath);
            string durStr = FormatDuration(_featuredTrack.Duration);
            var artistName = UserService.GetUserById(_featuredTrack.ArtistID)?.Nickname ?? "";
            FeaturedTrackArtist.Text = $"{artistName} · {_featuredTrack.Statistics?.PlayCount ?? 0} прослушиваний";

            var featuredItem = new TrackItem(
                _featuredTrack.TrackID,
                _featuredTrack.Title,
                artistName,
                durStr,
                fullPath,
                _featuredTrack.CoverPath,
                _featuredTrack.Duration);

            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.PlayTrackFromContext(featuredItem, _trackItems);
        }

        private void PlayFirst()
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.PlayTrackFromContext(_trackItems[0], _trackItems);
        }

        private void ShowAllTracksBtn_Click(object sender, MouseButtonEventArgs e)
        {
            _showAllTracks = !_showAllTracks;
            ShowAllTracksBtn.Text = _showAllTracks ? "Свернуть ↑" : "Все →";
            LoadMusicFromFolder();
        }

        private void Bell_Click(object sender, MouseButtonEventArgs e)
        {
            NotificationsPanel.Visibility = NotificationsPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;

            if (!_settingsInitialized)
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    IslandEnabledToggle.IsChecked = mainWindow.IslandEnabled;
                    IslandOpacitySlider.Value = mainWindow.IslandOpacity;
                }
                _settingsInitialized = true;
                ApplyIslandSettings();
            }
        }

        private void IslandSettings_Changed(object sender, RoutedEventArgs e)
        {
            if (OpacityLabel == null || IslandOpacitySlider == null)
            {
                return;
            }
            OpacityLabel.Text = $"{IslandOpacitySlider.Value * 100:F0}%";
            ApplyIslandSettings();
        }

        private void ApplyIslandSettings()
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.UpdateIslandSettings(
                    IslandEnabledToggle.IsChecked == true,
                    IslandOpacitySlider.Value);
            }
        }

        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;
    }
}