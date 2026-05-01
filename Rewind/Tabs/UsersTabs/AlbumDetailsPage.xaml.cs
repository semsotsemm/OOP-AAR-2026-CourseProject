using Rewind.Contols;
using Rewind.Helpers;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rewind.Tabs.UsersTabs
{
    public partial class AlbumDetailsPage : UserControl
    {
        private readonly Album _album;
        private readonly List<TrackItem> _trackItems = new();

        public AlbumDetailsPage(Album album)
        {
            InitializeComponent();
            _album = album;
            Loaded += (_, _) => LoadAlbum();
        }

        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;

        private void LoadAlbum()
        {
            AlbumTitleText.Text = _album.Title;
            AlbumArtistText.Text = _album.Artist?.Nickname ?? UserService.GetUserById(_album.ArtistId)?.Nickname ?? "Исполнитель";
            UpdateStats();
            RefreshSaveButton();
            TrySetCover(_album.CoverPath);

            TracksContainer.Children.Clear();
            _trackItems.Clear();
            var tracks = _album.AlbumTracks?.Select(at => at.Track).Where(t => t != null).Cast<Track>().ToList() ?? new List<Track>();
            if (tracks.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
                return;
            }
            EmptyText.Visibility = Visibility.Collapsed;

            foreach (var track in tracks)
            {
                var artist = UserService.GetUserById(track.ArtistID)?.Nickname ?? "Неизвестный";
                var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", track.FilePath);
                var item = new TrackItem(track.TrackID, track.Title, artist, FormatDuration(track.Duration), fullPath, track.CoverPath, track.Duration);
                item.MouseLeftButtonDown += OnTrackClick;
                _trackItems.Add(item);
                TracksContainer.Children.Add(item);
            }
        }

        private void OnTrackClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TrackItem clicked) return;
            RegisterAlbumListen();
            if (Window.GetWindow(this) is MainWindow mw)
                mw.PlayTrackFromContext(clicked, _trackItems);
        }

        public void RegisterAlbumListen()
        {
            if (_album.AlbumId <= 0) return;
            if (!Session.TryEnterPlaybackScope("album", _album.AlbumId)) return;
            AlbumService.RegisterListen(Session.UserId, _album.AlbumId);
            UpdateStats();
        }

        private void UpdateStats()
        {
            int tracks = _album.AlbumTracks?.Count ?? 0;
            int saves = 0, listens = 0;
            try { saves = AlbumService.GetSavedCount(_album.AlbumId); } catch { }
            try { listens = AlbumService.GetListenCount(_album.AlbumId); } catch { }
            AlbumStatsText.Text = $"{tracks} треков  •  ♥ {saves} сохранили  •  ► {listens} прослушали";
        }

        private void RefreshSaveButton()
        {
            bool saved = false;
            try { saved = AlbumService.IsSaved(Session.UserId, _album.AlbumId); } catch { }
            SaveAlbumText.Text = saved ? "✓ Сохранён" : "♥ Сохранить";
            SaveAlbumBtn.Background = saved
                ? new SolidColorBrush(Color.FromRgb(42, 140, 84))
                : new SolidColorBrush(Color.FromRgb(26, 26, 24));
        }

        private void SaveAlbum_Click(object sender, MouseButtonEventArgs e)
        {
            bool saved = AlbumService.ToggleSave(Session.UserId, _album.AlbumId);
            RefreshSaveButton();
            UpdateStats();
            MessageBox.Show(saved ? "Альбом сохранён в профиль." : "Альбом удалён из сохранённых.", "Rewind");
        }

        private void Back_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw) mw.NavigateBack();
        }

        private void TrySetCover(string? coverPath)
        {
            if (string.IsNullOrWhiteSpace(coverPath)) return;
            try
            {
                string fp = coverPath.Contains(":") ? coverPath : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoversLibrary", coverPath);
                if (File.Exists(fp))
                    AlbumCover.Background = new ImageBrush(new BitmapImage(new Uri(fp))) { Stretch = Stretch.UniformToFill };
            }
            catch { }
        }

        private static string FormatDuration(int sec) => $"{sec / 60}:{sec % 60:D2}";
    }
}
