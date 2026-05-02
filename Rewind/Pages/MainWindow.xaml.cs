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
        private int _currentIndex = -1;
        private IslandWindow? _island;
        private NowPlaying? _nowPlayingWindow;
        private bool _isPlaying;
        private bool _repeatEnabled = true;   // по умолчанию включён: очередь играет по кругу
        private bool _shuffleActive;
        private readonly Random _shuffleRandom = new();
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
            GlobalPlayerBar.VolumeChangeRequested += (_, vol) => Volume = Math.Clamp(vol, 0, 1);
            GlobalPlayerBar.ShuffleClicked += (_, _) => ShuffleQueue();
            GlobalPlayerBar.RepeatClicked += (_, _) => ToggleRepeat();
            GlobalPlayerBar.SetRepeatActive(_repeatEnabled);
            GlobalPlayerBar.SetShuffleActive(_shuffleActive);
            GlobalPlayerBar.SetVolumeExternal(_mediaPlayer.Volume);
            VolumeChanged += vol => Dispatcher.Invoke(() => GlobalPlayerBar.SetVolumeExternal(vol));

            // Обновляем иконку лайка в плеере при любом переключении
            Session.LikeChanged += (trackId, _) =>
            {
                if (_currentTrackItem?.TrackId == trackId)
                    Dispatcher.Invoke(() => GlobalPlayerBar.UpdateLikeIcon(trackId));
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
        public event Action<double>? VolumeChanged;

        public bool RepeatEnabled => _repeatEnabled;
        public bool ShuffleActive => _shuffleActive;

        public void ToggleRepeat()
        {
            _repeatEnabled = !_repeatEnabled;
            GlobalPlayerBar.SetRepeatActive(_repeatEnabled);
            PlaybackStateChanged?.Invoke();
        }

        /// <summary>
        /// Случайно перемешивает текущую очередь. Текущий трек остаётся на своём месте,
        /// остальные перемешиваются. Каждое нажатие даёт новый порядок.
        /// </summary>
        public void ShuffleQueue()
        {
            if (_playContext.Count <= 1) return;

            var current = _currentTrackItem;
            // Все элементы кроме экземпляра текущего трека (по ссылке) — перемешиваем.
            var others = _playContext.Where(t => !ReferenceEquals(t, current))
                                     .OrderBy(_ => _shuffleRandom.Next())
                                     .ToList();
            _playContext.Clear();
            if (current != null)
            {
                _playContext.Add(current);
                _currentIndex = 0;
            }
            _playContext.AddRange(others);

            _shuffleActive = true;
            GlobalPlayerBar.SetShuffleActive(true);
            PlaybackStateChanged?.Invoke();
        }

        public void PlayTrackFromContext(TrackItem selectedTrack, IReadOnlyList<TrackItem> contextTracks)
        {
            if (selectedTrack == null || contextTracks == null || contextTracks.Count == 0) return;

            foreach (var item in _playContext)
                item.SetPlaying(false);

            _playContext.Clear();
            _playContext.AddRange(contextTracks);

            // Берём первое совпадение — по умолчанию пользователь кликнул конкретный экземпляр,
            // а в новом контексте дубликаты редкость.
            int idx = _playContext.IndexOf(selectedTrack);
            if (idx < 0) idx = _playContext.FindIndex(t => t.TrackId == selectedTrack.TrackId);
            if (idx < 0) idx = 0;

            PlayTrackAtIndexCore(idx);
        }

        /// <summary>
        /// Воспроизвести трек по точному индексу в текущей очереди — не меняет состав очереди.
        /// Используется при Next/Prev/клике в очереди, чтобы корректно работать с дубликатами.
        /// </summary>
        private void PlayTrackAtIndexCore(int index)
        {
            if (index < 0 || index >= _playContext.Count) return;

            _currentIndex = index;
            _currentTrackItem = _playContext[index];

            // Сбрасываем индикаторы у всех остальных экземпляров и подсвечиваем текущий
            foreach (var item in _playContext)
                item.SetPlaying(false);
            _currentTrackItem.SetPlaying(true);

            _mediaPlayer.Open(new Uri(_currentTrackItem.FilePath));
            _mediaPlayer.Play();
            _isPlaying = true;
            _timer.Start();

            GlobalPlayerBar.CurrentTrack = _currentTrackItem.TrackName;
            GlobalPlayerBar.CurrentArtist = _currentTrackItem.ArtistName;
            GlobalPlayerBar.TotalSeconds = _currentTrackItem.DurationSeconds > 0 ? _currentTrackItem.DurationSeconds : 0;
            GlobalPlayerBar.CurrentSeconds = 0;
            GlobalPlayerBar.PlayPauseIcon = IconAssets.GetAbsolutePath("player_pause.png");
            GlobalPlayerBar.Visibility = Visibility.Visible;
            GlobalPlayerBar.UpdateCover(_currentTrackItem.CoverPath);
            GlobalPlayerBar.UpdateLikeIcon(_currentTrackItem.TrackId);

            if (_island != null)
            {
                _island.UpdateTrackInfo(_currentTrackItem.TrackName, _currentTrackItem.ArtistName, _currentTrackItem.CoverPath);
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

        public void NextTrack() => AdvanceTrack(fromUser: true);

        /// <summary>
        /// Воспроизвести трек в текущей очереди по индексу (надёжно для дубликатов).
        /// </summary>
        public void PlayQueueIndex(int index) => PlayTrackAtIndexCore(index);

        private void AdvanceTrack(bool fromUser)
        {
            if (_playContext.Count == 0) return;

            int nextIdx;
            if (_currentIndex < 0) nextIdx = 0;
            else if (_currentIndex >= _playContext.Count - 1)
            {
                // Достигли конца очереди
                if (!_repeatEnabled && !fromUser)
                {
                    // Репит выключен и трек закончился сам — останавливаемся
                    _mediaPlayer.Pause();
                    _isPlaying = false;
                    GlobalPlayerBar.PlayPauseIcon = IconAssets.GetAbsolutePath("player_play.png");
                    _currentTrackItem?.SetPlayPauseIcon(false);
                    _island?.SetPlayPauseIcon(false);
                    PlaybackStateChanged?.Invoke();
                    return;
                }
                nextIdx = 0;
            }
            else nextIdx = _currentIndex + 1;

            PlayTrackAtIndexCore(nextIdx);
        }

        public void PreviousTrack()
        {
            if (_playContext.Count == 0) return;
            int prevIdx = _currentIndex < 0
                ? 0
                : (_currentIndex - 1 + _playContext.Count) % _playContext.Count;
            PlayTrackAtIndexCore(prevIdx);
        }

        public void RemoveFromQueue(int trackId)
        {
            // Удаляем первый трек с таким TrackId, который не является сейчас играющим (по ссылке).
            int removeIdx = _playContext.FindIndex(t => t.TrackId == trackId && !ReferenceEquals(t, _currentTrackItem));
            if (removeIdx < 0) return;
            RemoveAtCore(removeIdx);
        }

        /// <summary>Удалить элемент очереди по конкретному индексу (надёжно для дубликатов).</summary>
        public void RemoveFromQueueAt(int index)
        {
            if (index < 0 || index >= _playContext.Count) return;
            if (index == _currentIndex) return; // нельзя удалить играющий
            RemoveAtCore(index);
        }

        private void RemoveAtCore(int index)
        {
            _playContext.RemoveAt(index);
            if (index < _currentIndex) _currentIndex--;
            PlaybackStateChanged?.Invoke();
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

            int insertAt = (_currentIndex >= 0 && _currentIndex < _playContext.Count - 1)
                ? _currentIndex + 1
                : _playContext.Count;
            _playContext.Insert(insertAt, item);
            // Если вставили перед текущим — сместим индекс (на самом деле никогда, но на всякий случай)
            if (insertAt <= _currentIndex) _currentIndex++;
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
                AdvanceTrack(fromUser: false);
                UpdateIslandVisibility();
                PlaybackStateChanged?.Invoke();
            });
        }

        public TrackItem? CurrentTrack => _currentTrackItem;
        public IReadOnlyList<TrackItem> CurrentContext => _playContext;
        public int CurrentIndex => _currentIndex;
        public bool IsPlaying => _isPlaying;
        public bool IslandEnabled => _islandEnabled;
        public double IslandScale => _islandScale;
        public double IslandOpacity => _islandOpacity;
        public double CurrentSeconds => _mediaPlayer.Source == null ? 0 : _mediaPlayer.Position.TotalSeconds;
        public double TotalSeconds => _mediaPlayer.NaturalDuration.HasTimeSpan ? _mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds : 0;
        public double Volume
        {
            get => _mediaPlayer.Volume;
            set
            {
                double v = Math.Clamp(value, 0, 1);
                if (Math.Abs(_mediaPlayer.Volume - v) < 0.0001) return;
                _mediaPlayer.Volume = v;
                VolumeChanged?.Invoke(v);
            }
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

        public void OpenAlbumDetails(Album album)
        {
            MainContentArea.Content = new AlbumDetailsPage(album);
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
                                "Трек отправлен!",
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
