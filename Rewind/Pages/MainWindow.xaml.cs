using Rewind.Contols;
using Rewind.Controls;
using Rewind.Helpers;
using Rewind.Pages;
using Rewind.Tabs.UsersTabs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Collections.Generic;

namespace Rewind
{
    public partial class MainWindow : Window
    {
        private readonly MediaPlayer _mediaPlayer = new();
        private readonly DispatcherTimer _timer;
        private readonly List<TrackItem> _playContext = new();

        private TrackItem? _currentTrackItem;
        private IslandWindow? _island;
        private NowPlaying? _nowPlayingWindow;
        private bool _isPlaying;
        private bool _islandEnabled = true;
        private double _islandScale = 1.0;
        private double _islandOpacity = 1.0;
        private DispatcherTimer? _toastTimer;

        public MainWindow()
        {
            InitializeComponent();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _timer.Tick += Timer_Tick;

            _mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

            GlobalPlayerBar.PlayPauseClicked += (_, _) => TogglePlayPause();
            GlobalPlayerBar.PreviousClicked += (_, _) => PreviousTrack();
            GlobalPlayerBar.NextClicked += (_, _) => NextTrack();
            GlobalPlayerBar.SeekRequested += (_, seconds) =>
            {
                if (_mediaPlayer.Source != null && _mediaPlayer.NaturalDuration.HasTimeSpan)
                    _mediaPlayer.Position = TimeSpan.FromSeconds(seconds);
            };

            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;

            TrackService.NewTrackUploaded += OnNewTrackUploaded;

            // Show Studio button only for artists
            if (Session.UserRole?.ToLower() == "исполнитель")
                BtnStudio.Visibility = Visibility.Visible;

            MainContentArea.Content = new MainPage();
            HighlightActiveButton(BtnHome);
        }

        public event Action? PlaybackStateChanged;

        public void PlayTrackFromContext(TrackItem selectedTrack, IReadOnlyList<TrackItem> contextTracks)
        {
            if (selectedTrack == null || contextTracks == null || contextTracks.Count == 0) return;

            foreach (var item in _playContext)
                item.SetPlaying(false);

            _playContext.Clear();
            _playContext.AddRange(contextTracks);

            _currentTrackItem = selectedTrack;
            _currentTrackItem.SetPlaying(true);

            _mediaPlayer.Open(new Uri(selectedTrack.FilePath));
            _mediaPlayer.Play();
            _isPlaying = true;
            _timer.Start();

            GlobalPlayerBar.CurrentTrack = selectedTrack.TrackName;
            GlobalPlayerBar.CurrentArtist = selectedTrack.ArtistName;
            GlobalPlayerBar.TotalSeconds = selectedTrack.DurationSeconds > 0 ? selectedTrack.DurationSeconds : 0;
            GlobalPlayerBar.CurrentSeconds = 0;
            GlobalPlayerBar.PlayPauseIcon = IconAssets.GetAbsolutePath("player_pause.png");
            GlobalPlayerBar.Visibility = Visibility.Visible;

            if (_island != null)
            {
                _island.UpdateTrackInfo(selectedTrack.TrackName, selectedTrack.ArtistName, selectedTrack.CoverPath);
                _island.SetPlayPauseIcon(true);
            }
            UpdateIslandVisibility();
            PlaybackStateChanged?.Invoke();
        }

        public void TogglePlayPause()
        {
            if (_mediaPlayer.Source == null) return;

            if (_isPlaying)
            {
                _mediaPlayer.Pause();
                _isPlaying = false;
                GlobalPlayerBar.PlayPauseIcon = IconAssets.GetAbsolutePath("player_play.png");
                _currentTrackItem?.SetPlayPauseIcon(false);
                _island?.SetPlayPauseIcon(false);
            }
            else
            {
                _mediaPlayer.Play();
                _isPlaying = true;
                GlobalPlayerBar.PlayPauseIcon = IconAssets.GetAbsolutePath("player_pause.png");
                _currentTrackItem?.SetPlayPauseIcon(true);
                _island?.SetPlayPauseIcon(true);
            }
            UpdateIslandVisibility();
            PlaybackStateChanged?.Invoke();
        }

