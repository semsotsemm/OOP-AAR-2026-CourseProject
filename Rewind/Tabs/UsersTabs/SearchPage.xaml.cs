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

        // Переменные для циклической сортировки
        private string _sortMode = "title";
        private static readonly string[] SortModes = { "title", "artist", "duration" };
        private static readonly string[] SortLabels = { "Название", "Исполнитель", "Длительность" };

        public SearchPage()
        {
            InitializeComponent();
            BuildGenreFilters();
            Render(string.Empty);
        }

        // Обработчик изменения текста в поиске
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => Render(SearchBox.Text.Trim().ToLower());

        // ЦИКЛИЧЕСКАЯ СОРТИРОВКА (как в примере)
        private void ToggleSort_Click(object sender, MouseButtonEventArgs e)
        {
            // Определяем следующий индекс сортировки
            int next = (Array.IndexOf(SortModes, _sortMode) + 1) % SortModes.Length;
            _sortMode = SortModes[next];

            // Обновляем текст в XAML (SortLabel)
            if (SortLabel != null)
            {
                SortLabel.Text = SortLabels[next];
            }

            Render(SearchBox.Text.Trim().ToLower());
        }

        private void Render(string query)
        {
            ResultsContainer.Children.Clear();
            _trackItems.Clear();

            // Получаем треки и фильтруем по жанрам и поисковому запросу
            var tracks = TrackService.GetPublishedTracks()
                .Where(t => _selectedGenres.Count == 0 || _selectedGenres.Contains(NormalizeGenre(t.Genre)))
                .Where(t => string.IsNullOrEmpty(query)
                            || t.Title.ToLower().Contains(query)
                            || (t.Artist?.Nickname?.ToLower().Contains(query) ?? false)
                            || NormalizeGenre(t.Genre).ToLower().Contains(query))
                .ToList();

            // Применяем выбранную сортировку
            tracks = _sortMode switch
            {
                "artist" => tracks.OrderBy(t => t.Artist?.Nickname).ToList(),
                "duration" => tracks.OrderBy(t => t.Duration).ToList(),
                _ => tracks.OrderBy(t => t.Title).ToList()
            };

            // Ограничиваем количество для производительности
            var finalTracks = tracks.Take(40).ToList();

            foreach (var track in finalTracks)
            {
                var fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", track.FilePath);

                // Создаем элемент трека
                var item = new TrackItem(
                    track.TrackID,
                    track.Title,
                    track.Artist?.Nickname ?? "—",
                    FormatDuration(track.Duration),
                    fullPath,
                    track.CoverPath,
                    track.Duration
                );

                // Добавляем логику клика для воспроизведения
                item.MouseLeftButtonDown += (s, e) => {
                    var it = (TrackItem)s;
                    if (Window.GetWindow(this) is MainWindow mainWindow)
                        mainWindow.PlayTrackFromContext(it, _trackItems);
                };

                _trackItems.Add(item);
                ResultsContainer.Children.Add(item);
            }
        }

        #region Жанры и Фильтры

        private void BuildGenreFilters()
        {
            GenreFilters.Children.Clear();
            var genres = GenreService.DefaultGenres
                .Concat(TrackService.GetPublishedTracks().Select(t => NormalizeGenre(t.Genre)))
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g)
                .ToList();

            GenreFilters.Children.Add(BuildGenreChip("Все", _selectedGenres.Count == 0));
            foreach (var genre in genres)
                GenreFilters.Children.Add(BuildGenreChip(genre, _selectedGenres.Contains(genre)));

            UpdateGenreDropdownLabel();
        }

        private UIElement BuildGenreChip(string genre, bool active)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                Background = active ? (Brush)Application.Current.Resources["AccentColor"] : (Brush)Application.Current.Resources["BgSidebar"],
                BorderBrush = active ? Brushes.Transparent : (Brush)Application.Current.Resources["BorderColor"],
                BorderThickness = new Thickness(active ? 0 : 1.5),
                Tag = genre
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            stack.Children.Add(new TextBlock
            {
                Text = genre,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
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

        #endregion

        private static string NormalizeGenre(string? genre) => string.IsNullOrWhiteSpace(genre) ? "Other" : genre.Trim();
        private static string FormatDuration(int sec) => $"{sec / 60}:{sec % 60:D2}";
        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;
    }
}