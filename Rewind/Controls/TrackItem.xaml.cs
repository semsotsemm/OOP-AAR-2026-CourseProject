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

            TrackTitleText.Text = title;
            ArtistNameText.Text = artist;
            DurationText.Text = duration;

            // Синхронизируем иконку лайка с кешем сессии
            RefreshLikeIcon();

            UpdateCover(coverPath);
            BuildPlaylistContextMenu();
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
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);

            while (parent != null && parent is not MainPage && parent is not FavoritesPage && parent is not PlaylistDetailsPage && parent is not SearchPage)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (Window.GetWindow(this) is not MainWindow mainWindow) return;

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
            LikeBtn.Text = liked ? "♥" : "♡";
            LikeBtn.Foreground = liked
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2AE876"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DEDEDA"));
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

            var ownPlaylists = Session.CachedPlaylists.Where(p => p.OwnerID == Session.UserId).ToList();
            if (ownPlaylists.Count == 0)
            {
                ContextMenu.Items.Add(new MenuItem
                {
                    Header = "Нет плейлистов",
                    IsEnabled = false
                });
                return;
            }

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
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2AE876"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A18"));
        }
    }
}
