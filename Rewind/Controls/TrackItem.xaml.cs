using Rewind.Controls;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rewind.Helpers;
using Rewind.Pages;
using Rewind.Tabs.UsersTabs;

namespace Rewind.Contols
{
    public partial class TrackItem : UserControl
    {
        // ─────────────────────────────────────────────
        //  Свойства
        // ─────────────────────────────────────────────
        public int TrackId { get; private set; }

        public event MouseButtonEventHandler TrackSelected;
        public string FilePath { get; private set; }
        public string? CoverPath { get; private set; }
        public string TrackName { get; private set; }
        public string ArtistName { get; private set; }
        public double DurationSeconds { get; private set; }

        public event RoutedEventHandler PlayClicked;

        private MainWindow? _playerHost;
        private void OnItemClick(object sender, MouseButtonEventArgs e)
        {
            TrackSelected?.Invoke(this, e);
        }
        // ─────────────────────────────────────────────
        //  Конструктор
        // ─────────────────────────────────────────────
        public TrackItem(int trackId, string title, string artist,
                         string duration, string path,
                         string? coverPath, double durationSeconds = 0)
        {
            InitializeComponent();

            TrackId = trackId;
            TrackName = title;
            ArtistName = artist;
            FilePath = path;
            CoverPath = coverPath;
            DurationSeconds = durationSeconds;
            Session._isPlaing = false;

            TrackTitleText.Text = title;
            ArtistNameText.Text = artist;
            DurationText.Text = duration;

            // Синхронизируем иконку лайка с кешем сессии
            RefreshLikeIcon();

            UpdateCover(coverPath);
            BuildPlaylistContextMenu();

            Loaded += (_, _) =>
            {
                if (Window.GetWindow(this) is MainWindow mw)
                {
                    _playerHost = mw;
                    mw.PlaybackStateChanged += OnPlaybackStateChanged;
                }

                // Подписываемся на реальное время
                TrackService.OnPlayCountUpdated += OnTrackPlayCountUpdated;
                // Загружаем начальное значение из БД
                try
                {
                    var stats = StatisticService.GetStatsByTrack(TrackId);
                    PlayCountText.Text = FormatPlayCount(stats?.PlayCount ?? 0);
                }
                catch { PlayCountText.Text = ""; }
            };

            Unloaded += (_, _) =>
            {
                if (_playerHost != null)
                    _playerHost.PlaybackStateChanged -= OnPlaybackStateChanged;
                TrackService.OnPlayCountUpdated -= OnTrackPlayCountUpdated;
            };
        }

        private void OnPlaybackStateChanged()
        {
            if (_playerHost == null) return;

            bool isThisTrackPlaying =
                _playerHost.CurrentTrack?.TrackId == TrackId &&
                _playerHost.IsPlaying;

            // Обновляем иконку play/pause на кнопке
            SetPlayPauseIcon(isThisTrackPlaying);

            // Подсветка названия
            SetPlaying(_playerHost.CurrentTrack?.TrackId == TrackId);
        }
        // ─────────────────────────────────────────────
        //  Обложка
        // ─────────────────────────────────────────────
        private void UpdateCover(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string fullPath = path.Contains(":")
                    ? path
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoversLibrary", path);

                if (File.Exists(fullPath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fullPath);

                    bitmap.DecodePixelWidth = 100;

                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    bitmap.Freeze();

                    RenderOptions.SetBitmapScalingMode(TrackCoverBrush, BitmapScalingMode.Fant);

                    TrackCoverBrush.ImageSource = bitmap;

                    if (CoverPlaceholder != null)
                        CoverPlaceholder.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                /* оставляем ноту-заглушку */
            }
        }


        // ─────────────────────────────────────────────
        //  Воспроизведение
        // ─────────────────────────────────────────────

        // Реальное время: обновляем счётчик прослушиваний при любом воспроизведении
        private void OnTrackPlayCountUpdated(int trackId, int newCount)
        {
            if (trackId != TrackId) return;
            Dispatcher.Invoke(() => PlayCountText.Text = FormatPlayCount(newCount));
        }

        private static string FormatPlayCount(int count)
        {
            if (count <= 0)   return "";
            if (count >= 1_000_000) return $"► {count / 1_000_000.0:F1}M";
            if (count >= 1_000)     return $"► {count / 1000.0:F1}K";
            return $"► {count}";
        }

        public void SetPlayPauseIcon(bool isPlaying)
        {
            if (PlayBtn.Template.FindName("PlayIcon", PlayBtn) is Image playIconInside)
            {
                string iconName = isPlaying ? "player_pause.png" : "player_play.png";
                playIconInside.Source = IconAssets.LoadBitmap(iconName);
            }
        }

