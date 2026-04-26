using System.Windows;
using System.Windows.Controls;

namespace Rewind
{
    public partial class TrackItem : UserControl
    {
        public string FilePath { get; private set; }

        public TrackItem(string title, string artist, string duration, string path)
        {
            InitializeComponent();
            TrackTitle.Text = title;
            ArtistName.Text = artist;
            DurationText.Text = duration;
            FilePath = path;
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}