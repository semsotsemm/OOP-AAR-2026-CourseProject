using System.Windows;
using System.Windows.Controls;

namespace OOTP_AAR_2026_CourseProject
{
    public partial class PlayerBar : UserControl
    {
        public PlayerBar()
        {
            InitializeComponent();
        }

        public string CurrentTrack
        {
            get { return (string)GetValue(CurrentTrackProperty); }
            set { SetValue(CurrentTrackProperty, value); }
        }
        public static readonly DependencyProperty CurrentTrackProperty =
            DependencyProperty.Register("CurrentTrack", typeof(string), typeof(PlayerBar), new PropertyMetadata("Название трека"));

        public string CurrentArtist
        {
            get { return (string)GetValue(CurrentArtistProperty); }
            set { SetValue(CurrentArtistProperty, value); }
        }
        public static readonly DependencyProperty CurrentArtistProperty =
            DependencyProperty.Register("CurrentArtist", typeof(string), typeof(PlayerBar), new PropertyMetadata("Исполнитель"));

        public string PlayPauseIcon
        {
            get { return (string)GetValue(PlayPauseIconProperty); }
            set { SetValue(PlayPauseIconProperty, value); }
        }
        public static readonly DependencyProperty PlayPauseIconProperty =
            DependencyProperty.Register("PlayPauseIcon", typeof(string), typeof(PlayerBar), new PropertyMetadata("⏸"));

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            // Здесь можно добавить логику смены иконки
            PlayPauseIcon = PlayPauseIcon == "▶" ? "⏸" : "▶";
        }
    }
}