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
    /// View поверх <see cref="AlbumDetailsViewModel"/>. Логика сохранения/
    /// прослушивания — в VM. Code-behind только отрисовывает треки и обложку.
    /// </summary>
    public partial class AlbumDetailsPage : UserControl
    {
        private readonly AlbumDetailsViewModel _vm;
        private readonly List<TrackItem> _trackItems = new();

        public AlbumDetailsPage(Album album)
        {
            InitializeComponent();
            _vm = new AlbumDetailsViewModel(
                album,
                ServiceLocator.Resolve<INavigationService>(),
                ServiceLocator.Resolve<IDialogService>());
            DataContext = _vm;
            _vm.PropertyChanged += (_, _) => RefreshUi();
            Loaded += (_, _) => { RenderTracks(); RefreshUi(); };
        }

        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;

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
                var artist = UserService.GetUserById(track.ArtistID)?.Nickname ?? "Неизвестный";
                var fullPath = Path.Combine(Rewind.Helpers.FileStorage.DataRoot, "MusicLibrary", track.FilePath);
                var item = new TrackItem(track.TrackID, track.Title, artist,
                    track.Duration > 0 ? $"{track.Duration / 60}:{track.Duration % 60:D2}" : "",
                    fullPath, track.CoverPath, track.Duration);

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
            AlbumTitleText.Text = _vm.Title;
            AlbumArtistText.Text = _vm.ArtistName;
            AlbumStatsText.Text = _vm.StatsText;
            SaveAlbumText.Text = _vm.SaveButtonText;
            SaveAlbumBtn.Background = _vm.IsSaved
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 140, 84))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 24));
            TrySetCover(_vm.CoverPath);
        }

        private void SaveAlbum_Click(object sender, MouseButtonEventArgs e) => _vm.ToggleSaveCommand.Execute(null);
        private void Back_Click(object sender, MouseButtonEventArgs e) => _vm.BackCommand.Execute(null);

        private void TrySetCover(string? coverPath)
        {
            if (string.IsNullOrWhiteSpace(coverPath)) return;
            try
            {
                string fp = FileStorage.ResolveImagePath(coverPath, "AlbumCovers");
                if (File.Exists(fp))
                {
                    AlbumCoverImage.Source = new BitmapImage(new Uri(fp));
                    AlbumCoverPlaceholder.Visibility = Visibility.Collapsed;
                }
            }
            catch { }
        }

        public void RegisterAlbumListen() => _vm.RegisterListenOnce();
    }
}
