using Rewind.Contols;
using Rewind.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Rewind.Tabs.UsersTabs
{
    public partial class PlaylistDetailsPage : UserControl
    {
        private readonly Playlist _playlist;
        private readonly List<TrackItem> _trackItems = new();
        private bool _listenRegistered;

        public PlaylistDetailsPage(Playlist playlist)
        {
            InitializeComponent();
            _playlist = playlist;
            LoadPlaylist();
        }

        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;

        private void LoadPlaylist()
        {
            PlaylistTitleText.Text = _playlist.Title;
            PlaylistMetaText.Text = $"{_playlist.PlaylistTracks?.Count ?? 0} треков";
            UpdateStatsText();

            bool canSave = _playlist.OwnerID != Session.UserId;
            SaveBtn.Visibility = canSave ? Visibility.Visible : Visibility.Collapsed;
            SaveBtnText.Text = IsAlreadySaved() ? "Уже добавлен" : "+ Добавить себе";

            TracksContainer.Children.Clear();
            _trackItems.Clear();

            var tracks = _playlist.PlaylistTracks?.Select(pt => pt.Track).Where(t => t != null).Cast<Track>().ToList() ?? new List<Track>();
            if (tracks.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
                return;
            }

            EmptyText.Visibility = Visibility.Collapsed;
            foreach (var track in tracks)
            {
                var artist = UserService.GetUserById(track.ArtistID)?.Nickname ?? "Неизвестный артист";
                var fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", track.FilePath);
                var item = new TrackItem(track.TrackID, track.Title, artist, FormatDuration(track.Duration), fullPath, track.CoverPath, track.Duration);
                item.MouseLeftButtonDown += OnTrackClick;
                _trackItems.Add(item);
                TracksContainer.Children.Add(item);
            }
        }

        private void OnTrackClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TrackItem clicked) return;

            if (!_listenRegistered)
            {
                PlaylistAnalyticsService.RegisterListen(Session.UserId, _playlist.PlaylistID);
                _listenRegistered = true;
                UpdateStatsText();
            }

            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.PlayTrackFromContext(clicked, _trackItems);
        }

        private void UpdateStatsText()
        {
            var likes = PlaylistAnalyticsService.GetLikesCount(_playlist.PlaylistID);
            var listens = PlaylistAnalyticsService.GetListenersCount(_playlist.PlaylistID);
            PlaylistStatsText.Text = $"Лайки: {likes}  •  Прослушали: {listens}";
        }

        private bool IsAlreadySaved()
            => Session.CachedPlaylists.Any(p => p.OwnerID == Session.UserId && p.Title == _playlist.Title);

        private static string FormatDuration(int sec) => $"{sec / 60}:{sec % 60:D2}";

        private void SaveToMe_Click(object sender, MouseButtonEventArgs e)
        {
            if (IsAlreadySaved())
            {
                MessageBox.Show("Этот плейлист уже есть у вас.");
                return;
            }

            var newPlaylist = Session.CreatePlaylist($"{_playlist.Title} (копия)", _playlist.CoverPath, false);
            foreach (var track in _playlist.PlaylistTracks ?? new List<PlaylistTrack>())
                Session.AddTrackToPlaylist(newPlaylist, track.TrackID);

            PlaylistAnalyticsService.ToggleLike(Session.UserId, _playlist.PlaylistID);
            SaveBtnText.Text = "Уже добавлен";
            UpdateStatsText();
            MessageBox.Show("Плейлист добавлен в ваши.");
        }

        private void Back_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.ShowPlaylistsPage();
        }
    }
}
