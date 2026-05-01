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
        private string _activeFilter = "Все";
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
                .Where(t => _activeFilter == "Все" || (t.Artist?.Nickname == _activeFilter))
                .Where(t => string.IsNullOrEmpty(query) ||
                            t.Title.ToLower().Contains(query) ||
                            (t.Artist?.Nickname?.ToLower().Contains(query) ?? false))
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
            var artists = new[] { "Все" }.Concat(_allTracks.Select(t => t.Artist?.Nickname).Where(n => n != null).Distinct().OrderBy(a => a)).Take(9);
            foreach (var name in artists)
            {
                var btn = new Button { Content = name, Margin = new Thickness(0, 0, 5, 0), Tag = name };
                btn.Click += (s, e) => { _activeFilter = (string)((Button)s).Tag; Render(); };
                GenreFilters.Children.Add(btn);
            }
        }

        private string FormatDuration(int sec) => $"{sec / 60}:{sec % 60:D2}";
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Render();
        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;
    }
}
