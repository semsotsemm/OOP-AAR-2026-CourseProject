using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Rewind.Helpers;
using System.ComponentModel;
using System.Windows.Controls;

namespace Rewind.Contols
{
    public partial class PlayerBar : UserControl, INotifyPropertyChanged
    {
        public event RoutedEventHandler PlayPauseClicked;
        public event RoutedEventHandler PreviousClicked;
        public event RoutedEventHandler NextClicked;
        public event EventHandler<double> SeekRequested;
        public event EventHandler<double> VolumeChangeRequested;

        public PlayerBar()
        {
            InitializeComponent();
            // Устанавливаем DataContext, чтобы Binding работал проще
            this.DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // --- DEPENDENCY PROPERTIES ---

        public static readonly DependencyProperty TotalSecondsProperty =
            DependencyProperty.Register("TotalSeconds", typeof(double), typeof(PlayerBar),
                new PropertyMetadata(0.0, (d, e) => ((PlayerBar)d).UpdateTextTimes()));

        public static readonly DependencyProperty CurrentSecondsProperty =
            DependencyProperty.Register("CurrentSeconds", typeof(double), typeof(PlayerBar),
                new PropertyMetadata(0.0, (d, e) => ((PlayerBar)d).UpdateTextTimes()));

        public static readonly DependencyProperty CurrentTrackProperty =
            DependencyProperty.Register("CurrentTrack", typeof(string), typeof(PlayerBar),
                new PropertyMetadata("Название трека"));

        public static readonly DependencyProperty CurrentArtistProperty =
            DependencyProperty.Register("CurrentArtist", typeof(string), typeof(PlayerBar),
                new PropertyMetadata("Исполнитель"));

        public static readonly DependencyProperty PlayPauseIconProperty =
            DependencyProperty.Register("PlayPauseIcon", typeof(string), typeof(PlayerBar),
                new PropertyMetadata(IconAssets.GetAbsolutePath("player_pause.png")));

        // --- WRAPPERS ---

        public double TotalSeconds
        {
            get { return (double)GetValue(TotalSecondsProperty); }
            set { SetValue(TotalSecondsProperty, value); }
        }

        public double CurrentSeconds
        {
            get { return (double)GetValue(CurrentSecondsProperty); }
            set { SetValue(CurrentSecondsProperty, value); }
        }

        public string CurrentTrack
        {
            get { return (string)GetValue(CurrentTrackProperty); }
            set { SetValue(CurrentTrackProperty, value); }
        }

        public string CurrentArtist
        {
            get { return (string)GetValue(CurrentArtistProperty); }
            set { SetValue(CurrentArtistProperty, value); }
        }

        public string PlayPauseIcon
        {
            get { return (string)GetValue(PlayPauseIconProperty); }
            set { SetValue(PlayPauseIconProperty, value); }
        }

        // --- ВЫЧИСЛЯЕМЫЕ СВОЙСТВА ДЛЯ ТЕКСТА ---

        public string CurrentTimeStr => TimeSpan.FromSeconds(CurrentSeconds).ToString(@"mm\:ss");
        public string TotalTimeStr => TimeSpan.FromSeconds(TotalSeconds).ToString(@"mm\:ss");
        public bool IsUserDragging { get; private set; } = false;
        private void MusicSlider_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IsUserDragging = true;
        }

        private void MusicSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IsUserDragging = false;
            SeekRequested?.Invoke(this, CurrentSeconds);
        }
        private void UpdateTextTimes()
        {
            OnPropertyChanged(nameof(CurrentTimeStr));
            OnPropertyChanged(nameof(TotalTimeStr));
        }

        // --- МЕТОДЫ ---

        public void ShowPlayer()
        {
            this.Visibility = Visibility.Visible;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Session._isPlaing = !Session._isPlaing;
            PlayPauseClicked?.Invoke(this, e);
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            PreviousClicked?.Invoke(this, e);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NextClicked?.Invoke(this, e);
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded) VolumeChangeRequested?.Invoke(this, e.NewValue);
        }

        /// <summary>Клик по обложке или названию — открывает страницу «Сейчас играет».</summary>
        private void PlayerInfo_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (Application.Current?.MainWindow is Rewind.MainWindow mw)
                mw.OpenNowPlaying("Плеер");
        }

        /// <summary>Клик по имени исполнителя — открывает его страницу.</summary>
        private void ArtistName_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
            string artistName = CurrentArtist;
            if (string.IsNullOrWhiteSpace(artistName)) return;
            if (Application.Current?.MainWindow is Rewind.MainWindow mw)
                mw.OpenArtistProfileByName(artistName);
        }

        private void LikeBtn_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Активный трек возьмём через логику MainWindow
            if (Application.Current?.MainWindow is Rewind.MainWindow mw)
            {
                var track = mw.CurrentTrack;
                if (track == null) return;
                bool liked = Session.ToggleLike(track.TrackId);
                LikeIconBrush.ImageSource = IconAssets.LoadBitmap(
                    liked ? "like_filled.png" : "like_outline.png");
            }
        }

        /// <summary>Обновляет миниатюрную обложку в плеере.</summary>
        public void UpdateCover(string? coverPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(coverPath))
                {
                    CoverImage.Source = null;
                    CoverPlaceholder.Visibility = Visibility.Visible;
                    return;
                }

                string fp = FileStorage.ResolveImagePath(coverPath);

                if (!File.Exists(fp))
                {
                    CoverImage.Source = null;
                    CoverPlaceholder.Visibility = Visibility.Visible;
                    return;
                }

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(fp);
                bmp.DecodePixelWidth = 88;
                bmp.EndInit();
                bmp.Freeze();

                CoverImage.Source = bmp;
                CoverPlaceholder.Visibility = Visibility.Collapsed;
            }
            catch
            {
                CoverImage.Source = null;
                CoverPlaceholder.Visibility = Visibility.Visible;
            }
        }

        /// <summary>Обновляет состояние лайк в плеере.</summary>
        public void UpdateLikeIcon(int trackId)
        {
            LikeIconBrush.ImageSource = IconAssets.LoadBitmap(
                Session.IsLiked(trackId) ? "like_filled.png" : "like_outline.png");
        }
    }
}
