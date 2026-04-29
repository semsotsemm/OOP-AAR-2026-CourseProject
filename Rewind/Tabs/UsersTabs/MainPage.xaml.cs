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
            Loaded += (_, _) =>
            {
                LoadMusicFromFolder();
                LoadPopularPlaylists();
                LoadFeaturedTrack();
            };
        }

        private void LoadFeaturedTrack()
        {
            try
            {
                _featuredTrack = TrackService.GetMostPopularTrack();
                if (_featuredTrack == null) return;

                var artistName = UserService.GetUserById(_featuredTrack.ArtistID)?.Nickname ?? "Неизвестен";

                FeaturedTrackTitle.Text = _featuredTrack.Title;
                FeaturedTrackArtist.Text = $"{artistName} · {_featuredTrack.Statistics?.PlayCount ?? 0} прослушиваний";

                if (!string.IsNullOrWhiteSpace(_featuredTrack.CoverPath))
                {
                    var cover = IconAssets.LoadBitmap(_featuredTrack.CoverPath);
                    if (cover != null)
                    {
                        FeaturedTrackCover.Source = cover;
                        FeaturedTrackCover.Width = 100;
                        FeaturedTrackCover.Height = 100;
                    }
                }
            }
            catch { }
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
                    _trackItems.Add(item);
                    MusicContainer.Children.Add(item);
                }
            }
            catch { }
        }

        private void LoadPopularPlaylists()
        {
            PopularPlaylistsContainer.Children.Clear();
            var top = PlaylistAnalyticsService.GetTopPlaylists(8);
            foreach (var playlist in top)
            {
                var likes = PlaylistAnalyticsService.GetLikesCount(playlist.PlaylistID);
                var listens = PlaylistAnalyticsService.GetListenersCount(playlist.PlaylistID);
                var card = new AlbumCard
                {
                    AlbumTitle = playlist.Title,
                    Artist = $"♥ {likes}  •  ▶ {listens}",
                    StartColor = (Color)ColorConverter.ConvertFromString("#11998e"),
                    EndColor = (Color)ColorConverter.ConvertFromString("#38ef7d"),
                    Cursor = Cursors.Hand
                };
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
                    IslandSizeSlider.Value = mainWindow.IslandScale;
                    IslandOpacitySlider.Value = mainWindow.IslandOpacity;
                }
                _settingsInitialized = true;
                ApplyIslandSettings();
            }
        }

        private void IslandSettings_Changed(object sender, RoutedEventArgs e)
            => ApplyIslandSettings();

        private void ApplyIslandSettings()
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.UpdateIslandSettings(
                    IslandEnabledToggle.IsChecked == true,
                    IslandSizeSlider.Value,
                    IslandOpacitySlider.Value);
            }
        }

        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;
    }
}