        // Переход на страницу исполнителя по клику на никнейм
        private void ArtistName_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;   // не проваливаемся до TrackCard_MouseLeftButtonDown
            if (string.IsNullOrWhiteSpace(ArtistName)) return;
            if (Window.GetWindow(this) is MainWindow mw)
                mw.OpenArtistProfileByName(ArtistName);
        }


        private void Play_Click(object sender, RoutedEventArgs e)
        {
            PlayClicked?.Invoke(this, new RoutedEventArgs());

            Session.AddListenedTrack(TrackId, DurationSeconds);
            DependencyObject parent = VisualTreeHelper.GetParent(this);

            while (parent != null && parent is not MainPage && parent is not FavoritesPage && parent is not PlaylistDetailsPage && parent is not SearchPage)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (Window.GetWindow(this) is not MainWindow mainWindow) return;
            if (mainWindow.CurrentTrack?.TrackId == TrackId)
            {
                mainWindow.TogglePlayPause();
                return;
            }

            if (parent is MainPage mainPage)
            {
                mainWindow.PlayTrackFromContext(this, mainPage.GetTrackItems());
            }
            else if (parent is FavoritesPage favPage)
            {
                mainWindow.PlayTrackFromContext(this, favPage.GetTrackItems());
            }
            else if (parent is PlaylistDetailsPage playlistPage)
            {
                mainWindow.PlayTrackFromContext(this, playlistPage.GetTrackItems());
            }
            else if (parent is SearchPage searchPage)
            {
                mainWindow.PlayTrackFromContext(this, searchPage.GetTrackItems());
            }
        }
        // ─────────────────────────────────────────────
        //  Лайк — работает через Session-кеш
        // ─────────────────────────────────────────────
        private void LikeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            // Переключаем состояние в кеше сессии
            bool isNowLiked = Session.ToggleLike(TrackId);

            // Обновляем иконку
            RefreshLikeIcon(isNowLiked);

