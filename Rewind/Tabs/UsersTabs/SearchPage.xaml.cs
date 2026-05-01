using Rewind.Contols;
using Rewind.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rewind.Tabs.UsersTabs
{
    public partial class SearchPage : UserControl
    {
        private readonly List<TrackItem> _trackItems = new();
        private string _sortMode = "title"; // title / artist / duration

        public SearchPage()
        {
            InitializeComponent();
            Render(string.Empty);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => Render(SearchBox.Text.Trim().ToLower());

        private void SortTitle_Click(object sender, MouseButtonEventArgs e) { _sortMode = "title"; UpdateSortUI(); Render(SearchBox.Text.Trim().ToLower()); }
        private void SortArtist_Click(object sender, MouseButtonEventArgs e) { _sortMode = "artist"; UpdateSortUI(); Render(SearchBox.Text.Trim().ToLower()); }
        private void SortDuration_Click(object sender, MouseButtonEventArgs e) { _sortMode = "duration"; UpdateSortUI(); Render(SearchBox.Text.Trim().ToLower()); }

        private void UpdateSortUI()
        {
            var dark = new SolidColorBrush(Color.FromRgb(26, 26, 24));
            var card = (Application.Current.TryFindResource("BgCard") as Brush) ?? new SolidColorBrush(Colors.White);
            var border = (Application.Current.TryFindResource("BorderColor") as Brush) ?? new SolidColorBrush(Color.FromRgb(235, 235, 231));
            var sec = (Application.Current.TryFindResource("TextSecondary") as Brush) ?? new SolidColorBrush(Color.FromRgb(136, 136, 128));

            var map = new[] { (SortTitle, "title"), (SortArtist, "artist"), (SortDuration, "duration") };
            foreach (var (b, key) in map)
            {
                bool a = _sortMode == key;
                b.Background = a ? dark : card;
                b.BorderBrush = a ? System.Windows.Media.Brushes.Transparent : border;
                b.BorderThickness = a ? new Thickness(0) : new Thickness(1.5);
                if (b.Child is TextBlock tb) tb.Foreground = a ? System.Windows.Media.Brushes.White : sec;
            }
        }

        private void Render(string query)
        {
            ResultsContainer.Children.Clear();
            _trackItems.Clear();

            var tracks = TrackService.GetPublishedTracks()
                .Where(t => string.IsNullOrEmpty(query)
                            || t.Title.ToLower().Contains(query)
                            || (t.Artist?.Nickname?.ToLower().Contains(query) ?? false))
                .ToList();

            tracks = _sortMode switch
            {
                "artist" => tracks.OrderBy(t => t.Artist?.Nickname).ToList(),
                "duration" => tracks.OrderBy(t => t.Duration).ToList(),
                _ => tracks.OrderBy(t => t.Title).ToList()
            };

            tracks = tracks.Take(40).ToList();

            foreach (var track in tracks)
            {
                var fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", track.FilePath);
                var item = new TrackItem(track.TrackID, track.Title, track.Artist?.Nickname ?? "—", FormatDuration(track.Duration), fullPath, track.CoverPath, track.Duration);
                _trackItems.Add(item);
                ResultsContainer.Children.Add(item);
            }
        }

        private static string FormatDuration(int sec) => $"{sec / 60}:{sec % 60:D2}";
        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;
    }
}
