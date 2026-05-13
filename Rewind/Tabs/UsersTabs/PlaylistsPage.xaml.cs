using Microsoft.Win32;
using Rewind.Helpers;
using Rewind.MVVM.Services;
using Rewind.MVVM.ViewModels.Entities;
using Rewind.MVVM.ViewModels.Pages;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rewind.Tabs.UsersTabs
{
    /// <summary>
    /// View поверх <see cref="PlaylistsPageViewModel"/>.
    /// Логика загрузки/фильтрации в VM. Рендер карточек — императивно
    /// (сложные вёрстка с тенями, обложками и бэйджами приватности).
    /// </summary>
    public partial class PlaylistsPage : UserControl
    {
        private readonly PlaylistsPageViewModel _vm;
        private string? _pendingCoverPath;

        public PlaylistsPage()
        {
            InitializeComponent();
            _vm = new PlaylistsPageViewModel(
                ServiceLocator.Resolve<INavigationService>(),
                ServiceLocator.Resolve<IDialogService>());
            DataContext = _vm;
            _vm.RenderRequested += Render;
            Render();
            Unloaded += (_, _) => _vm.Dispose();
        }

        // ─── Делегирование в VM ───

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => _vm.Query = SearchBox.Text;
        private void FilterAll_Click(object sender, MouseButtonEventArgs e) => _vm.FilterAllCommand.Execute(null);
        private void FilterOwn_Click(object sender, MouseButtonEventArgs e) => _vm.FilterOwnCommand.Execute(null);
        private void FilterSaved_Click(object sender, MouseButtonEventArgs e) => _vm.FilterSavedCommand.Execute(null);

        // Совместимость с XAML
        private void SwitchToGrid_Click(object sender, MouseButtonEventArgs e) { /* always grid */ }
        private void SwitchToList_Click(object sender, MouseButtonEventArgs e) { /* always grid */ }
        private void FavoritesPlaylist_Click(object sender, MouseButtonEventArgs e)
            => ServiceLocator.TryResolve<IDialogService>()?.Info("Открываем: Любимые треки");

        // ─── Модал создания плейлиста ───

        private void CreatePlaylist_Click(object sender, MouseButtonEventArgs e)
        {
            PlaylistNameBox.Text = "";
            PlaylistDescBox.Text = "";
            PrivateToggle.IsChecked = false;
            _pendingCoverPath = null;
            ResetCoverPicker();
            CreatePlaylistModal.Visibility = Visibility.Visible;
        }

        private void CloseModal_Click(object sender, MouseButtonEventArgs e)
            => CreatePlaylistModal.Visibility = Visibility.Collapsed;

        private void PickCover_Click(object sender, MouseButtonEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Изображения|*.jpg;*.jpeg;*.png;*.webp" };
            if (dlg.ShowDialog() != true) return;

            _pendingCoverPath = FileStorage.CopyPlaylistCover(dlg.FileName);
            CoverPicker.Background = new ImageBrush(new BitmapImage(new Uri(FileStorage.ResolvePath(_pendingCoverPath))))
            { Stretch = Stretch.UniformToFill };
            if (CoverPicker.Child is StackPanel sp) sp.Visibility = Visibility.Collapsed;
        }

        private void ResetCoverPicker()
        {
            CoverPicker.Background = GradBrush("#2AE876", "#004D40");
            if (CoverPicker.Child is StackPanel sp) sp.Visibility = Visibility.Visible;
        }

        private void ConfirmCreatePlaylist_Click(object sender, RoutedEventArgs e)
        {
            var name = PlaylistNameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                PlaylistNameBox.BorderBrush = new SolidColorBrush(Color.FromRgb(232, 70, 42));
                return;
            }
            _vm.CreatePlaylistCommand.Execute((name, _pendingCoverPath, PrivateToggle.IsChecked == true));
            CreatePlaylistModal.Visibility = Visibility.Collapsed;
        }

        // ─── Рендер ───

        private void Render()
        {
            PlaylistsGridPanel.Children.Clear();
            EmptyState.Visibility = _vm.Shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            PlaylistsGridPanel.Visibility = _vm.Shown.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            foreach (var vm in _vm.Shown) PlaylistsGridPanel.Children.Add(BuildGridCard(vm));

            UpdateFilterPills();
        }

        private void UpdateFilterPills()
        {
            var dark = (Brush)Application.Current.TryFindResource("TextPrimary") ?? new SolidColorBrush(Color.FromRgb(26, 26, 24));
            var neutral = (Brush)Application.Current.TryFindResource("BgCard") ?? new SolidColorBrush(Color.FromRgb(240, 239, 235));
            FilterAll.Background = _vm.ActiveFilter == "all" ? dark : neutral;
            FilterOwn.Background = _vm.ActiveFilter == "own" ? dark : neutral;
            FilterSaved.Background = _vm.ActiveFilter == "saved" ? dark : neutral;
            PillFg(FilterAll, _vm.ActiveFilter == "all");
            PillFg(FilterOwn, _vm.ActiveFilter == "own");
            PillFg(FilterSaved, _vm.ActiveFilter == "saved");
        }

        private static void PillFg(Border b, bool active)
            => ((TextBlock)b.Child).Foreground = active ? Brushes.White : (Brush)Application.Current.Resources["TextSecondary"];

        private UIElement BuildGridCard(PlaylistViewModel vm)
        {
            var card = new Border
            {
                Width = 188,
                CornerRadius = new CornerRadius(16),
                Cursor = Cursors.Hand,
                Background = (Brush)Application.Current.Resources["BgCard"],
                Margin = new Thickness(0, 0, 14, 18),
                Padding = new Thickness(10, 10, 10, 14),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromArgb(38, 0, 0, 0), BlurRadius = 14, ShadowDepth = 2, Direction = 270, Opacity = 0.45
                }
            };
            card.MouseLeftButtonDown += (_, _) => _vm.OpenPlaylist(vm);

            var stack = new StackPanel();
            var coverWrap = new Grid { Height = 164, Margin = new Thickness(0, 0, 0, 12) };
            var coverBorder = new Border { CornerRadius = new CornerRadius(12), ClipToBounds = true };

            string coverFp = string.IsNullOrEmpty(vm.CoverPath) ? "" : FileStorage.ResolveImagePath(vm.CoverPath, "PlaylistCovers");
            if (!string.IsNullOrEmpty(coverFp) && File.Exists(coverFp))
                coverBorder.Background = new ImageBrush(new BitmapImage(new Uri(coverFp))) { Stretch = Stretch.UniformToFill };
            else
            {
                coverBorder.Background = (Brush?)Application.Current.TryFindResource("GreenGradientStyle") ?? GradBrush("#2AE876", "#004D40");
                coverBorder.Child = new Image
                {
                    Source = IconAssets.LoadBitmap("music_note.png"),
                    Width = 56, Height = 56,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            coverWrap.Children.Add(coverBorder);

            if (vm.IsPrivate)
                coverWrap.Children.Add(MakeIconBadge("lock.png",
                    HorizontalAlignment.Right, VerticalAlignment.Top, new Thickness(0, 8, 8, 0)));

            stack.Children.Add(coverWrap);

            stack.Children.Add(new TextBlock
            {
                Text = vm.Title, FontSize = 14, FontWeight = FontWeights.Bold,
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = (Brush)Application.Current.Resources["TextPrimary"],
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(2, 0, 2, 3)
            });

            string subtitle = $"{vm.TrackCount} {Pluralize(vm.TrackCount, "трек", "трека", "треков")}";
            if (!vm.IsOwned && vm.IsSaved) subtitle += "  ·  сохранён";

            stack.Children.Add(new TextBlock
            {
                Text = subtitle, FontSize = 11,
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = (Brush)Application.Current.Resources["TextSecondary"],
                Margin = new Thickness(2, 0, 2, 0)
            });

            card.Child = stack;
            return card;
        }

        private static Border MakeIconBadge(string iconFile, HorizontalAlignment ha, VerticalAlignment va, Thickness margin)
        {
            var b = new Border
            {
                Width = 26, Height = 26, CornerRadius = new CornerRadius(13),
                Background = new SolidColorBrush(Color.FromArgb(180, 26, 26, 24)),
                HorizontalAlignment = ha, VerticalAlignment = va, Margin = margin
            };
            b.Child = new Image
            {
                Source = IconAssets.LoadBitmap(iconFile),
                Width = 14, Height = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            return b;
        }

        private static string Pluralize(int n, string one, string few, string many)
        {
            int mod100 = n % 100;
            if (mod100 is >= 11 and <= 19) return many;
            return (n % 10) switch { 1 => one, 2 or 3 or 4 => few, _ => many };
        }

        private static LinearGradientBrush GradBrush(string c1, string c2) => new()
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop((Color)ColorConverter.ConvertFromString(c1), 0),
                new GradientStop((Color)ColorConverter.ConvertFromString(c2), 1),
            }
        };
    }
}