        public void NextTrack()
        {
            if (_playContext.Count == 0) return;
            var snapshot = _playContext.ToList(); // make a copy first!
            int idx = _currentTrackItem == null ? 0 : (snapshot.FindIndex(t => t.TrackId == _currentTrackItem.TrackId) + 1) % snapshot.Count;
            if (idx < 0) idx = 0;
            PlayTrackFromContext(snapshot[idx], snapshot);
            PlaybackStateChanged?.Invoke();
        }

        public void PreviousTrack()
        {
            if (_playContext.Count == 0) return;
            var snapshot = _playContext.ToList();
            int idx = _currentTrackItem == null ? 0 : (snapshot.FindIndex(t => t.TrackId == _currentTrackItem.TrackId) - 1 + snapshot.Count) % snapshot.Count;
            if (idx < 0) idx = 0;
            PlayTrackFromContext(snapshot[idx], snapshot);
            PlaybackStateChanged?.Invoke();
        }

        public void RemoveFromQueue(int trackId)
        {
            var item = _playContext.FirstOrDefault(t => t.TrackId == trackId);
            if (item != null && item != _currentTrackItem)
            {
                _playContext.Remove(item);
                PlaybackStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// Inserts <paramref name="item"/> as the very next track after the current one.
        /// If nothing is playing, starts playing immediately.
        /// </summary>
        public void PlayNext(TrackItem item)
        {
            if (item == null) return;

            if (_playContext.Count == 0 || _currentTrackItem == null)
            {
                PlayTrackFromContext(item, new List<TrackItem> { item });
                return;
            }

            // Remove duplicate (skip if it IS the currently playing track)
            var dup = _playContext.FirstOrDefault(t => t.TrackId == item.TrackId && t != _currentTrackItem);
            if (dup != null) _playContext.Remove(dup);

            int curIdx = _playContext.FindIndex(t => t.TrackId == _currentTrackItem.TrackId);
            int insertAt = (curIdx >= 0 && curIdx < _playContext.Count - 1) ? curIdx + 1 : _playContext.Count;
            _playContext.Insert(insertAt, item);
            PlaybackStateChanged?.Invoke();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_isPlaying || _mediaPlayer.Source == null) return;
            if (!_mediaPlayer.NaturalDuration.HasTimeSpan) return;
            if (!GlobalPlayerBar.IsUserDragging)
                GlobalPlayerBar.CurrentSeconds = _mediaPlayer.Position.TotalSeconds;
            PlaybackStateChanged?.Invoke();
        }

        private void MediaPlayer_MediaOpened(object? sender, EventArgs e)
        {
            // Media events fire on media thread — must dispatch to UI thread
            Dispatcher.BeginInvoke(() =>
            {
                if (_mediaPlayer.NaturalDuration.HasTimeSpan)
                    GlobalPlayerBar.TotalSeconds = _mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                PlaybackStateChanged?.Invoke();
            });
        }

        private void MediaPlayer_MediaEnded(object? sender, EventArgs e)
        {
            // CRITICAL: MediaEnded fires on the media thread — MUST dispatch to UI thread
            // Without this, _playContext.Clear() + NextTrack() race and the queue breaks
            Dispatcher.BeginInvoke(() =>
            {
                _isPlaying = false;
                GlobalPlayerBar.PlayPauseIcon = IconAssets.GetAbsolutePath("player_play.png");
                NextTrack();
                UpdateIslandVisibility();
                PlaybackStateChanged?.Invoke();
            });
        }

        public TrackItem? CurrentTrack => _currentTrackItem;
        public IReadOnlyList<TrackItem> CurrentContext => _playContext;
        public bool IsPlaying => _isPlaying;
        public bool IslandEnabled => _islandEnabled;
        public double IslandScale => _islandScale;
        public double IslandOpacity => _islandOpacity;
        public double CurrentSeconds => _mediaPlayer.Source == null ? 0 : _mediaPlayer.Position.TotalSeconds;
        public double TotalSeconds => _mediaPlayer.NaturalDuration.HasTimeSpan ? _mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds : 0;
        public double Volume
        {
            get => _mediaPlayer.Volume;
            set => _mediaPlayer.Volume = Math.Clamp(value, 0, 1);
        }

        private void MyUserControl_ThemeChanged(object sender, EventArgs e)
        {
            HighlightActiveButton(BtnProfile);
        }

        public void SeekTo(double seconds)
        {
            if (_mediaPlayer.Source == null || !_mediaPlayer.NaturalDuration.HasTimeSpan) return;
            _mediaPlayer.Position = TimeSpan.FromSeconds(Math.Clamp(seconds, 0, TotalSeconds));
            PlaybackStateChanged?.Invoke();
        }

        public void OpenNowPlaying(string sourcePage)
        {
            if (_currentTrackItem == null) return;

            if (_nowPlayingWindow == null || !_nowPlayingWindow.IsLoaded)
            {
                _nowPlayingWindow = new NowPlaying(this, sourcePage);
                _nowPlayingWindow.Owner = this;
                _nowPlayingWindow.Closed += (_, _) => _nowPlayingWindow = null;
                _nowPlayingWindow.Show();
                return;
            }

            _nowPlayingWindow.UpdateSourcePage(sourcePage);
            _nowPlayingWindow.Activate();
        }

        private void ShowIsland()
        {
            if (!_islandEnabled || _currentTrackItem == null) return;
            if (_island == null)
            {
                _island = new IslandWindow { Topmost = true, ShowInTaskbar = false };
                _island.AttachPlayer(this);
                _island.Closed += (_, _) => _island = null;
            }

            _island.UpdateTrackInfo(_currentTrackItem.TrackName, _currentTrackItem.ArtistName, _currentTrackItem.CoverPath);
            _island.SetPlayPauseIcon(_isPlaying);
            _island.ApplyVisualSettings(_islandScale, _islandOpacity);
            _island.Left = (SystemParameters.PrimaryScreenWidth - _island.Width) / 2;
            _island.Top = 0;
            _island.Topmost = true;
            _island.Show();
        }

        private void HideIsland() => _island?.Hide();

        private void UpdateIslandVisibility()
        {
            bool hasTrack = _currentTrackItem != null && _mediaPlayer.Source != null;
            bool shouldShow = _islandEnabled && hasTrack && WindowState == WindowState.Minimized;
            if (shouldShow) ShowIsland();
            else HideIsland();
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            UpdateIslandVisibility();
        }

        private void ShowStudio_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new ArtistStudioPage();
            HighlightActiveButton(BtnStudio);
        }

