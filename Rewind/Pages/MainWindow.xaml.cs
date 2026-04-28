using Rewind.Contols;
using Rewind.Controls;
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
        private bool _isPlaying;

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
            Deactivated += (_, _) => { if (_isPlaying) ShowIsland(); };
            Activated += (_, _) => HideIsland();

            MainContentArea.Content = new MainPage();
            HighlightActiveButton(BtnHome);
        }

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
            GlobalPlayerBar.PlayPauseIcon = "⏸";
            GlobalPlayerBar.Visibility = Visibility.Visible;

            if (_island != null)
            {
                _island.UpdateTrackInfo(selectedTrack.TrackName, selectedTrack.ArtistName, selectedTrack.CoverPath);
                _island.SetPlayPauseIcon(true);
            }
        }

        public void TogglePlayPause()
        {
            if (_mediaPlayer.Source == null) return;

            if (_isPlaying)
            {
                _mediaPlayer.Pause();
                _isPlaying = false;
                GlobalPlayerBar.PlayPauseIcon = "▶";
                _island?.SetPlayPauseIcon(false);
                HideIsland();
            }
            else
            {
                _mediaPlayer.Play();
                _isPlaying = true;
                GlobalPlayerBar.PlayPauseIcon = "⏸";
                _island?.SetPlayPauseIcon(true);
            }
        }

        public void NextTrack()
        {
            if (_playContext.Count == 0) return;
            int idx = _currentTrackItem == null ? 0 : (_playContext.IndexOf(_currentTrackItem) + 1) % _playContext.Count;
            PlayTrackFromContext(_playContext[idx], _playContext);
        }

        public void PreviousTrack()
        {
            if (_playContext.Count == 0) return;
            int idx = _currentTrackItem == null ? 0 : (_playContext.IndexOf(_currentTrackItem) - 1 + _playContext.Count) % _playContext.Count;
            PlayTrackFromContext(_playContext[idx], _playContext);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_isPlaying || _mediaPlayer.Source == null) return;
            if (!_mediaPlayer.NaturalDuration.HasTimeSpan) return;
            if (!GlobalPlayerBar.IsUserDragging)
                GlobalPlayerBar.CurrentSeconds = _mediaPlayer.Position.TotalSeconds;
        }

        private void MediaPlayer_MediaOpened(object? sender, EventArgs e)
        {
            if (_mediaPlayer.NaturalDuration.HasTimeSpan)
                GlobalPlayerBar.TotalSeconds = _mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
        }

        private void MediaPlayer_MediaEnded(object? sender, EventArgs e)
        {
            _isPlaying = false;
            GlobalPlayerBar.PlayPauseIcon = "▶";
            NextTrack();
        }

        private void ShowIsland()
        {
            if (!_isPlaying || _currentTrackItem == null) return;
            if (_island == null)
            {
                _island = new IslandWindow { Topmost = true, ShowInTaskbar = false };
                _island.AttachPlayer(this);
                _island.Closed += (_, _) => _island = null;
            }

            _island.UpdateTrackInfo(_currentTrackItem.TrackName, _currentTrackItem.ArtistName, _currentTrackItem.CoverPath);
            _island.SetPlayPauseIcon(_isPlaying);
            _island.Left = (SystemParameters.PrimaryScreenWidth - _island.Width) / 2;
            _island.Top = 0;
            _island.Show();
        }

        private void HideIsland() => _island?.Hide();

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && _isPlaying) ShowIsland();
            else if (WindowState == WindowState.Normal) HideIsland();
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

        private void HighlightActiveButton(Button activeBtn)
        {
            Button[] menuButtons = { BtnHome, BtnSearch, BtnFavorites, BtnPlaylists, BtnProfile };

            var activeAccent = (Brush)FindResource("AccentColor");
            var activeIconFill = Brushes.White;

            var inactiveBg = (Brush)new BrushConverter().ConvertFrom("#F0EFEB");
            var inactiveText = (Brush)new BrushConverter().ConvertFrom("#888880");

            foreach (var btn in menuButtons)
            {
                bool isActive = btn == activeBtn;

                btn.Style = (Style)FindResource(isActive ? "ActiveNavButtonStyle" : "NavButtonStyle");

                if (btn.Content is StackPanel sp)
                {
                    if (sp.Children[0] is Border iconBorder)
                    {
                        iconBorder.Background = isActive ? activeAccent : inactiveBg;

                        if (iconBorder.Child is Path iconPath)
                        {
                            if (iconPath.Fill != null && iconPath.Fill != Brushes.Transparent)
                                iconPath.Fill = isActive ? activeIconFill : inactiveText;

                            if (iconPath.Stroke != null && iconPath.Stroke != Brushes.Transparent)
                                iconPath.Stroke = isActive ? activeIconFill : inactiveText;
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
    }
}