using Rewind.Contols;
using Rewind.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Rewind.Tabs.UsersTabs
{
    public partial class FavoritesPage : UserControl
    {
        private List<Track> _allTracks = new();
        private List<Track> _shown = new();
        private int? _playingId = null;
        private readonly HashSet<string> _selectedGenres = new(StringComparer.OrdinalIgnoreCase);
        private string _sortMode = "recent";
        private readonly List<TrackItem> _trackItems = new();

        private static readonly string[] SortModes = { "recent", "az", "artist", "duration" };
        private static readonly string[] SortLabels = { "Недавние", "А → Я", "Исполнитель", "Длительность" };



        public FavoritesPage()
        {
            InitializeComponent();
            LoadFavorites();
            BuildGenreFilters();
            Render();

            // Реальное время: обновляем список при любом изменении лайка
            Session.LikeChanged += OnLikeChanged;
            Unloaded += (_, _) => Session.LikeChanged -= OnLikeChanged;
        }

        private void OnLikeChanged(int trackId, bool isNowLiked)
        {
            Dispatcher.Invoke(() =>
            {
                if (!isNowLiked)
                {
                    // Убрать из списка
                    var toRemove = _allTracks.FirstOrDefault(t => t.TrackID == trackId);
                    if (toRemove != null) _allTracks.Remove(toRemove);
                }
                else
                {
                    // Добавить, если ещё нет
                    if (!_allTracks.Any(t => t.TrackID == trackId))
                    {
                        var track = TrackService.GetTrackById(trackId);
                        if (track != null) _allTracks.Add(track);
                    }
                }
                BuildGenreFilters();
                Render();
                UpdateStats();
            });
        }

        // Метод для кнопки "Воспроизвести всё"
        private void PlayAll_Click(object sender, MouseButtonEventArgs e)
        {
            if (_shown.Count > 0)
            {
                PlayTrack(_shown[0]);
            }
        }

        public void PlayMusic(string path, string title, string artist, double durationSeconds = 0)
        {
            var selected = _trackItems.FirstOrDefault(t => t.FilePath == path);
            if (selected != null && Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.PlayTrackFromContext(selected, _trackItems);
        }

        // Метод для переключения сортировки
        private void ToggleSort_Click(object sender, MouseButtonEventArgs e)
        {
            // Определяем следующий индекс сортировки
            int next = (Array.IndexOf(SortModes, _sortMode) + 1) % SortModes.Length;
            _sortMode = SortModes[next];

            // Если у тебя есть текстовый блок для отображения режима сортировки (например, SortLabel)
            // Если его нет в XAML или он называется иначе — закомментируй или поправь строку ниже
            if (FindName("SortLabel") is TextBlock label)
            {
                label.Text = SortLabels[next];
            }

            Render();
        }

        private void LoadFavorites()
        {
            _allTracks.Clear();
            try
            {
                // Берем список ID лайкнутых треков из текущей сессии
                var likedIds = Session.LikedTrackIds;

                foreach (var trackId in likedIds)
                {
                    // Получаем полный объект трека из БД по ID
                    var t = TrackService.GetTrackById(trackId);
                    if (t != null)
                    {
                        _allTracks.Add(t);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки избранного: {ex.Message}");
            }
            UpdateStats();
        }

        private void Render()
        {
            var query = SearchBox.Text.Trim().ToLower();

            // Фильтрация
            _shown = _allTracks
                .Where(t => _selectedGenres.Count == 0 || _selectedGenres.Contains(NormalizeGenre(t.Genre)))
                .Where(t => string.IsNullOrEmpty(query) ||
                            t.Title.ToLower().Contains(query) ||
                            (t.Artist?.Nickname?.ToLower().Contains(query) ?? false) ||
                            NormalizeGenre(t.Genre).ToLower().Contains(query))
                .ToList();

            // Сортировка
            _shown = _sortMode switch
            {
                "az" => _shown.OrderBy(t => t.Title).ToList(),
                "artist" => _shown.OrderBy(t => t.Artist?.Nickname).ToList(),
                "duration" => _shown.OrderBy(t => t.Duration).ToList(),
                _ => _shown
            };

            TracksContainer.Children.Clear();
            _trackItems.Clear(); // ОБЯЗАТЕЛЬНО очищаем, чтобы не было путаницы!
            EmptyState.Visibility = _shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            foreach (var track in _shown)
            {
                string durStr = FormatDuration(track.Duration);
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", track.FilePath);

                var item = new TrackItem(track.TrackID, track.Title, UserService.GetUserById(track.ArtistID).Nickname, durStr, fullPath, track.CoverPath, track.Duration);

                // ВАЖНО: Подписываемся на клик ПРЯМО ТУТ
                item.MouseLeftButtonDown += (s, e) => {
                    var it = (TrackItem)s;
                    // Вызываем PlayMusic именно этой страницы (FavoritesPage)
                    this.PlayMusic(it.FilePath, it.TrackName, it.ArtistName, it.DurationSeconds);
                };

                _trackItems.Add(item);
                TracksContainer.Children.Add(item);
            }
        }

        private void PlayTrack(Track t)
        {
            _playingId = (_playingId == t.TrackID) ? null : t.TrackID;
            Render();
            var selected = _trackItems.FirstOrDefault(i => i.TrackId == t.TrackID);
            if (selected != null && Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.PlayTrackFromContext(selected, _trackItems);
        }

        private void UpdateStats()
        {
            StatTracksCount.Text = _allTracks.Count.ToString();
            StatArtistsCount.Text = _allTracks.Select(t => t.ArtistID).Distinct().Count().ToString();
            int totalSec = _allTracks.Sum(t => t.Duration);
            StatDuration.Text = $"{totalSec / 60}м {totalSec % 60}с";
        }

        private void BuildGenreFilters()
        {
            GenreFilters.Children.Clear();

            // Список жанров: базовые + реально встречающиеся в любимых треках
            var genres = GenreService.DefaultGenres
                .Concat(_allTracks.Select(t => NormalizeGenre(t.Genre)))
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
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                Background = active
                    ? (System.Windows.Media.Brush)Application.Current.Resources["AccentColor"]
                    : (System.Windows.Media.Brush)Application.Current.Resources["BgSidebar"],
                BorderBrush = active
                    ? System.Windows.Media.Brushes.Transparent
                    : (System.Windows.Media.Brush)Application.Current.Resources["BorderColor"],
                BorderThickness = new Thickness(active ? 0 : 1.5),
                Tag = genre
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            stack.Children.Add(new TextBlock
            {
                Text = emoji,
                FontSize = 12,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            stack.Children.Add(new TextBlock
            {
                Text = genre,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                FontFamily = (System.Windows.Media.FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = active
                    ? System.Windows.Media.Brushes.White
                    : (System.Windows.Media.Brush)Application.Current.Resources["TextSecondary"],
                VerticalAlignment = VerticalAlignment.Center
            });
            border.Child = stack;

            border.MouseLeftButtonDown += (_, _) =>
            {
                if (genre == "Все") _selectedGenres.Clear();
                else
                {
                    if (_selectedGenres.Contains(genre)) _selectedGenres.Remove(genre);
                    else _selectedGenres.Add(genre);
                }
                BuildGenreFilters();
                Render();
            };
            return border;
        }

        private void ToggleGenreDropdown_Click(object sender, MouseButtonEventArgs e)
        {
            GenreDropdownPopup.IsOpen = !GenreDropdownPopup.IsOpen;
        }

        private void ClearGenreFilters_Click(object sender, MouseButtonEventArgs e)
        {
            _selectedGenres.Clear();
            BuildGenreFilters();
            Render();
            GenreDropdownPopup.IsOpen = false;
        }

        private void UpdateGenreDropdownLabel()
        {
            if (_selectedGenres.Count == 0)
            {
                GenreDropdownLabel.Text = "Все жанры";
                return;
            }

            GenreDropdownLabel.Text = _selectedGenres.Count <= 3
                ? string.Join(", ", _selectedGenres.OrderBy(g => g))
                : $"Выбрано жанров: {_selectedGenres.Count}";
        }

        private static string NormalizeGenre(string? genre)
            => string.IsNullOrWhiteSpace(genre) ? "Other" : genre.Trim();

        private static string GenreEmoji(string genre) => genre.ToLower() switch
        {
            "pop" => "✨",
            "rock" => "🎸",
            "hip-hop" => "🎤",
            "electronic" => "⚡",
            "r&b" => "💜",
            "jazz" => "🎷",
            "classical" => "🎻",
            "metal" => "🔥",
            "folk" => "🌿",
            "indie" => "🌙",
            "alternative" => "🌀",
            "dance" => "💃",
            "reggae" => "🌴",
            "latin" => "☀",
            _ => "🎵"
        };

        private string FormatDuration(int sec) => $"{sec / 60}:{sec % 60:D2}";
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Render();
        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;
    }
}