        // ────────────────────────────────────────────────
        //  Навигация на страницу исполнителя
        // ────────────────────────────────────────────────

        public void OpenArtistProfile(int artistId)
        {
            if (artistId <= 0) return;
            MainContentArea.Content = new ArtistProfilePage(artistId);
        }

        public void OpenArtistProfileByName(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return;
            var user = UserService.GetUserByNickname(nickname);
            if (user != null) OpenArtistProfile(user.UserId);
        }

        public void NavigateBack()
        {
            // Возвращаемся на главную страницу без сложной истории
            MainContentArea.Content = new MainPage();
            HighlightActiveButton(BtnHome);
        }

        private void ShowProfile_Click(object sender, RoutedEventArgs e)
        {
            var profilePage = new ProfilePage();

            profilePage.ThemeChanged += MyUserControl_ThemeChanged;

            MainContentArea.Content = profilePage;
            HighlightActiveButton(BtnProfile);
        }

        private void ShowPlaylists_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new PlaylistsPage();
            HighlightActiveButton(BtnPlaylists);
        }

        public void ShowPlaylistsPage()
        {
            MainContentArea.Content = new PlaylistsPage();
            HighlightActiveButton(BtnPlaylists);
        }

        public void OpenPlaylistDetails(Playlist playlist)
        {
            MainContentArea.Content = new PlaylistDetailsPage(playlist);
            HighlightActiveButton(BtnPlaylists);
        }