            // Небольшая анимация — быстрый scale через RenderTransform
            AnimateLikeButton();
        }

        private void RefreshLikeIcon() => RefreshLikeIcon(Session.IsLiked(TrackId));

        private void RefreshLikeIcon(bool liked)
        {
            LikeIcon.ImageSource = IconAssets.LoadBitmap(liked ? "like_filled.png" : "like_outline.png");
        }

        private void AnimateLikeButton()
        {
            var scale = new System.Windows.Media.ScaleTransform(1, 1);
            LikeBtn.RenderTransform = scale;
            LikeBtn.RenderTransformOrigin = new Point(0.5, 0.5);

            var anim = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames();
            anim.KeyFrames.Add(new System.Windows.Media.Animation.EasingDoubleKeyFrame(1.4,
                System.Windows.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100))));
            anim.KeyFrames.Add(new System.Windows.Media.Animation.EasingDoubleKeyFrame(1.0,
                System.Windows.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));

            scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, anim);
            scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, anim);
        }

        private void BuildPlaylistContextMenu()
        {
            ContextMenu = new ContextMenu();
            ContextMenu.Opened += (_, _) => RebuildPlaylistMenu();
            RebuildPlaylistMenu();
        }

        private void RebuildPlaylistMenu()
        {
            if (ContextMenu == null) return;
            ContextMenu.Items.Clear();

            // ―― Очередь ――
            var playNextItem = new MenuItem { Header = "⇥  Играть следующим" };
            playNextItem.Click += (_, _) => PlayNext_Click(this, null!);
            ContextMenu.Items.Add(playNextItem);

            ContextMenu.Items.Add(new Separator());

            var ownPlaylists = Session.CachedPlaylists.Where(p => p.OwnerID == Session.UserId).ToList();
            if (ownPlaylists.Count == 0)
            {
                ContextMenu.Items.Add(new MenuItem { Header = "Нет плейлистов", IsEnabled = false });
            }
            else
            {
                foreach (var playlist in ownPlaylists)
                {
                    var menuItem = new MenuItem
                    {
                        Header = $"Добавить в: {playlist.Title}",
                        Tag = playlist
                    };
                    menuItem.Click += AddTrackToPlaylist_Click;
                    ContextMenu.Items.Add(menuItem);
                }
            }

            ContextMenu.Items.Add(new Separator());

            // Subscribe / Unsubscribe item — resolved at menu-open time
            try
            {
                var track = TrackService.GetTrackById(TrackId);
                if (track != null && track.ArtistID != Session.UserId)
                {
                    bool isFollowing = SubscriptionService.IsFollowing(Session.UserId, track.ArtistID);
                    string artistName = UserService.GetUserById(track.ArtistID)?.Nickname ?? ArtistName;
                    int capturedArtistId = track.ArtistID;
                    string capturedName = artistName;
                    bool capturedFollowing = isFollowing;

                    var subItem = new MenuItem
                    {
                        Header = isFollowing
                            ? $"✓ Отписаться от {artistName}"
                            : $"⭐ Подписаться на {artistName}"
                    };
                    subItem.Click += (_, _) => ToggleSubscription(capturedArtistId, capturedName, capturedFollowing);
                    ContextMenu.Items.Add(subItem);
                    ContextMenu.Items.Add(new Separator());
                }
            }
            catch { /* ignore DB errors during menu open */ }

            var reportItem = new MenuItem { Header = "⚠  Пожаловаться на трек" };
            reportItem.Click += ReportTrack_Click;
            ContextMenu.Items.Add(reportItem);
        }

        private void PlayNext_Click(object sender, MouseButtonEventArgs e)
        {
            if (e != null) e.Handled = true;
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.PlayNext(this);
        }

        private void ReportTrack_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ReportTrackDialog(TrackId, TrackName);
            dialog.ShowDialog();
        }

        private void ToggleSubscription(int artistId, string artistName, bool wasFollowing)
        {
            try
            {
                if (wasFollowing)
                {
                    SubscriptionService.Unsubscribe(Session.UserId, artistId);
                    MessageBox.Show($"Вы отписались от {artistName}.", "Rewind",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    SubscriptionService.Subscribe(Session.UserId, artistId);

                    // Notify if settings allow
                    if (Session.NotifNewTracksEnabled)
                    {
                        var recent = TrackService.GetPublishedTracks()
                            .Where(t => t.ArtistID == artistId)
                            .OrderByDescending(t => t.UploadDate)
                            .FirstOrDefault();

                        if (Session.NotifPushEnabled && recent != null
                            && Window.GetWindow(this) is MainWindow mw)
                        {
                            mw.ShowToastNotification(
                                $"Подписка на {artistName}!",
                                $"Последний трек: «{recent.Title}»");
                        }
                        else if (!Session.NotifPushEnabled)
                        {
                            Session.NotificationCount++;
                        }
                    }

                    MessageBox.Show($"Вы подписались на {artistName}! Вы будете получать уведомления о новых треках.",
                        "Rewind", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подписки: {ex.Message}", "Rewind",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTrackToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item || item.Tag is not Playlist playlist) return;

            bool added = Session.AddTrackToPlaylist(playlist, TrackId);
            if (!added)
            {
                MessageBox.Show("Трек уже есть в этом плейлисте.", "Rewind", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show($"Трек добавлен в плейлист \"{playlist.Title}\".", "Rewind", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddToPlaylist_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (ContextMenu == null) return;
            RebuildPlaylistMenu();
            ContextMenu.PlacementTarget = sender as FrameworkElement ?? this;
            ContextMenu.IsOpen = true;
        }

        private void TrackCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var origin = e.OriginalSource as DependencyObject;
            while (origin != null)
            {
                if (origin is Button || origin is MenuItem || origin is ContextMenu || origin is Slider)
                    return;
                origin = VisualTreeHelper.GetParent(origin);
            }

            if (Window.GetWindow(this) is not MainWindow mainWindow) return;

            string sourcePage = "Главная";
            DependencyObject parent = VisualTreeHelper.GetParent(this);
            while (parent != null && parent is not MainPage && parent is not FavoritesPage && parent is not PlaylistDetailsPage && parent is not SearchPage)
                parent = VisualTreeHelper.GetParent(parent);

            if (parent is FavoritesPage) sourcePage = "Любимые";
            else if (parent is PlaylistDetailsPage) sourcePage = "Плейлист";
            else if (parent is SearchPage) sourcePage = "Поиск";

            if (mainWindow.CurrentTrack == null || mainWindow.CurrentTrack.TrackId != TrackId)
            {
                Play_Click(PlayBtn, new RoutedEventArgs());
            }

            mainWindow.OpenNowPlaying(sourcePage);
            e.Handled = true;
        }

        // ─────────────────────────────────────────────
        //  Подсветка при воспроизведении
        // ─────────────────────────────────────────────
        public void SetPlaying(bool isPlaying)
        {
            TrackTitleText.Foreground = isPlaying
                ? (SolidColorBrush)Application.Current.Resources["AccentColor"]
                : (SolidColorBrush)Application.Current.Resources["TextPrimary"];
        }
    }
}
