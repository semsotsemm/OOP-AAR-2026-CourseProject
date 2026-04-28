using Rewind.Contols;
using Rewind.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace Rewind.Tabs.UsersTabs
{
    public partial class SearchPage : UserControl
    {
        private readonly List<TrackItem> _trackItems = new();

        public SearchPage()
        {
            InitializeComponent();
            Render(string.Empty);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => Render(SearchBox.Text.Trim().ToLower());

        private void Render(string query)
        {
            ResultsContainer.Children.Clear();
            _trackItems.Clear();

            var tracks = TrackService.GetAllTracks()
                .Where(t => string.IsNullOrEmpty(query)
                            || t.Title.ToLower().Contains(query)
                            || t.Artist.Nickname.ToLower().Contains(query))
                .Take(30)
                .ToList();

            foreach (var track in tracks)
            {
                var fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", track.FilePath);
                var item = new TrackItem(track.TrackID, track.Title, track.Artist.Nickname, FormatDuration(track.Duration), fullPath, track.CoverPath, track.Duration);
                _trackItems.Add(item);
                ResultsContainer.Children.Add(item);
            }
        }

        private static string FormatDuration(int sec) => $"{sec / 60}:{sec % 60:D2}";

        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;
    }
}
