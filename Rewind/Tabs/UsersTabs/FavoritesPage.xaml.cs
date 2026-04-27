using Rewind.Contols;
using Rewind.Helpers;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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
        private readonly MediaPlayer _mediaPlayer = new MediaPlayer();
        private readonly DispatcherTimer _timer;
        public bool IsPlaying { get; private set; } = false;
        private string _currentTrackTitle = "";
        private double _currentTrackDuration = 0;
        private TrackItem _currentTrackItem = null;
        private IslandWindow _island;

        private static readonly string[] SortModes = { "recent", "az", "artist", "duration" };
        private static readonly string[] SortLabels = { "Недавние", "А → Я", "Исполнитель", "Длительность" };



        private void LoadMusicFromFolder()
        {
            try
            {
                TracksContainer.Children.Clear();
                _trackItems.Clear();

                List<Track> tracks = TrackService.GetAllTracks();

                foreach (var track in tracks)
                {
                    string durStr = FormatDuration(track.Duration);
                    string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", track.FilePath);
                    var item = new TrackItem(track.TrackID, track.Title, UserService.GetUserById(track.ArtistID).Nickname, durStr, fullPath, track.CoverPath, track.Duration);
                    _trackItems.Add(item);
                    TracksContainer.Children.Add(item);
                }
            }
            catch { }
        }

        private void FavortiteWindow_StateChanged(object sender, EventArgs e)
        {
            var win = sender as Window;
            if (win?.WindowState == WindowState.Minimized && IsPlaying) ShowIsland();
            else if (win?.WindowState == WindowState.Normal) HideIsland();
        }
        private void FavortitePage_Loaded(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.StateChanged += FavortiteWindow_StateChanged;
                parentWindow.Deactivated += (_, _) => { if (IsPlaying) ShowIsland(); };
                parentWindow.Activated += (_, _) => HideIsland();
            }
            LoadMusicFromFolder();
        }
        private void ShowIsland()
        {
            if (!IsPlaying) return;
            if (_island == null)
            {
                _island = new IslandWindow { Topmost = true, ShowInTaskbar = false };
                _island.Closed += (_, _) => _island = null;
            }
            _island.TrackTitle.Text = _currentTrackTitle;
            _island.Left = (SystemParameters.PrimaryScreenWidth - _island.Width) / 2;
            _island.Top = 0;
            _island.Show();
        }

        public void TogglePlayPause()
        {
            if (_mediaPlayer.Source == null) return;

            if (IsPlaying)
            {
                _mediaPlayer.Pause();
                IsPlaying = false;
                BottomPlayerBar.PlayPauseIcon = "▶";
                HideIsland(); // Если хочешь прятать остров на паузе
            }
            else
            {
                _mediaPlayer.Play();
                IsPlaying = true;
                BottomPlayerBar.PlayPauseIcon = "⏸";
                // ShowIsland(); // Можно вызвать показ острова здесь
            }
        }
        private void HideIsland() => _island?.Hide();
        public FavoritesPage()
        {
            InitializeComponent();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _timer.Tick += Timer_Tick;


            this.Loaded += FavortitePage_Loaded;
            LoadFavorites();
            BuildGenreFilters();
            Render();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!IsPlaying || _mediaPlayer.Source == null) return;
            if (!_mediaPlayer.NaturalDuration.HasTimeSpan) return;
            if (!BottomPlayerBar.IsUserDragging)
                BottomPlayerBar.CurrentSeconds = _mediaPlayer.Position.TotalSeconds;
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
            _currentTrackItem?.SetPlaying(false);
            _currentTrackTitle = title;
            _currentTrackDuration = durationSeconds;

            _mediaPlayer.Open(new Uri(path));
            _mediaPlayer.Play();
            IsPlaying = true;
            _timer.Start();

            BottomPlayerBar.CurrentTrack = title;
            BottomPlayerBar.CurrentArtist = artist;
            BottomPlayerBar.Visibility = Visibility.Visible;
            BottomPlayerBar.PlayPauseIcon = "⏸";

            if (durationSeconds > 0)
                BottomPlayerBar.TotalSeconds = durationSeconds;

            _currentTrackItem = _trackItems.FirstOrDefault(t => t.FilePath == path);
            _currentTrackItem?.SetPlaying(true);
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
    }
}