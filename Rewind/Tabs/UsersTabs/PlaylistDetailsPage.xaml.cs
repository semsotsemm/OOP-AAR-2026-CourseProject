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

            if (!_listenRegistered && _playlist.PlaylistID > 0)
            {
                _listenRegistered = true;
                int pid = _playlist.PlaylistID;
                // Регистрируем прослушивание в фоне, обновляем UI после записи в БД
                System.Threading.Tasks.Task.Run(() =>
                    PlaylistListenService.RegisterListen(Session.UserId, pid))
                    .ContinueWith(_ =>
                        Dispatcher.BeginInvoke(UpdateStatsText));
            }

            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.PlayTrackFromContext(clicked, _trackItems);
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
