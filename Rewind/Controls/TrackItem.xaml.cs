using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rewind
{
    public partial class TrackItem : UserControl
    {
        public string FilePath { get; private set; }
        public string TrackName { get; private set; }
        public string ArtistName { get; private set; }
        public double DurationSeconds { get; private set; }

        private bool _isLiked = false;

        public TrackItem(string title, string artist, string duration, string path, double durationSeconds = 0)
        {
            InitializeComponent();
            TrackName = title;
            ArtistName = artist;
            FilePath = path;
            DurationSeconds = durationSeconds;

            TrackTitleText.Text = title;
            ArtistNameText.Text = artist;
            DurationText.Text = duration;
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            var main = Application.Current.MainWindow as MainWindow;
            if (main == null) return;

            main.PlayMusic(FilePath, TrackName, ArtistName, DurationSeconds);
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
