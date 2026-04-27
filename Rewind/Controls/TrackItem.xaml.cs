using Rewind.Controls;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rewind.Helpers;
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
                    TrackCoverBrush.ImageSource = new BitmapImage(new Uri(fullPath));
                    if (CoverPlaceholder != null)
                        CoverPlaceholder.Visibility = Visibility.Collapsed;
                }
            }
            catch { /* оставляем ноту-заглушку */ }
        }

        // ─────────────────────────────────────────────
        //  Воспроизведение
        // ─────────────────────────────────────────────
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);

            // Ищем родителя, пока не упремся в потолок или не найдем одну из двух страниц
            while (parent != null && parent is not MainPage && parent is not FavoritesPage)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            // Если нашли главную — вызываем у неё
            if (parent is MainPage mainPage)
            {
                mainPage.PlayMusic(FilePath, TrackName, ArtistName, DurationSeconds);
            }
            // Если нашли страницу лайков — вызываем у неё
            else if (parent is FavoritesPage favPage)
            {
                favPage.PlayMusic(FilePath, TrackName, ArtistName, DurationSeconds);
            }
        }
        // ─────────────────────────────────────────────
        //  Лайк — работает через Session-кеш
        // ─────────────────────────────────────────────
        private void LikeBtn_Click(object sender, MouseButtonEventArgs e)
        {
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
