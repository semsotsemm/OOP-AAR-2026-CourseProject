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
        private readonly HashSet<string> _selectedGenres = new(StringComparer.OrdinalIgnoreCase);
        private string _sortMode = "title"; // title / artist / duration

        public SearchPage()
        {
            InitializeComponent();
            BuildGenreFilters();
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
                .Where(t => _selectedGenres.Count == 0 || _selectedGenres.Contains(NormalizeGenre(t.Genre)))
                .Where(t => string.IsNullOrEmpty(query)
                            || t.Title.ToLower().Contains(query)
                            || (t.Artist?.Nickname?.ToLower().Contains(query) ?? false)
                            || NormalizeGenre(t.Genre).ToLower().Contains(query))
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

        private void BuildGenreFilters()
        {
            GenreFilters.Children.Clear();
            var genres = GenreService.DefaultGenres
                .Concat(TrackService.GetPublishedTracks().Select(t => NormalizeGenre(t.Genre)))
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g)
                .ToList();

            GenreFilters.Children.Add(BuildGenreChip("Все", "✦", _selectedGenres.Count == 0));
            foreach (var genre in genres)
                GenreFilters.Children.Add(BuildGenreChip(genre, GenreEmoji(genre), _selectedGenres.Contains(genre)));

            UpdateGenreDropdownLabel();
        }

        private UIElement BuildGenreChip(string genre, string emoji, bool active)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 8, 8),
                Cursor = Cursors.Hand,
                Background = active ? (Brush)Application.Current.Resources["AccentColor"] : (Brush)Application.Current.Resources["BgSidebar"],
                BorderBrush = active ? Brushes.Transparent : (Brush)Application.Current.Resources["BorderColor"],
                BorderThickness = new Thickness(active ? 0 : 1.5),
                Tag = genre
            };
            var stack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            stack.Children.Add(new TextBlock { Text = emoji, FontSize = 12, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
            stack.Children.Add(new TextBlock
            {
                Text = genre, FontSize = 12, FontWeight = FontWeights.SemiBold,
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = active ? Brushes.White : (Brush)Application.Current.Resources["TextSecondary"],
                VerticalAlignment = VerticalAlignment.Center
            });
            border.Child = stack;
            border.MouseLeftButtonDown += (_, _) =>
            {
                if (genre == "Все") _selectedGenres.Clear();
                else if (_selectedGenres.Contains(genre)) _selectedGenres.Remove(genre);
                else _selectedGenres.Add(genre);
                BuildGenreFilters();
                Render(SearchBox.Text.Trim().ToLower());
            };
            return border;
        }

        private void ToggleGenreDropdown_Click(object sender, MouseButtonEventArgs e)
            => GenreDropdownPopup.IsOpen = !GenreDropdownPopup.IsOpen;

        private void ClearGenreFilters_Click(object sender, MouseButtonEventArgs e)
        {
            _selectedGenres.Clear();
            BuildGenreFilters();
            Render(SearchBox.Text.Trim().ToLower());
            GenreDropdownPopup.IsOpen = false;
        }

        private void UpdateGenreDropdownLabel()
        {
            GenreDropdownLabel.Text = _selectedGenres.Count == 0
                ? "Все жанры"
                : _selectedGenres.Count <= 3
                    ? string.Join(", ", _selectedGenres.OrderBy(g => g))
                    : $"Выбрано жанров: {_selectedGenres.Count}";
        }

        private static string NormalizeGenre(string? genre) => string.IsNullOrWhiteSpace(genre) ? "Other" : genre.Trim();

        private static string GenreEmoji(string genre) => genre.ToLower() switch
        {
            "pop" => "✨", "rock" => "🎸", "hip-hop" => "🎤", "electronic" => "⚡", "r&b" => "💜",
            "jazz" => "🎷", "classical" => "🎻", "metal" => "🔥", "folk" => "🌿", "indie" => "🌙",
            "alternative" => "🌀", "dance" => "💃", "reggae" => "🌴", "latin" => "☀", _ => "🎵"
        };

        private static string FormatDuration(int sec) => $"{sec / 60}:{sec % 60:D2}";
        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;
    }
}