        private void ShowHome_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new MainPage();
            HighlightActiveButton(BtnHome);
        }

        private void ShowLiked_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new FavoritesPage();
            HighlightActiveButton(BtnFavorites);
        }

        private void ShowSearch_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new SearchPage();
            HighlightActiveButton(BtnSearch);
        }

        public void UpdateIslandSettings(bool enabled, double opacity)
        {
            _islandEnabled = enabled;
            _islandOpacity = Math.Clamp(opacity, 0.25, 1.0);

            if (!_islandEnabled)
            {
                HideIsland();
                return;
            }

            _island?.ApplyVisualSettings(_islandScale, _islandOpacity);
            UpdateIslandVisibility();
        }
        private void HighlightActiveButton(Button activeBtn)
        {
            var btnList = new List<Button> { BtnHome, BtnSearch, BtnFavorites, BtnPlaylists, BtnProfile };
            if (BtnStudio.Visibility == Visibility.Visible)
                btnList.Add(BtnStudio);
            Button[] menuButtons = btnList.ToArray();

            var activeAccent = (Brush)FindResource("AccentColor");
            var activeIconFill = (Brush)FindResource("BgMain");
            var inactiveIconFill = (Brush)FindResource("AccentColor");

            var inactiveBg = (Brush)FindResource("BgMain");
            var inactiveText = (Brush)FindResource("TextPrimary");

            foreach (var btn in menuButtons)
            {
                bool isActive = btn == activeBtn;

                btn.Style = (Style)FindResource(isActive ? "ActiveNavButtonStyle" : "NavButtonStyle");

                if (btn.Content is StackPanel sp)
                {
                    if (sp.Children[0] is Border iconBorder)
                    {
                        iconBorder.Background = isActive ? activeAccent : inactiveBg;

                        if (iconBorder.Child is Rectangle iconRect)
                        {
                            iconRect.Fill = isActive ? activeIconFill : inactiveIconFill;
                        }
                    }

                    if (sp.Children.Count > 1 && sp.Children[1] is TextBlock textBlock)
                    {
                        textBlock.Foreground = isActive ? activeAccent : inactiveText;
                        textBlock.FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal;
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────
        //  Тоаст-уведомления
        // ─────────────────────────────────────────────────

        public void ShowToastNotification(string title, string body)
        {
            ToastTitle.Text = title;
            ToastBody.Text = body;
            ToastBorder.Visibility = Visibility.Visible;

            _toastTimer?.Stop();
            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _toastTimer.Tick += (_, _) =>
            {
                ToastBorder.Visibility = Visibility.Collapsed;
                _toastTimer?.Stop();
            };
            _toastTimer.Start();
        }

        private void ToastDismiss_Click(object sender, MouseButtonEventArgs e)
        {
            ToastBorder.Visibility = Visibility.Collapsed;
            _toastTimer?.Stop();
        }

        private void OnNewTrackUploaded(int artistId, string artistName, string trackTitle)
        {
            // Исполнитель загрузил свой трек — показываем сколько подписчиков получат уведомление
            if (artistId == Session.UserId)
            {
                try
                {
                    var followers = SubscriptionService.GetFollowers(artistId);
                    if (followers.Count > 0 && Session.NotifNewTracksEnabled && Session.NotifPushEnabled)
                    {
                        string word = PluralFollowers(followers.Count);
                        Dispatcher.Invoke(() =>
                            ShowToastNotification(
                                "🎵 Трек отправлен!",
                                $"Ваши {followers.Count} {word} получат уведомление после одобрения."));
                    }
                }
                catch { }
                return;
            }

            // В реальной системе здесь был бы server-push для всех подписчиков;
            // в desktop-демо проверяем только текущего пользователя
            if (!SubscriptionService.IsFollowing(Session.UserId, artistId)) return;
            if (!Session.NotifNewTracksEnabled) return;

            if (Session.NotifPushEnabled)
                Dispatcher.Invoke(() =>
                    ShowToastNotification($"Новый трек от {artistName}", $"«{trackTitle}»"));
            else
                Dispatcher.Invoke(() => Session.NotificationCount++);
        }

        private static string PluralFollowers(int n)
        {
            if (n % 100 is >= 11 and <= 19) return "подписчиков";
            return (n % 10) switch { 1 => "подписчик", 2 or 3 or 4 => "подписчика", _ => "подписчиков" };
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _timer.Stop();
            _toastTimer?.Stop();
            _mediaPlayer.Stop();
            _mediaPlayer.Close();

            TrackService.NewTrackUploaded -= OnNewTrackUploaded;

            if (_island != null)
            {
                try { _island.Close(); } catch { }
                _island = null;
            }
        }
    }
}
