using Rewind.Contols;
using Rewind.Helpers;
using Rewind.MVVM.ViewModels.Pages;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rewind.Tabs.UsersTabs
{
    /// <summary>
    /// View поверх <see cref="MainPageViewModel"/>. VM отвечает за данные/команды,
    /// View — за создание TrackItem/AlbumCard и управление визуальными деталями
    /// (бейдж уведомлений, тогглы Island/Notif, тост-уведомления).
    /// </summary>
    public partial class MainPage : UserControl
    {
        private readonly MainPageViewModel _vm = new();
        private readonly List<TrackItem> _trackItems = new();

        public MainPage()
        {
            InitializeComponent();
            DataContext = _vm;

            _vm.TracksChanged += RenderTracks;
            _vm.PlaylistsChanged += RenderPlaylists;
            _vm.FeaturedChanged += RefreshFeaturedUi;

            Loaded += MainPage_Loaded;
            Unloaded += MainPage_Unloaded;
            OpacityLabel.Text = $"{IslandOpacitySlider.Value * 100:F0}%";
        }

        private void MainPage_Loaded(object? sender, RoutedEventArgs e)
        {
            UpdateGreeting();
            _vm.LoadAll();
            RefreshFeaturedUi();

            if (Window.GetWindow(this) is MainWindow mw)
            {
                mw.PlaybackStateChanged += OnPlaybackStateChanged;

                IslandEnabledToggle.IsChecked = mw.IslandEnabled;
                IslandOpacitySlider.Value = mw.IslandOpacity;
                NotifNewTracks.IsChecked = Session.NotifNewTracksEnabled;
                NotifPush.IsChecked = Session.NotifPushEnabled;
                NotifNewTracks.Checked   += (_, _) => Session.NotifNewTracksEnabled = true;
                NotifNewTracks.Unchecked += (_, _) => Session.NotifNewTracksEnabled = false;
                NotifPush.Checked   += (_, _) => Session.NotifPushEnabled = true;
                NotifPush.Unchecked += (_, _) => Session.NotifPushEnabled = false;
            }

            UpdateBadge();
            CheckSubscriptionNotifications();
        }

        private void MainPage_Unloaded(object? sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mw) mw.PlaybackStateChanged -= OnPlaybackStateChanged;
            _vm.Dispose();
        }

        // ─── VM-driven рендер ───

        private void RenderTracks()
        {
            MusicContainer.Children.Clear();
            _trackItems.Clear();

            foreach (var track in _vm.Tracks)
            {
                string durStr = $"{track.Duration / 60}:{track.Duration % 60:D2}";
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", track.FilePath);
                var artist = UserService.GetUserById(track.ArtistID)?.Nickname ?? "Неизвестный";
                var item = new TrackItem(track.TrackID, track.Title, artist, durStr, fullPath, track.CoverPath, track.Duration);
                item.PlayClicked += (_, _) => RefreshFeaturedUi();
                _trackItems.Add(item);
                MusicContainer.Children.Add(item);
            }
        }

        private void RenderPlaylists()
        {
            PopularPlaylistsContainer.Children.Clear();
            foreach (var (playlist, saves, listens) in _vm.PopularPlaylists)
            {
                var card = new AlbumCard
                {
                    AlbumTitle = playlist.Title,
                    Artist = $"♥ {saves}  •  ► {listens}",
                    StartColor = (Color)ColorConverter.ConvertFromString("#11998e"),
                    EndColor = (Color)ColorConverter.ConvertFromString("#38ef7d"),
                    Cursor = Cursors.Hand
                };

                if (!string.IsNullOrEmpty(playlist.CoverPath))
                {
                    try
                    {
                        string coverPath = FileStorage.ResolveImagePath(playlist.CoverPath, "PlaylistCovers");
                        if (!string.IsNullOrEmpty(coverPath) && System.IO.File.Exists(coverPath))
                            card.CoverImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(coverPath));
                    }
                    catch { card.CoverImageSource = null; }
                }

                var captured = playlist;
                card.MouseLeftButtonDown += (_, _) => _vm.OpenPlaylist(captured);
                PopularPlaylistsContainer.Children.Add(card);
            }
        }

        private void RefreshFeaturedUi()
        {
            FeaturedTrackTitle.Text = _vm.FeaturedTitle;
            FeaturedTrackArtist.Text = _vm.FeaturedArtistInfo;
            ShowAllTracksBtn.Text = _vm.ShowAllLabel;

            if (_vm.FeaturedTrack != null && !string.IsNullOrWhiteSpace(_vm.FeaturedTrack.CoverPath))
            {
                try
                {
                    string fp = FileStorage.ResolveImagePath(_vm.FeaturedTrack.CoverPath);
                    if (System.IO.File.Exists(fp))
                    {
                        var bmp = new System.Windows.Media.Imaging.BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(fp);
                        bmp.EndInit();
                        bmp.Freeze();
                        FeaturedTrackCover.Source = bmp;
                        FeaturedTrackCover.Width = 120; FeaturedTrackCover.Height = 120;
                    }
                }
                catch { }
            }

            // Иконка play/pause «трека дня»
            if (_vm.FeaturedTrack != null && Window.GetWindow(this) is MainWindow mw)
            {
                bool isFp = mw.CurrentTrack?.TrackId == _vm.FeaturedTrack.TrackID && mw.IsPlaying;
                FeaturedPlayIconBrush.ImageSource = IconAssets.LoadBitmap(isFp ? "player_pause.png" : "player_play.png");
                FeaturedPlayLabel.Text = isFp ? " Пауза" : " Слушать";
            }
        }

        // ─── Хендлеры ───

        private void OnPlaybackStateChanged() => Dispatcher.Invoke(RefreshFeaturedUi);

        private void UpdateGreeting() => GreetingText.Text = _vm.Greeting;

        private void FeaturedPlay_Click(object sender, MouseButtonEventArgs e)
        {
            if (_vm.FeaturedTrack == null)
            {
                if (_trackItems.Count > 0 && Window.GetWindow(this) is MainWindow mw0)
                    mw0.PlayTrackFromContext(_trackItems[0], _trackItems);
                return;
            }

            if (Window.GetWindow(this) is not MainWindow mw) return;

            if (mw.CurrentTrack?.TrackId == _vm.FeaturedTrack.TrackID)
            { mw.TogglePlayPause(); return; }

            string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", _vm.FeaturedTrack.FilePath);
            string durStr = $"{_vm.FeaturedTrack.Duration / 60}:{_vm.FeaturedTrack.Duration % 60:D2}";
            var artistName = UserService.GetUserById(_vm.FeaturedTrack.ArtistID)?.Nickname ?? "";

            var featuredItem = new TrackItem(
                _vm.FeaturedTrack.TrackID, _vm.FeaturedTrack.Title, artistName,
                durStr, fullPath, _vm.FeaturedTrack.CoverPath, _vm.FeaturedTrack.Duration);

            Session.AddListenedTrack(_vm.FeaturedTrack.TrackID, _vm.FeaturedTrack.Duration);
            Session.ResetPlaybackScope();
            mw.PlayTrackFromContext(featuredItem, _trackItems);
        }

        private void ShowAllTracksBtn_Click(object sender, MouseButtonEventArgs e)
            => _vm.ToggleShowAllCommand.Execute(null);

        private void Bell_Click(object sender, MouseButtonEventArgs e)
        {
            bool opening = NotificationsPanel.Visibility != Visibility.Visible;
            NotificationsPanel.Visibility = opening ? Visibility.Visible : Visibility.Collapsed;
            if (opening && Session.NotificationCount > 0)
            {
                Session.NotificationCount = 0;
                NotifBadge.Visibility = Visibility.Collapsed;
            }
        }

        private void IslandSettings_Changed(object sender, RoutedEventArgs e)
        {
            if (OpacityLabel == null || IslandOpacitySlider == null) return;
            OpacityLabel.Text = $"{IslandOpacitySlider.Value * 100:F0}%";
            if (Window.GetWindow(this) is MainWindow mw)
                mw.UpdateIslandSettings(IslandEnabledToggle.IsChecked == true, IslandOpacitySlider.Value);
        }

        private void CheckSubscriptionNotifications()
        {
            if (!Session.NotifNewTracksEnabled || Session.UserId <= 0) return;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var followed = SubscriptionService.GetFollowing(Session.UserId);
                    if (followed.Count == 0) return;
                    var since = Session.LastNotificationCheck ?? DateTime.UtcNow.AddDays(-7);
                    Session.LastNotificationCheck = DateTime.UtcNow;
                    var newTracks = followed
                        .SelectMany(a => TrackService.GetPublishedTracks().Where(t => t.ArtistID == a.UserId && t.UploadDate > since))
                        .Take(5).ToList();
                    if (newTracks.Count == 0) return;

                    Dispatcher.Invoke(() =>
                    {
                        if (Session.NotifPushEnabled)
                        {
                            string msg = newTracks.Count == 1
                                ? $"Новый трек: «{newTracks[0].Title}»"
                                : $"{newTracks.Count} новых трека от подписок";
                            if (Window.GetWindow(this) is MainWindow mw) mw.ShowToastNotification("Новые треки", msg);
                        }
                        else
                        {
                            Session.NotificationCount += newTracks.Count;
                            UpdateBadge();
                        }
                    });
                }
                catch { }
            });
        }

        private void UpdateBadge()
        {
            int cnt = Session.NotificationCount;
            if (cnt <= 0) { NotifBadge.Visibility = Visibility.Collapsed; return; }
            NotifBadge.Visibility = Visibility.Visible;
            NotifBadgeText.Text = cnt > 99 ? "99+" : cnt.ToString();
        }

        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;
    }
}
