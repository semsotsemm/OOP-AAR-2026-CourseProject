using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TagLib;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Rewind
{
    public partial class MainWindow : Window
    {
        // ── Плеер ──────────────────────────────────────────────
        private readonly MediaPlayer _mediaPlayer = new MediaPlayer();
        private readonly DispatcherTimer _timer;
        public bool IsPlaying { get; private set; } = false;
        private string _currentTrackTitle = "";
        private double _currentTrackDuration = 0;
        private TrackItem _currentTrackItem = null;
        private IslandWindow _island;
        private string _tempAvatarPath = Session.AvatarPath;

        // ── Список треков ──────────────────────────────────────
        private readonly List<TrackItem> _trackItems = new();

        public MainWindow()
        {
            InitializeComponent();

            // Приветствие по времени суток
            UpdateGreeting();

            // Таймер обновления прогресса
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _timer.Tick += Timer_Tick;

            _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            _mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;

            this.StateChanged += MainWindow_StateChanged;
            this.Deactivated += (_, _) => { if (IsPlaying) ShowIsland(); };
            this.Activated += (_, _) => HideIsland();

            // Загрузить треки из папки
            LoadMusicFromFolder();
        }

        // ── Приветствие ────────────────────────────────────────

        private void UpdateGreeting()
        {
            var h = DateTime.Now.Hour;
            string time = h < 6 ? "Доброй ночи" : h < 12 ? "Доброе утро" :
                          h < 18 ? "Добрый день" : "Добрый вечер";
            GreetingText.Text = $"{time}, {Session.UserName}";
        }

        // ── Загрузка треков ────────────────────────────────────

        private void LoadMusicFromFolder()
        {
            try
            {
                string musicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "music");
                if (!Directory.Exists(musicPath)) Directory.CreateDirectory(musicPath);

                MusicContainer.Children.Clear();
                _trackItems.Clear();

                foreach (var file in Directory.GetFiles(musicPath, "*.mp3"))
                {
                    double dur = GetDurationSeconds(file);
                    string durStr = FormatDuration(dur);
                    string title = Path.GetFileNameWithoutExtension(file);

                    var item = new TrackItem(title, "Unknown", durStr, file, dur);
                    _trackItems.Add(item);
                    MusicContainer.Children.Add(item);
                }
            }
            catch { /* игнорируем */ }
        }

        // Считываем длительность mp3 через MediaPlayer синхронно
        private double GetDurationSeconds(string filePath)
        {
            try
            {
                // Используем TagLib для точного чтения длительности
                var file = TagLib.File.Create(filePath);
                return file.Properties.Duration.TotalSeconds;
            }
            catch
            {
                // Запасной вариант — 0, обновится при MediaOpened
                return 0;
            }
        }

        private static string FormatDuration(double seconds)
        {
            if (seconds <= 0) return "0:00";
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.Hours > 0
                ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes}:{ts.Seconds:D2}";
        }

        // ── Воспроизведение ────────────────────────────────────

        public void PlayMusic(string path, string title, string artist, double durationSeconds = 0)
        {
            // Снимаем подсветку с предыдущего
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

            // Подсвечиваем текущий трек
            _currentTrackItem = _trackItems.FirstOrDefault(t => t.FilePath == path);
            _currentTrackItem?.SetPlaying(true);

            _island?.Dispatcher.Invoke(() =>
            {
                if (_island.IsVisible) _island.TrackTitle.Text = title;
            });
        }

        private void MediaPlayer_MediaOpened(object sender, EventArgs e)
        {
            if (_mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                double dur = _mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                _currentTrackDuration = dur;
                BottomPlayerBar.TotalSeconds = dur;

                // Обновляем длительность в карточке трека если она была 0
                if (_currentTrackItem != null && _currentTrackItem.DurationSeconds <= 0)
                {
                    _currentTrackItem.DurationText.Text = FormatDuration(dur);
                }
            }
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            // Трек закончился — засчитываем прослушивание
            Session.AddListenedTrack(_currentTrackDuration);

            IsPlaying = false;
            BottomPlayerBar.PlayPauseIcon = "▶";
            _currentTrackItem?.SetPlaying(false);
            HideIsland();

            // Автопереход к следующему треку
            NextTrack();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!IsPlaying || _mediaPlayer.Source == null) return;
            if (!_mediaPlayer.NaturalDuration.HasTimeSpan) return;

            if (!BottomPlayerBar.IsUserDragging)
                BottomPlayerBar.CurrentSeconds = _mediaPlayer.Position.TotalSeconds;
        }

        public void SeekMusic(double seconds)
        {
            if (_mediaPlayer.Source != null && _mediaPlayer.NaturalDuration.HasTimeSpan)
                _mediaPlayer.Position = TimeSpan.FromSeconds(seconds);
        }

        public void TogglePlayPause()
        {
            if (IsPlaying)
            {
                _mediaPlayer.Pause();
                IsPlaying = false;
                BottomPlayerBar.PlayPauseIcon = "▶";
                HideIsland();
            }
            else if (_mediaPlayer.Source != null)
            {
                _mediaPlayer.Play();
                IsPlaying = true;
                BottomPlayerBar.PlayPauseIcon = "⏸";
                if (!this.IsActive) ShowIsland();
            }
        }

        public void NextTrack()
        {
            if (_trackItems.Count == 0) return;
            int idx = _currentTrackItem == null ? 0 :
                      (_trackItems.IndexOf(_currentTrackItem) + 1) % _trackItems.Count;
            var next = _trackItems[idx];
            PlayMusic(next.FilePath, next.TrackName, next.ArtistName, next.DurationSeconds);
        }

        public void PreviousTrack()
        {
            if (_trackItems.Count == 0) return;
            int idx = _currentTrackItem == null ? 0 :
                      (_trackItems.IndexOf(_currentTrackItem) - 1 + _trackItems.Count) % _trackItems.Count;
            var prev = _trackItems[idx];
            PlayMusic(prev.FilePath, prev.TrackName, prev.ArtistName, prev.DurationSeconds);
        }

        // ── Трек дня ───────────────────────────────────────────

        private void FeaturedPlay_Click(object sender, MouseButtonEventArgs e)
        {
            // Воспроизводим первый трек в папке как «трек дня»
            if (_trackItems.Count > 0)
            {
                var t = _trackItems[0];
                PlayMusic(t.FilePath, t.TrackName, t.ArtistName, t.DurationSeconds);
            }
            else
            {
                MessageBox.Show("Добавьте mp3-файлы в папку music рядом с приложением.");
            }
        }

        // ── Island ─────────────────────────────────────────────

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
            if (this.WindowState == WindowState.Minimized && IsPlaying) ShowIsland();
            else if (this.WindowState == WindowState.Normal) HideIsland();
        }

        // ── Навигация (переключение страниц) ───────────────────

        private void ShowPage(UIElement page)
        {
            PageHome.Visibility = Visibility.Collapsed;
            PageSearch.Visibility = Visibility.Collapsed;
            PageLiked.Visibility = Visibility.Collapsed;
            PagePlaylists.Visibility = Visibility.Collapsed;
            PageProfile.Visibility = Visibility.Collapsed;

            page.Visibility = Visibility.Visible;
        }

        private void SetNavActive(Button active)
        {
            var navButtons = new[] { NavHome, NavSearch, NavLiked, NavPlaylists, NavProfile };
            foreach (var b in navButtons)
                b.Style = (Style)FindResource("NavButtonStyle");
            active.Style = (Style)FindResource("ActiveNavButtonStyle");
        }

        private void NavHome_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(PageHome);
            SetNavActive(NavHome);
            UpdateGreeting();
        }

        private void NavSearch_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(PageSearch);
            SetNavActive(NavSearch);
        }

        private void NavLiked_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(PageLiked);
            SetNavActive(NavLiked);
            LikedCountText.Text = $"{Session.Liked} треков";
        }

        private void NavPlaylists_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(PagePlaylists);
            SetNavActive(NavPlaylists);
        }

        private void NavProfile_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(PageProfile);
            SetNavActive(NavProfile);
            SetProfileTab(PPanelOverview, PTabOverview);
        }

        private void BottomProfile_Click(object sender, MouseButtonEventArgs e)
        {
            NavProfile_Click(sender, e);
        }

        // ── Уведомления ────────────────────────────────────────

        private void NotifBell_Click(object sender, MouseButtonEventArgs e)
        {
            NotifOverlay.Visibility = Visibility.Visible;
        }

        private void CloseNotif_Click(object sender, MouseButtonEventArgs e)
        {
            NotifOverlay.Visibility = Visibility.Collapsed;
        }

        // ── Профиль — табы ─────────────────────────────────────

        private void SetProfileTab(UIElement panel, Button activeBtn)
        {
            PPanelOverview.Visibility = Visibility.Collapsed;
            PPanelLiked.Visibility = Visibility.Collapsed;
            PPanelPlaylists.Visibility = Visibility.Collapsed;
            PPanelSettings.Visibility = Visibility.Collapsed;

            PTabOverview.Style = (Style)FindResource("ProfileTab");
            PTabLiked.Style = (Style)FindResource("ProfileTab");
            PTabPlaylists.Style = (Style)FindResource("ProfileTab");
            PTabSettings.Style = (Style)FindResource("ProfileTab");

            panel.Visibility = Visibility.Visible;
            activeBtn.Style = (Style)FindResource("ProfileTabActive");
        }

        private void PTabOverview_Click(object sender, RoutedEventArgs e)
            => SetProfileTab(PPanelOverview, PTabOverview);
        private void PTabLiked_Click(object sender, RoutedEventArgs e)
            => SetProfileTab(PPanelLiked, PTabLiked);
        private void PTabPlaylists_Click(object sender, RoutedEventArgs e)
            => SetProfileTab(PPanelPlaylists, PTabPlaylists);
        private void PTabSettings_Click(object sender, RoutedEventArgs e)
            => SetProfileTab(PPanelSettings, PTabSettings);

        // ── Профиль — редактирование ───────────────────────────

        private void EditBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            EditNameInput.Text = Session.UserName;
            EditEmailInput.Text = Session.Email;
            EditPassInput.Password = Session.Password;
            _tempAvatarPath = Session.AvatarPath;
            EditOverlay.Visibility = Visibility.Visible;
        }

        private void CloseEdit_Click(object sender, RoutedEventArgs e)
            => EditOverlay.Visibility = Visibility.Collapsed;

        private void CloseEdit_Click(object sender, MouseButtonEventArgs e)
            => EditOverlay.Visibility = Visibility.Collapsed;

        private void UploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            { Filter = "Изображения (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png" };
            if (dlg.ShowDialog() == true)
            {
                _tempAvatarPath = dlg.FileName;
                AvatarPreview.ImageSource = new BitmapImage(new Uri(_tempAvatarPath));
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EditNameInput.Text))
            {
                MessageBox.Show("Имя не может быть пустым.");
                return;
            }
            Session.UserName = EditNameInput.Text;
            Session.Email = EditEmailInput.Text;
            if (!string.IsNullOrEmpty(EditPassInput.Password))
            {
                Session.Password = EditPassInput.Password;
                Session.HidedPassword = new string('●', Session.Password.Length);
            }
            Session.AvatarPath = _tempAvatarPath;
            UpdateGreeting();
            EditOverlay.Visibility = Visibility.Collapsed;
            MessageBox.Show("Профиль успешно обновлён.");
        }

        private void LogOut_Click(object sender, MouseButtonEventArgs e)
        {
            var reg = new Pages.Registration();
            reg.Show();
            this.Close();
        }

        // ── Смена темы ─────────────────────────────────────────

        private void ThemeCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border clicked) return;

            var cards = new List<Border> { ThemeClassic, ThemePink, ThemeMidnight, ThemeLavender };
            var normal = (Style)FindResource("ThemeCard");
            var active = (Style)FindResource("ThemeCardActive");
            foreach (var c in cards) c.Style = c == clicked ? active : normal;

            string themeFile = clicked.Name switch
            {
                "ThemeClassic" => "ThemeClassic.xaml",
                "ThemePink" => "ThemePink.xaml",
                "ThemeMidnight" => "ThemeMidnight.xaml",
                "ThemeLavender" => "ThemeLavender.xaml",
                _ => "ThemeClassic.xaml"
            };
            ApplyTheme(themeFile);
        }

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
                Session.ActiveTheme = fileName.Replace(".xaml", "");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка темы: {ex.Message}");
            }
        }
    }
}
