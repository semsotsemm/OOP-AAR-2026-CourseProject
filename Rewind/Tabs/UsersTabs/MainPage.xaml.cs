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

                // Подписываемся на события плеера для иконки кнопки «Трек дня»
                if (Window.GetWindow(this) is MainWindow mw)
                {
                    mw.PlaybackStateChanged += OnPlaybackStateChanged;

                    // Инициализация настроек до первого открытия панели
                    IslandEnabledToggle.IsChecked = mw.IslandEnabled;
                    IslandOpacitySlider.Value = mw.IslandOpacity;
                    NotifNewTracks.IsChecked = Session.NotifNewTracksEnabled;
                    NotifPush.IsChecked = Session.NotifPushEnabled;
                    NotifNewTracks.Checked   += (_, _) => Session.NotifNewTracksEnabled = true;
                    NotifNewTracks.Unchecked += (_, _) => Session.NotifNewTracksEnabled = false;
                    NotifPush.Checked   += (_, _) => Session.NotifPushEnabled = true;
                    NotifPush.Unchecked += (_, _) => Session.NotifPushEnabled = false;
                    _settingsInitialized = true;
                }

                UpdateBadge();
                // Проверяем новые треки от подписок при каждом открытии страницы
                CheckSubscriptionNotifications();
            };

            Unloaded += (_, _) =>
            {
                if (Window.GetWindow(this) is MainWindow mw2)
                    mw2.PlaybackStateChanged -= OnPlaybackStateChanged;
            };

            OpacityLabel.Text = $"{IslandOpacitySlider.Value * 100:F0}%";

            PlaylistListenService.OnPlaylistListenChanged += OnPlaylistStatsChanged;
            SavedPlaylistService.OnPlaylistSavedChanged += OnPlaylistStatsChanged;

            Unloaded += (_, _) =>
            {
                TrackService.OnPlayCountUpdated -= Global_OnPlayCountUpdated;
                PlaylistListenService.OnPlaylistListenChanged -= OnPlaylistStatsChanged;
                SavedPlaylistService.OnPlaylistSavedChanged -= OnPlaylistStatsChanged;
            };
        }

        private void OnPlaylistStatsChanged(int playlistId)
        {
            Dispatcher.Invoke(LoadPopularPlaylists);
        }

        private void OnPlaybackStateChanged()
        {
            if (_featuredTrack == null) return;
            if (Window.GetWindow(this) is not MainWindow mw) return;

            bool isFeaturedPlaying = mw.CurrentTrack?.TrackId == _featuredTrack.TrackID && mw.IsPlaying;
            FeaturedPlayIconBrush.ImageSource = IconAssets.LoadBitmap(
                isFeaturedPlaying ? "player_pause.png" : "player_play.png");
            FeaturedPlayLabel.Text = isFeaturedPlaying ? " Пауза" : " Слушать";
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

            // Получаем плейлисты из БД (не из пустого кеша в памяти)
            List<Playlist> all;
            try
            {
                var pub = PlaylistService.GetPublicPlaylists(0);
                var own = Session.CachedPlaylists.Where(p => !p.IsPrivate && p.PlaylistID > 0);
                all = pub
                    .Concat(own.Where(o => !pub.Any(p => p.PlaylistID == o.PlaylistID)))
                    .Where(p => p.PlaylistID > 0)
                    .ToList();
            }
            catch { return; }

            if (all.Count == 0) return;

            // Загружаем счётчики из БД для всех плейлистов сразу
            var counts = all.ToDictionary(
                p => p.PlaylistID,
                p =>
                {
                    int s = 0, l = 0;
                    try { s = SavedPlaylistService.GetSavedCount(p.PlaylistID); } catch { }
                    try { l = PlaylistListenService.GetListenerCount(p.PlaylistID); } catch { }
                    return (saves: s, listens: l);
                });

            var top = all
                .OrderByDescending(p => counts[p.PlaylistID].saves + counts[p.PlaylistID].listens)
                .Take(8)
                .ToList();

            foreach (var playlist in top)
            {
                var (saves, listens) = counts[playlist.PlaylistID];

                var card = new AlbumCard
                {
                    AlbumTitle = playlist.Title,
                    Artist = $"♥ {saves}  •  ► {listens}",
                    StartColor = (Color)ColorConverter.ConvertFromString("#11998e"),
                    EndColor = (Color)ColorConverter.ConvertFromString("#38ef7d"),
                    Cursor = Cursors.Hand
                };

                // Обложка: поддерживаем абсолютные и относительные пути
                if (!string.IsNullOrEmpty(playlist.CoverPath))
                {
                    try
                    {
                        string coverPath = playlist.CoverPath.Contains(":")
                            ? playlist.CoverPath
                            : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoversLibrary", playlist.CoverPath);
                        if (System.IO.File.Exists(coverPath))
                            card.CoverImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(coverPath));
                    }
                    catch { card.CoverImageSource = null; }
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

            if (Window.GetWindow(this) is not MainWindow mainWindow) return;

            // Если трек уже играет — переключаем паузу/плей
            if (mainWindow.CurrentTrack?.TrackId == _featuredTrack.TrackID)
            {
                mainWindow.TogglePlayPause();
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

            // Засчитываем прослушивание
            Session.AddListenedTrack(_featuredTrack.TrackID, _featuredTrack.Duration);
            Session.ResetPlaybackScope();

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
            bool opening = NotificationsPanel.Visibility != Visibility.Visible;
            NotificationsPanel.Visibility = opening ? Visibility.Visible : Visibility.Collapsed;

            if (opening && Session.NotificationCount > 0)
            {
                Session.NotificationCount = 0;
                NotifBadge.Visibility = Visibility.Collapsed;
            }
        }

        private void CheckSubscriptionNotifications()
        {
            // Если «Новые треки от авторов» выключено — ничего не делаем
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
                        .SelectMany(a => TrackService.GetPublishedTracks()
                            .Where(t => t.ArtistID == a.UserId && t.UploadDate > since))
                        .Take(5)
                        .ToList();

                    if (newTracks.Count == 0) return;

                    Dispatcher.Invoke(() =>
                    {
                        // Оба переключателя включены — всплывает toast сразу
                        if (Session.NotifPushEnabled)
                        {
                            string msg = newTracks.Count == 1
                                ? $"Новый трек: «{newTracks[0].Title}»"
                                : $"{newTracks.Count} новых трека от подписок";
                            if (Window.GetWindow(this) is MainWindow mw)
                                mw.ShowToastNotification("🎵 Новые треки", msg);
                        }
                        else
                        {
                            // Пуш-уведомления выключены — только бейдж на колокольчике
                            Session.NotificationCount += newTracks.Count;
                            UpdateBadge();
                        }
                    });
                }
                catch { /* игнорируем ошибки СБД */ }
            });
        }

        private void UpdateBadge()
        {
            int cnt = Session.NotificationCount;
            if (cnt <= 0)
            {
                NotifBadge.Visibility = Visibility.Collapsed;
                return;
            }
            NotifBadge.Visibility = Visibility.Visible;
            NotifBadgeText.Text = cnt > 99 ? "99+" : cnt.ToString();
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
