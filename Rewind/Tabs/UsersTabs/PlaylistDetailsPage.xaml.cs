using Rewind.Contols;
using Rewind.Helpers;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Rewind.Tabs.UsersTabs
{
    public partial class PlaylistDetailsPage : UserControl
    {
        private readonly Playlist _playlist;
        private readonly List<TrackItem> _trackItems = new();
        // Каждое нажатие Play в плейлисте считается отдельным прослушиванием

        public PlaylistDetailsPage(Playlist playlist)
        {
            InitializeComponent();
            _playlist = playlist;
            LoadPlaylist();

            Session.PlaylistChanged += OnPlaylistChanged;
            Unloaded += (_, _) => Session.PlaylistChanged -= OnPlaylistChanged;
        }

        private void OnPlaylistChanged(int playlistId)
        {
            if (playlistId != _playlist.PlaylistID) return;
            Dispatcher.Invoke(LoadPlaylist);
        }

        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;

        private void LoadPlaylist()
        {
            PlaylistTitleText.Text = _playlist.Title;
            PlaylistMetaText.Text = $"{_playlist.PlaylistTracks?.Count ?? 0} треков";
            UpdateStatsText();
            TrySetCover(_playlist.CoverPath);

            bool canSave = _playlist.OwnerID != Session.UserId;
            SaveBtn.Visibility = canSave ? Visibility.Visible : Visibility.Collapsed;
            RefreshSaveButton();

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
            RegisterPlaylistListen();
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.PlayTrackFromContext(clicked, _trackItems);
        }

        public void RegisterPlaylistListen()
        {
            if (_playlist.PlaylistID <= 0) return;
            if (!Session.TryEnterPlaybackScope("playlist", _playlist.PlaylistID)) return;
            PlaylistListenService.RegisterListen(Session.UserId, _playlist.PlaylistID);
            UpdateStatsText();
        }

        private void UpdateStatsText()
        {
            // Используем БД-счётчики (не кеш в памяти)
            var saved = SavedPlaylistService.GetSavedCount(_playlist.PlaylistID);
            var listens = PlaylistListenService.GetListenerCount(_playlist.PlaylistID);
            PlaylistStatsText.Text = $"♥ {saved} сохранили  •  ► {listens} прослушали";
        }

        private void RefreshSaveButton()
        {
            if (_playlist.OwnerID == Session.UserId) return;
            bool saved = SavedPlaylistService.IsSaved(Session.UserId, _playlist.PlaylistID);
            SaveBtnText.Text = saved ? "♥ Сохранён" : "♥ Сохранить";
        }

        private static string FormatDuration(int sec) => $"{sec / 60}:{sec % 60:D2}";

        private void TrySetCover(string? coverPath)
        {
            if (string.IsNullOrWhiteSpace(coverPath)) return;
            try
            {
                string fp = FileStorage.ResolveImagePath(coverPath, "PlaylistCovers");
                if (File.Exists(fp))
                {
                    PlaylistCoverImage.Source = new BitmapImage(new System.Uri(fp));
                    PlaylistCoverPlaceholder.Visibility = Visibility.Collapsed;
                }
            }
            catch { }
        }

        private void SaveToMe_Click(object sender, MouseButtonEventArgs e)
        {
            if (_playlist.PlaylistID <= 0)
            {
                MessageBox.Show("Нельзя сохранить неопубликованный плейлист.");
                return;
            }

            bool isSaved = SavedPlaylistService.Toggle(Session.UserId, _playlist.PlaylistID);
            RefreshSaveButton();
            UpdateStatsText();
            MessageBox.Show(isSaved ? "Плейлист сохранён — он появится в вашем профиле." : "Плейлист удалён из сохранённых.");
        }

        private void Back_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.ShowPlaylistsPage();
        }
    }
}
