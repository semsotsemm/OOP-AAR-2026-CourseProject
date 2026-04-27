using System.Windows;
using System.Windows.Controls;

namespace Rewind.Contols
{
    public partial class PlaylistButton : UserControl
    {
        public PlaylistButton()
        {
            InitializeComponent();
        }

        // Свойство для заголовка (Chill Vibes)
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(PlaylistButton), new PropertyMetadata("Название плейлиста"));

        // Свойство для подзаголовка (24 трека)
        public string Subtitle
        {
            get { return (string)GetValue(SubtitleProperty); }
            set { SetValue(SubtitleProperty, value); }
        }
        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register("Subtitle", typeof(string), typeof(PlaylistButton), new PropertyMetadata("0 треков"));

        // Свойство для иконки (эмодзи или символ)
        public string Icon
        {
            get { return (string)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(string), typeof(PlaylistButton), new PropertyMetadata("🎵"));
    }
}