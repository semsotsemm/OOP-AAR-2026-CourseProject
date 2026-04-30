using Rewind.Contols;
using Rewind.Controls;
using Rewind.Helpers;
using Rewind.Pages;
using Rewind.Tabs.UsersTabs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

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
            int idx = _currentTrackItem == null ? 0 : (_playContext.IndexOf(_currentTrackItem) + 1) % _playContext.Count;
            PlayTrackFromContext(_playContext[idx], _playContext);
            PlaybackStateChanged?.Invoke();
        }

        public void PreviousTrack()
        {
            if (_playContext.Count == 0) return;
            int idx = _currentTrackItem == null ? 0 : (_playContext.IndexOf(_currentTrackItem) - 1 + _playContext.Count) % _playContext.Count;
            PlayTrackFromContext(_playContext[idx], _playContext);
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
            if (_mediaPlayer.NaturalDuration.HasTimeSpan)
                GlobalPlayerBar.TotalSeconds = _mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            PlaybackStateChanged?.Invoke();
        }

        private void MediaPlayer_MediaEnded(object? sender, EventArgs e)
        {
            _isPlaying = false;
            GlobalPlayerBar.PlayPauseIcon = IconAssets.GetAbsolutePath("player_play.png");
            NextTrack();
            UpdateIslandVisibility();
            PlaybackStateChanged?.Invoke();
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

        private void ShowProfile_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new ProfilePage();
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

        public void UpdateIslandSettings(bool enabled, double scale, double opacity)
        {
            _islandEnabled = enabled;
            _islandScale = Math.Clamp(scale, 0.8, 1.5);
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
            Button[] menuButtons = { BtnHome, BtnSearch, BtnFavorites, BtnPlaylists, BtnProfile };

            var activeAccent = (Brush)FindResource("AccentColor");
            var activeIconFill = Brushes.White; 

            var inactiveBg = (Brush)new BrushConverter().ConvertFrom("#F0EFEB");
            var inactiveText = (Brush)new BrushConverter().ConvertFrom("#888880");
            var inactiveIconFill = activeAccent;

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

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _timer.Stop();
            _mediaPlayer.Stop();
            _mediaPlayer.Close();

            if (_island != null)
            {
                try { _island.Close(); } catch { }
                _island = null;
            }
        }
    }
}