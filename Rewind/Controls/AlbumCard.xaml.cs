using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rewind
{
    public partial class AlbumCard : UserControl
    {
        public AlbumCard()
        {
            InitializeComponent();
        }

        // Название альбома
        public string AlbumTitle
        {
            get { return (string)GetValue(AlbumTitleProperty); }
            set { SetValue(AlbumTitleProperty, value); }
        }
        public static readonly DependencyProperty AlbumTitleProperty =
            DependencyProperty.Register("AlbumTitle", typeof(string), typeof(AlbumCard), new PropertyMetadata("Название альбома"));

        // Исполнитель
        public string Artist
        {
            get { return (string)GetValue(ArtistProperty); }
            set { SetValue(ArtistProperty, value); }
        }
        public static readonly DependencyProperty ArtistProperty =
            DependencyProperty.Register("Artist", typeof(string), typeof(AlbumCard), new PropertyMetadata("Исполнитель"));

        // Начальный цвет градиента
        public Color StartColor
        {
            get { return (Color)GetValue(StartColorProperty); }
            set { SetValue(StartColorProperty, value); }
        }
        public static readonly DependencyProperty StartColorProperty =
            DependencyProperty.Register("StartColor", typeof(Color), typeof(AlbumCard), new PropertyMetadata(Color.FromRgb(102, 126, 234)));

        // Конечный цвет градиента
        public Color EndColor
        {
            get { return (Color)GetValue(EndColorProperty); }
            set { SetValue(EndColorProperty, value); }
        }
        public static readonly DependencyProperty EndColorProperty =
            DependencyProperty.Register("EndColor", typeof(Color), typeof(AlbumCard), new PropertyMetadata(Color.FromRgb(118, 75, 162)));
    }
}