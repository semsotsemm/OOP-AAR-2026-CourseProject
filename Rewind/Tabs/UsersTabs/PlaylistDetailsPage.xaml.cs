using Rewind.Contols;
using Rewind.Helpers;
using Rewind.MVVM.Services;
using Rewind.MVVM.ViewModels.Pages;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Rewind.Tabs.UsersTabs
{
    /// <summary>
    /// View поверх <see cref="PlaylistDetailsViewModel"/>.
    /// Бизнес-логика (сохранение/прослушивание/перезагрузка) в VM.
    /// </summary>
    public partial class PlaylistDetailsPage : UserControl
    {
        private readonly PlaylistDetailsViewModel _vm;
        private readonly List<TrackItem> _trackItems = new();

        public PlaylistDetailsPage(Playlist playlist)
        {
            InitializeComponent();
            _vm = new PlaylistDetailsViewModel(
                playlist,
                ServiceLocator.Resolve<INavigationService>(),
                ServiceLocator.Resolve<IDialogService>());
            DataContext = _vm;
            _vm.PropertyChanged += (_, _) => RefreshUi();

            RenderTracks();
            RefreshUi();

            Unloaded += (_, _) => _vm.Dispose();
        }

        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;
        public void RegisterPlaylistListen() => _vm.RegisterListenOnce();

        private void RenderTracks()
        {
            TracksContainer.Children.Clear();
            _trackItems.Clear();

            if (_vm.Tracks.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
                return;
            }
            EmptyText.Visibility = Visibility.Collapsed;

            foreach (var track in _vm.Tracks)
            {
                var artist = UserService.GetUserById(track.ArtistID)?.Nickname ?? "Неизвестный артист";
                var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", track.FilePath);
                var item = new TrackItem(track.TrackID, track.Title, artist,
                    $"{track.Duration / 60}:{track.Duration % 60:D2}",
                    fullPath, track.CoverPath, track.Duration);
                item.PlaylistContext = _vm.Playlist;
                item.MouseLeftButtonDown += (s, _) =>
                {
                    if (s is not TrackItem clicked) return;
                    _vm.RegisterListenOnce();
                    if (Window.GetWindow(this) is MainWindow mw)
                        mw.PlayTrackFromContext(clicked, _trackItems);
                };
                _trackItems.Add(item);
                TracksContainer.Children.Add(item);
            }
        }

        private void RefreshUi()
        {
            PlaylistTitleText.Text = _vm.Title;
            PlaylistMetaText.Text = _vm.MetaText;
            PlaylistStatsText.Text = _vm.StatsText;
            SaveBtn.Visibility = _vm.CanSave ? Visibility.Visible : Visibility.Collapsed;
            SaveBtnText.Text = _vm.SaveButtonText;
            TrySetCover(_vm.CoverPath);
            // Перерисовываем список только если меняется число треков (Reload)
            if (TracksContainer.Children.Count != _vm.Tracks.Count) RenderTracks();
        }

        private void TrySetCover(string? coverPath)
        {
            if (string.IsNullOrWhiteSpace(coverPath)) return;
            try
            {
                string fp = FileStorage.ResolveImagePath(coverPath, "PlaylistCovers");
                if (File.Exists(fp))
                {
                    PlaylistCoverImage.Source = new BitmapImage(new Uri(fp));
                    PlaylistCoverPlaceholder.Visibility = Visibility.Collapsed;
                }
            }
            catch { }
        }

        private void SaveToMe_Click(object sender, MouseButtonEventArgs e) => _vm.ToggleSaveCommand.Execute(null);
        private void Back_Click(object sender, MouseButtonEventArgs e) => _vm.BackCommand.Execute(null);
    }
}
