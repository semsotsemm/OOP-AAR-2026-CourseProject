using Rewind.Controls;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rewind.Helpers;
using Rewind.MVVM.Services;
using Rewind.Pages;
using Rewind.Tabs.UsersTabs;

namespace Rewind.Contols
{
    public partial class TrackItem : UserControl
    {
        public int TrackId { get; private set; }

        public event MouseButtonEventHandler TrackSelected;
        public string FilePath { get; private set; }
        public string? CoverPath { get; private set; }
        public string TrackName { get; private set; }
        public string ArtistName { get; private set; }
        public double DurationSeconds { get; private set; }

        public Playlist? PlaylistContext { get; set; }

        public event RoutedEventHandler PlayClicked;

        private MainWindow? _playerHost;
        private void OnItemClick(object sender, MouseButtonEventArgs e)
        {
            TrackSelected?.Invoke(this, e);
        }
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

                TrackService.OnPlayCountUpdated += OnTrackPlayCountUpdated;
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

            SetPlayPauseIcon(isThisTrackPlaying);

            SetPlaying(_playerHost.CurrentTrack?.TrackId == TrackId);
        }
        private void UpdateCover(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string fullPath = FileStorage.ResolveImagePath(path);

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
            }
        }



        private void OnTrackPlayCountUpdated(int trackId, int newCount)
        {
            if (trackId != TrackId) return;
        }

        public void SetPlayPauseIcon(bool isPlaying)
        {
            if (PlayBtn.Template.FindName("PlayIcon", PlayBtn) is Image playIconInside)
            {
                string iconName = isPlaying ? "player_pause.png" : "player_play.png";
                playIconInside.Source = IconAssets.LoadBitmap(iconName);
            }
        }

        private void ArtistName_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (string.IsNullOrWhiteSpace(ArtistName)) return;
            if (Window.GetWindow(this) is MainWindow mw)
                mw.OpenArtistProfileByName(ArtistName);
        }


        private void Play_Click(object sender, RoutedEventArgs e)
        {
            PlayClicked?.Invoke(this, new RoutedEventArgs());

            DependencyObject? par = VisualTreeHelper.GetParent(this);
            while (par != null && par is not MainPage && par is not FavoritesPage
                                && par is not PlaylistDetailsPage && par is not Tabs.UsersTabs.AlbumDetailsPage
                                && par is not SearchPage && par is not Tabs.UsersTabs.ArtistProfilePage)
                par = VisualTreeHelper.GetParent(par);

            if (Window.GetWindow(this) is not MainWindow mainWindow) return;

            string sourcePage = par switch
            {
                FavoritesPage        => "Любимые",
                PlaylistDetailsPage  => "Плейлист",
                Tabs.UsersTabs.AlbumDetailsPage => "Альбом",
                SearchPage           => "Поиск",
                Tabs.UsersTabs.ArtistProfilePage => "Исполнитель",
                _                    => "Главная"
            };

            if (mainWindow.CurrentTrack?.TrackId == TrackId)
            {
                mainWindow.TogglePlayPause();
            }
            else
            {
                Session.AddListenedTrack(TrackId, DurationSeconds);

                if (par is MainPage mp)
                {
                    Session.ResetPlaybackScope();
                    mainWindow.PlayTrackFromContext(this, mp.GetTrackItems());
                }
                else if (par is FavoritesPage fp)
                {
                    Session.ResetPlaybackScope();
                    mainWindow.PlayTrackFromContext(this, fp.GetTrackItems());
                }
                else if (par is PlaylistDetailsPage pp)
                {
                    pp.RegisterPlaylistListen();
                    mainWindow.PlayTrackFromContext(this, pp.GetTrackItems());
                }
                else if (par is Tabs.UsersTabs.AlbumDetailsPage ad)
                {
                    ad.RegisterAlbumListen();
                    mainWindow.PlayTrackFromContext(this, ad.GetTrackItems());
                }
                else if (par is SearchPage sp)
                {
                    Session.ResetPlaybackScope();
                    mainWindow.PlayTrackFromContext(this, sp.GetTrackItems());
                }
                else if (par is Tabs.UsersTabs.ArtistProfilePage ap)
                {
                    Session.ResetPlaybackScope();
                    mainWindow.PlayTrackFromContext(this, ap.GetTrackItems());
                }
                else
                {
                    Session.ResetPlaybackScope();
                    mainWindow.PlayTrackFromContext(this, new List<TrackItem> { this });
                }
            }

        }
        private void LikeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            bool isNowLiked = Session.ToggleLike(TrackId);

            RefreshLikeIcon(isNowLiked);

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

            var playNextItem = new MenuItem { Header = "⇥  Играть следующим" };
            playNextItem.Click += (_, _) => PlayNext_Click(this, null!);
            ContextMenu.Items.Add(playNextItem);

            ContextMenu.Items.Add(new Separator());

            if (PlaylistContext != null && PlaylistContext.OwnerID == Session.UserId)
            {
                var removeItem = new MenuItem
                {
                    Header = $"✕  Удалить из «{PlaylistContext.Title}»"
                };
                removeItem.Click += RemoveFromPlaylist_Click;
                ContextMenu.Items.Add(removeItem);
                ContextMenu.Items.Add(new Separator());
            }

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
                            : $"Подписаться на {artistName}"
                    };
                    subItem.Click += (_, _) => ToggleSubscription(capturedArtistId, capturedName, capturedFollowing);
                    ContextMenu.Items.Add(subItem);
                    ContextMenu.Items.Add(new Separator());
                }
            }
            catch { 
            }

            var reportItem = new MenuItem { Header = "Пожаловаться на трек" };
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
                    ServiceLocator.TryResolve<IDialogService>()?.Info($"Вы отписались от {artistName}.");
                }
                else
                {
                    SubscriptionService.Subscribe(Session.UserId, artistId);

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

                    ServiceLocator.TryResolve<IDialogService>()?.Info(
                        $"Вы подписались на {artistName}! Вы будете получать уведомления о новых треках.");
                }
            }
            catch (Exception ex)
            {
                ServiceLocator.TryResolve<IDialogService>()?.Error($"Ошибка подписки: {ex.Message}");
            }
        }

        private void RemoveFromPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistContext == null) return;
            if (PlaylistContext.OwnerID != Session.UserId) return;

            var dialog = ServiceLocator.TryResolve<IDialogService>();
            if (dialog != null && !dialog.Confirm(
                $"Удалить трек «{TrackName}» из плейлиста «{PlaylistContext.Title}»?")) return;

            bool removed = Session.RemoveTrackFromPlaylist(PlaylistContext, TrackId);
            if (!removed)
                dialog?.Error("Не удалось удалить трек из плейлиста.");
        }

        private void AddTrackToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item || item.Tag is not Playlist playlist) return;

            var dialog = ServiceLocator.TryResolve<IDialogService>();
            bool added = Session.AddTrackToPlaylist(playlist, TrackId);
            if (!added) { dialog?.Info("Трек уже есть в этом плейлисте."); return; }
            dialog?.Info($"Трек добавлен в плейлист \"{playlist.Title}\".");
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

            Play_Click(PlayBtn, new RoutedEventArgs());
            e.Handled = true;
        }

        public void SetPlaying(bool isPlaying)
        {
            TrackTitleText.Foreground = isPlaying
                ? (SolidColorBrush)Application.Current.Resources["AccentColor"]
                : (SolidColorBrush)Application.Current.Resources["TextPrimary"];
        }
    }
}
