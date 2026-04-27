using Rewind.Controls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rewind
{
    public partial class TrackItem : UserControl
    {
        public string FilePath { get; private set; }
        public string? CoverPath { get; private set; }
        public string TrackName { get; private set; }
        public string ArtistName { get; private set; }
        public double DurationSeconds { get; private set; }

        private bool _isLiked = false;

        public TrackItem(string title, string artist, string duration, string path, string? coverPath, double durationSeconds = 0)
        {
            InitializeComponent();
            TrackName = title;
            ArtistName = artist;
            FilePath = path;
            CoverPath = coverPath; 
            DurationSeconds = durationSeconds;

            TrackTitleText.Text = title;
            ArtistNameText.Text = artist;
            DurationText.Text = duration;

            UpdateCover(coverPath);
        }
        private void UpdateCover(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                // Если путь относительный (только имя файла), собираем полный путь
                string fullPath = path.Contains(":")
                    ? path
                    : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoversLibrary", path);

                if (File.Exists(fullPath))
                {
                    // Находим наш ImageBrush в XAML и меняем его источник
                    TrackCoverBrush.ImageSource = new BitmapImage(new Uri(fullPath));

                    // Если есть текст-нота, скрываем её
                    if (CoverPlaceholder != null) CoverPlaceholder.Visibility = Visibility.Collapsed;
                }
            }
            catch { /* Ошибка загрузки картинки — останется стандартная нота */ }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is MainPage))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent is MainPage mainPage)
            {
                mainPage.PlayMusic(FilePath, TrackName, ArtistName, DurationSeconds);
            }
        }

        private void LikeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            _isLiked = !_isLiked;
            if (_isLiked)
            {
                LikeBtn.Text = "♥";
                LikeBtn.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#2AE876"));
                Session.Liked++;
            }
            else
            {
                LikeBtn.Text = "♡";
                LikeBtn.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#DEDEDA"));
                if (Session.Liked > 0) Session.Liked--;
            }
        }

        // Вызывается из MainWindow когда этот трек начал играть
        public void SetPlaying(bool isPlaying)
        {
            if (isPlaying)
            {
                // Подсвечиваем активный трек
                TrackTitleText.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#2AE876"));
            }
            else
            {
                TrackTitleText.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#1A1A18"));
            }
        }
    }
}
