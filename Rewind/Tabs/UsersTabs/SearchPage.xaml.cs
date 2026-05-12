using Rewind.Contols;
using Rewind.Helpers;
using Rewind.MVVM.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rewind.Tabs.UsersTabs
{
    /// <summary>
    /// View поверх <see cref="SearchPageViewModel"/>.
    /// Вся логика поиска/фильтрации/сортировки — в VM.
    /// Code-behind отвечает только за отрисовку TrackItem-контролов
    /// (они пока не переведены на DataTemplate) и чипсов жанров.
    /// </summary>
    public partial class SearchPage : UserControl
    {
        private readonly SearchPageViewModel _vm = new();
        private readonly List<TrackItem> _trackItems = new();

        public SearchPage()
        {
            InitializeComponent();
            DataContext = _vm;

            _vm.ResultsChanged += RenderTrackItems;
            _vm.SelectedGenres.CollectionChanged += (_, _) => { BuildGenreFilters(); UpdateGenreDropdownLabel(); };

            BuildGenreFilters();
            UpdateGenreDropdownLabel();
            RenderTrackItems();
        }

        // ─── Прокидываем ввод в VM ───

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => _vm.Query = SearchBox.Text;

        private void ToggleSort_Click(object sender, MouseButtonEventArgs e)
        {
            _vm.ToggleSortCommand.Execute(null);
            if (SortLabel != null) SortLabel.Text = _vm.SortLabel;
        }

        private void ToggleGenreDropdown_Click(object sender, MouseButtonEventArgs e)
            => GenreDropdownPopup.IsOpen = !GenreDropdownPopup.IsOpen;

        private void ClearGenreFilters_Click(object sender, MouseButtonEventArgs e)
        {
            _vm.ClearGenresCommand.Execute(null);
            GenreDropdownPopup.IsOpen = false;
        }

        // ─── Визуальный рендер (остаётся в View) ───

        private void RenderTrackItems()
        {
            ResultsContainer.Children.Clear();
            _trackItems.Clear();

            foreach (var track in _vm.Results)
            {
                var fullPath = System.IO.Path.Combine(Rewind.Helpers.FileStorage.DataRoot, "MusicLibrary", track.FilePath);
                var item = new TrackItem(
                    track.TrackID, track.Title,
                    track.Artist?.Nickname ?? "—",
                    track.Duration > 0 ? $"{track.Duration / 60}:{track.Duration % 60:D2}" : "",
                    fullPath, track.CoverPath, track.Duration);

                item.MouseLeftButtonDown += (s, _) =>
                {
                    var it = (TrackItem)s;
                    if (Window.GetWindow(this) is MainWindow mw)
                        mw.PlayTrackFromContext(it, _trackItems);
                };

                _trackItems.Add(item);
                ResultsContainer.Children.Add(item);
            }
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
                Background = active ? (Brush)Application.Current.Resources["AccentColor"] : (Brush)Application.Current.Resources["BgSidebar"],
                BorderBrush = active ? Brushes.Transparent : (Brush)Application.Current.Resources["BorderColor"],
                BorderThickness = new Thickness(active ? 0 : 1.5),
                Tag = genre
            };

            border.Child = new TextBlock
            {
                Text = genre,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = active ? Brushes.White : (Brush)Application.Current.Resources["TextSecondary"],
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
