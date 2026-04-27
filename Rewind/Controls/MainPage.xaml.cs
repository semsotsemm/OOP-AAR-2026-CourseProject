using System.IO;
using System.Windows;
using Rewind.DbManager;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Rewind.Controls
{
    public partial class MainPage : UserControl
    {
        private readonly MediaPlayer _mediaPlayer = new MediaPlayer();
        private readonly DispatcherTimer _timer;
        public bool IsPlaying { get; private set; } = false;
        private string _currentTrackTitle = "";
        private double _currentTrackDuration = 0;
        private TrackItem _currentTrackItem = null;
        private IslandWindow _island;

        private readonly List<TrackItem> _trackItems = new();

        public MainPage()
        {
            InitializeComponent();

            UpdateGreeting();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _timer.Tick += Timer_Tick;

            _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            _mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;

            // Чтобы поймать события окна, нужно подождать загрузки контрола
            this.Loaded += MainPage_Loaded;
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

        public void PreviousTrack()
        {
            if (_trackItems.Count == 0) return;

            // Находим индекс текущего трека
            int idx = _currentTrackItem == null ? 0 :
                      (_trackItems.IndexOf(_currentTrackItem) - 1 + _trackItems.Count) % _trackItems.Count;

            var prev = _trackItems[idx];
            PlayMusic(prev.FilePath, prev.TrackName, prev.ArtistName, prev.DurationSeconds);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                // Подписываемся на события Окна, а не Контрола
                parentWindow.StateChanged += MainWindow_StateChanged;
                parentWindow.Deactivated += (_, _) => { if (IsPlaying) ShowIsland(); };
                parentWindow.Activated += (_, _) => HideIsland();
            }
            LoadMusicFromFolder();
        }

        // --- Метод для смены темы (если карточки тем остались в MainPage) ---
        // Если они в другом месте, этот метод можно удалить
        private void ApplyTheme(string fileName)
        {
            try
            {
                var uri = new Uri($"Themes/{fileName}", UriKind.Relative);
                var newDict = new ResourceDictionary { Source = uri };
                var old = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source?.OriginalString.Contains("Themes/") == true);
                if (old != null)
                    Application.Current.Resources.MergedDictionaries.Remove(old);
                Application.Current.Resources.MergedDictionaries.Add(newDict);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void UpdateGreeting()
        {
            var h = DateTime.Now.Hour;
            string time = h < 6 ? "Доброй ночи" : h < 12 ? "Доброе утро" :
                          h < 18 ? "Добрый день" : "Добрый вечер";
            GreetingText.Text = $"{time}, {Session.UserName}";
        }

        private void LoadMusicFromFolder()
        {
            try
            {
                MusicContainer.Children.Clear();
                _trackItems.Clear();

                List<Track> tracks = TrackService.GetAllTracks();

                foreach (var track in tracks)
                {
                    string durStr = FormatDuration(track.Duration);
                    string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", track.FilePath);
                    var item = new TrackItem(track.Title, UserService.GetUserById(track.ArtistID).Nickname, durStr, fullPath,  track.CoverPath , track.Duration);

                    item.MouseDown += (s, e) => PlayMusic(item.FilePath, item.TrackName, item.ArtistName, item.DurationSeconds);

                    _trackItems.Add(item);
                    MusicContainer.Children.Add(item);
                }
            }
            catch { }
        }

        private static string FormatDuration(double seconds)
        {
            if (seconds <= 0) return "0:00";
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.Hours > 0 ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}" : $"{ts.Minutes}:{ts.Seconds:D2}";
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

            // Убедись, что BottomPlayerBar лежит внутри MainPage.xaml
            BottomPlayerBar.CurrentTrack = title;
            BottomPlayerBar.CurrentArtist = artist;
            BottomPlayerBar.Visibility = Visibility.Visible;
            BottomPlayerBar.PlayPauseIcon = "⏸";

            if (durationSeconds > 0)
                BottomPlayerBar.TotalSeconds = durationSeconds;

            _currentTrackItem = _trackItems.FirstOrDefault(t => t.FilePath == path);
            _currentTrackItem?.SetPlaying(true);
        }

        private void MediaPlayer_MediaOpened(object sender, EventArgs e)
        {
            if (_mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                double dur = _mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                _currentTrackDuration = dur;
                BottomPlayerBar.TotalSeconds = dur;
            }
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            IsPlaying = false;
            BottomPlayerBar.PlayPauseIcon = "▶";
            _currentTrackItem?.SetPlaying(false);
            NextTrack();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!IsPlaying || _mediaPlayer.Source == null) return;
            if (!_mediaPlayer.NaturalDuration.HasTimeSpan) return;
            if (!BottomPlayerBar.IsUserDragging)
                BottomPlayerBar.CurrentSeconds = _mediaPlayer.Position.TotalSeconds;
        }

        public void NextTrack()
        {
            if (_trackItems.Count == 0) return;
            int idx = _currentTrackItem == null ? 0 : (_trackItems.IndexOf(_currentTrackItem) + 1) % _trackItems.Count;
            var next = _trackItems[idx];
            PlayMusic(next.FilePath, next.TrackName, next.ArtistName, next.DurationSeconds);
        }

        private void FeaturedPlay_Click(object sender, MouseButtonEventArgs e)
        {
            if (_trackItems.Count > 0) PlayFirst();
            else MessageBox.Show("Нет треков");
        }

        private void PlayFirst() => PlayMusic(_trackItems[0].FilePath, _trackItems[0].TrackName, _trackItems[0].ArtistName, _trackItems[0].DurationSeconds);

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

        private void HideIsland() => _island?.Hide();

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            var win = sender as Window;
            if (win?.WindowState == WindowState.Minimized && IsPlaying) ShowIsland();
            else if (win?.WindowState == WindowState.Normal) HideIsland();
        }
    }
}