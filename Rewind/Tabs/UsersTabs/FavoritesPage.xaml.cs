using Rewind.Contols;
using Rewind.Helpers;
using Rewind.MVVM.ViewModels.Pages;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Rewind.Tabs.UsersTabs
{
    /// <summary>
    /// View поверх <see cref="FavoritesPageViewModel"/>.
    /// Логика фильтрации/сортировки/подписки на лайки — в VM.
    /// Code-behind генерирует TrackItem-контролы и чипсы жанров (визуальные детали).
    /// </summary>
    public partial class FavoritesPage : UserControl
    {
        private readonly FavoritesPageViewModel _vm = new();
        private readonly List<TrackItem> _trackItems = new();

        public FavoritesPage()
        {
            InitializeComponent();
            DataContext = _vm;

            _vm.ResultsChanged += RenderTrackItems;
            _vm.StatsChanged += UpdateStats;
            _vm.SelectedGenres.CollectionChanged += (_, _) => { BuildGenreFilters(); UpdateGenreDropdownLabel(); };

            BuildGenreFilters();
            UpdateGenreDropdownLabel();
            UpdateStats();
            RenderTrackItems();

            Unloaded += (_, _) => _vm.Dispose();
        }

        public void PlayMusic(string path, string title, string artist, double durationSeconds = 0)
        {
            var selected = _trackItems.FirstOrDefault(t => t.FilePath == path);
            if (selected != null && Window.GetWindow(this) is MainWindow mw)
                mw.PlayTrackFromContext(selected, _trackItems);
        }

        // ─── Делегируем в VM ───

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => _vm.Query = SearchBox.Text;

        private void ToggleSort_Click(object sender, MouseButtonEventArgs e)
        {
            _vm.ToggleSortCommand.Execute(null);
            if (FindName("SortLabel") is TextBlock label) label.Text = _vm.SortLabel;
        }

        private void PlayAll_Click(object sender, MouseButtonEventArgs e)
        {
            if (_vm.Results.Count == 0) return;
            var selected = _trackItems.FirstOrDefault();
            if (selected != null && Window.GetWindow(this) is MainWindow mw)
                mw.PlayTrackFromContext(selected, _trackItems);
        }

        private void ToggleGenreDropdown_Click(object sender, MouseButtonEventArgs e)
            => GenreDropdownPopup.IsOpen = !GenreDropdownPopup.IsOpen;

        private void ClearGenreFilters_Click(object sender, MouseButtonEventArgs e)
        {
            _vm.ClearGenresCommand.Execute(null);
            GenreDropdownPopup.IsOpen = false;
        }

        // ─── Визуальный рендер ───

        private void RenderTrackItems()
        {
            TracksContainer.Children.Clear();
            _trackItems.Clear();
            EmptyState.Visibility = _vm.Results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            foreach (var track in _vm.Results)
            {
                string durStr = $"{track.Duration / 60}:{track.Duration % 60:D2}";
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", track.FilePath);
                var artist = UserService.GetUserById(track.ArtistID)?.Nickname ?? "—";

                var item = new TrackItem(track.TrackID, track.Title, artist, durStr, fullPath, track.CoverPath, track.Duration);

                item.MouseLeftButtonDown += (s, _) =>
                {
                    var it = (TrackItem)s;
                    PlayMusic(it.FilePath, it.TrackName, it.ArtistName, it.DurationSeconds);
                };

                _trackItems.Add(item);
                TracksContainer.Children.Add(item);
            }
        }

        private void UpdateStats()
        {
            StatTracksCount.Text = _vm.TracksCount.ToString();
            StatArtistsCount.Text = _vm.ArtistsCount.ToString();
            StatDuration.Text = _vm.DurationText;
        }

        private void BuildGenreFilters()
        {
            GenreFilters.Children.Clear();
            GenreFilters.Children.Add(BuildGenreChip("Все", _vm.IsGenreActive("Все")));
            foreach (var g in _vm.AvailableGenres)
                GenreFilters.Children.Add(BuildGenreChip(g, _vm.IsGenreActive(g)));
        }

        private UIElement BuildGenreChip(string genre, bool active)
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

            border.Child = new TextBlock
            {
                Text = genre,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                FontFamily = (System.Windows.Media.FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = active
                    ? System.Windows.Media.Brushes.White
                    : (System.Windows.Media.Brush)Application.Current.Resources["TextSecondary"],
                VerticalAlignment = VerticalAlignment.Center
            };

            border.MouseLeftButtonDown += (_, _) => _vm.ToggleGenreCommand.Execute(genre);
            return border;
        }

        private void UpdateGenreDropdownLabel()
        {
            if (GenreDropdownLabel != null) GenreDropdownLabel.Text = _vm.GenreDropdownLabel;
        }

        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;
    }
}
