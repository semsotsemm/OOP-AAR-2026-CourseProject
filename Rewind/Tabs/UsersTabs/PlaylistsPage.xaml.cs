using Microsoft.Win32;
using Rewind.Helpers;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rewind.Tabs.UsersTabs
{
    public partial class PlaylistsPage : UserControl
    {
        // ─────────────────────────────────────────────
        //  Внутренняя VM поверх DbManager.Playlist
        // ─────────────────────────────────────────────
        private class PlaylistVM
        {
            public Playlist Source { get; set; }
            public string Title => Source.Title;
            public int TrackCount => Source.PlaylistTracks?.Count ?? 0;
            // DB-backed counts (use try-catch to avoid failures during render)
            public int LikesCount
            {
                get { try { return SavedPlaylistService.GetSavedCount(Source.PlaylistID); } catch { return 0; } }
            }
            public int ListensCount
            {
                get { try { return PlaylistListenService.GetListenerCount(Source.PlaylistID); } catch { return 0; } }
            }
            public bool IsPrivate => Source.IsPrivate;
            public bool IsOwned { get; set; }
            public string GradStart { get; set; } = "#2AE876";
            public string GradEnd { get; set; } = "#004D40";
            public string Emoji { get; set; } = "🎵";
            public string? CoverPath => Source.CoverPath;
        }

        // ─────────────────────────────────────────────
        //  Состояние
        // ─────────────────────────────────────────────
        private List<PlaylistVM> _all = new();
        private List<PlaylistVM> _shown = new();
        private bool _isGridView = true;
        private string _activeFilter = "all";
        private string? _pendingCoverPath;

        private readonly (string Start, string End, string Emoji)[] _colorSchemes =
        {
            ("#2AE876", "#004D40", "🎵"),
            ("#667EEA", "#764BA2", "🎶"),
            ("#F093FB", "#F5576C", "🎸"),
            ("#11998e", "#38ef7d", "🎹"),
            ("#E8462A", "#FF8C42", "🔥"),
            ("#43C6AC", "#191654", "🌊"),
            ("#FFB347", "#FF6B6B", "⚡"),
        };

        // ─────────────────────────────────────────────
        //  Конструктор
        // ─────────────────────────────────────────────
        public PlaylistsPage()
        {
            InitializeComponent();
            LoadPlaylists();
            Render();

            PlaylistListenService.OnPlaylistListenChanged += OnPlaylistStatsChanged;
            SavedPlaylistService.OnPlaylistSavedChanged += OnPlaylistStatsChanged;
            Unloaded += (_, _) =>
            {
                PlaylistListenService.OnPlaylistListenChanged -= OnPlaylistStatsChanged;
                SavedPlaylistService.OnPlaylistSavedChanged -= OnPlaylistStatsChanged;
            };
        }

        private void OnPlaylistStatsChanged(int playlistId)
        {
            Dispatcher.Invoke(() => Render());
        }

        // ─────────────────────────────────────────────
        //  Загрузка из кеша сессии + публичные чужие
        // ─────────────────────────────────────────────
        private void LoadPlaylists()
        {
            _all.Clear();

            foreach (var pl in Session.CachedPlaylists)
                _all.Add(MakeVM(pl, owned: true));

            try
            {
                var publicOthers = PlaylistService.GetPublicPlaylists(excludeUserId: Session.UserId);
                foreach (var pl in publicOthers)
                    _all.Add(MakeVM(pl, owned: false));
            }
            catch { /* нет соединения — пропускаем */ }
        }

        private PlaylistVM MakeVM(Playlist pl, bool owned)
        {
            var scheme = _colorSchemes[Math.Abs(pl.PlaylistID) % _colorSchemes.Length];
            return new PlaylistVM
            {
                Source = pl,
                IsOwned = owned,
                GradStart = scheme.Start,
                GradEnd = scheme.End,
                Emoji = scheme.Emoji
            };
        }

        // ─────────────────────────────────────────────
        //  Рендер
        // ─────────────────────────────────────────────
        private void Render()
        {
            var query = SearchBox.Text.Trim().ToLower();
            _shown = _all
                .Where(p => _activeFilter == "all" ||
                            (_activeFilter == "own" && p.IsOwned) ||
                            (_activeFilter == "saved" && !p.IsOwned))
                .Where(p => string.IsNullOrEmpty(query) || p.Title.ToLower().Contains(query))
                .ToList();

            PlaylistsGridPanel.Children.Clear();

            EmptyState.Visibility = _shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            PlaylistsGridPanel.Visibility = _isGridView && _shown.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            foreach (var vm in _shown)
            {
                PlaylistsGridPanel.Children.Add(BuildGridCard(vm));
            }
        }

        // ─────────────────────────────────────────────
        //  Карточка-сетка
        // ─────────────────────────────────────────────
        private UIElement BuildGridCard(PlaylistVM vm)
        {
            var card = new Border
            {
                Width = 175,
                Height = 200, 
                CornerRadius = new CornerRadius(18),
                Cursor = Cursors.Hand,
                Background = (Brush)Application.Current.Resources["BgCard"],
                ClipToBounds = true
            };
            card.MouseLeftButtonDown += (_, _) => OpenPlaylist(vm);

            var inner = new Grid();

            // Обложка
            var coverBorder = new Border
            {
                Height = 130,
                CornerRadius = new CornerRadius(14), // Скругляем все углы или только верхние
                Margin = new Thickness(8, 8, 8, 20),   // Добавляем небольшой отступ сверху и по бокам
                VerticalAlignment = VerticalAlignment.Top,
                ClipToBounds = true
            };

            if (!string.IsNullOrEmpty(vm.CoverPath) && File.Exists(vm.CoverPath))
                coverBorder.Background = new ImageBrush(new BitmapImage(new Uri(vm.CoverPath))) { Stretch = Stretch.UniformToFill };
            else
            {
                coverBorder.Background = GradBrush(vm.GradStart, vm.GradEnd);
                coverBorder.Child = new TextBlock
                {
                    Text = vm.Emoji,
                    FontSize = 44,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            inner.Children.Add(coverBorder);

            // Значок приватности
            if (vm.IsPrivate)
                inner.Children.Add(MakeBadge("🔒", HorizontalAlignment.Right, VerticalAlignment.Top, new Thickness(0, 8, 8, 0)));
            if (!vm.IsOwned)
                inner.Children.Add(MakeBadge("👤", HorizontalAlignment.Left, VerticalAlignment.Top, new Thickness(8, 8, 0, 0)));

            // Текст внизу
            var info = new StackPanel
            {
                Margin = new Thickness(12, 0, 12, 20),
                VerticalAlignment = VerticalAlignment.Top
            };
            info.Children.Add(new TextBlock
            {
                Text = vm.Title,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = (Brush)Application.Current.Resources["TextPrimary"],
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            info.Children.Add(new TextBlock
            {
                Text = $"{vm.TrackCount} треков  •  ♥ {vm.LikesCount}  •  ▶ {vm.ListensCount}",
                FontSize = 11,
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = (Brush)Application.Current.Resources["TextSecondary"],
                Margin = new Thickness(0, 3, 0, 0)
            });
            inner.Children.Add(new Border { VerticalAlignment = VerticalAlignment.Bottom, Child = info });

            card.Child = inner;
            return card;
        }

        private static Border MakeBadge(string emoji, HorizontalAlignment ha, VerticalAlignment va, Thickness margin)
        {
            var b = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                Background = new SolidColorBrush(Color.FromArgb(180, 26, 26, 24)),
                HorizontalAlignment = ha,
                VerticalAlignment = va,
                Margin = margin
            };
            b.Child = new TextBlock
            {
                Text = emoji,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            return b;
        }

        // ─────────────────────────────────────────────
        //  Строка-список
        // ─────────────────────────────────────────────
        private UIElement BuildListRow(PlaylistVM vm)
        {
            var row = new Border
            {
                Style = (Style)Application.Current.Resources["PlaylistCard"],
                Cursor = Cursors.Hand
            };
            row.MouseLeftButtonDown += (_, _) => OpenPlaylist(vm);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var thumb = new Border
            {
                Width = 52,
                Height = 52,
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 14, 0),
                ClipToBounds = true
            };
            Grid.SetColumn(thumb, 0);

            if (!string.IsNullOrEmpty(vm.CoverPath) && File.Exists(vm.CoverPath))
                thumb.Background = new ImageBrush(new BitmapImage(new Uri(vm.CoverPath))) { Stretch = Stretch.UniformToFill };
            else
            {
                thumb.Background = GradBrush(vm.GradStart, vm.GradEnd);
                thumb.Child = new TextBlock
                {
                    Text = vm.Emoji,
                    FontSize = 22,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(info, 1);
            info.Children.Add(new TextBlock
            {
                Text = vm.Title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = (Brush)Application.Current.Resources["TextPrimary"]
            });
            var meta = $"{vm.TrackCount} треков  •  {(vm.IsPrivate ? "🔒 Приватный" : "Публичный")}  •  ♥ {vm.LikesCount}  •  ▶ {vm.ListensCount}";
            if (!vm.IsOwned) meta += "  •  👤";
            info.Children.Add(new TextBlock
            {
                Text = meta,
                FontSize = 12,
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = (Brush)Application.Current.Resources["TextSecondary"],
                Margin = new Thickness(0, 3, 0, 0)
            });

            var playBtn = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(18),
                Background = GradBrush(vm.GradStart, vm.GradEnd),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(playBtn, 2);
            playBtn.Child = new TextBlock
            {
                Text = "▶",
                FontSize = 13,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            grid.Children.Add(thumb);
            grid.Children.Add(info);
            grid.Children.Add(playBtn);
            row.Child = grid;
            return row;
        }

        // ─────────────────────────────────────────────
        //  Открыть плейлист
        // ─────────────────────────────────────────────
        private void OpenPlaylist(PlaylistVM vm)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.OpenPlaylistDetails(vm.Source);
        }

        // ─────────────────────────────────────────────
        //  Переключение вида
        // ─────────────────────────────────────────────
        private void SwitchToGrid_Click(object sender, MouseButtonEventArgs e) { _isGridView = true; SetViewButtons(); Render(); }
        private void SwitchToList_Click(object sender, MouseButtonEventArgs e) { _isGridView = false; SetViewButtons(); Render(); }

        private void SetViewButtons()
        {
            var accent = Color.FromRgb(42, 232, 118);
            var neutral = Color.FromRgb(240, 239, 235);
        }

        // ─────────────────────────────────────────────
        //  Фильтры
        // ─────────────────────────────────────────────
        private void FilterAll_Click(object sender, MouseButtonEventArgs e) => SetFilter("all");
        private void FilterOwn_Click(object sender, MouseButtonEventArgs e) => SetFilter("own");
        private void FilterSaved_Click(object sender, MouseButtonEventArgs e) => SetFilter("saved");

        private void SetFilter(string f)
        {
            _activeFilter = f;

            var dark = (Brush)Application.Current.TryFindResource("TextPrimary") ?? new SolidColorBrush(Color.FromRgb(26, 26, 24));

            var neutral = (Brush)Application.Current.TryFindResource("BgCard") ?? new SolidColorBrush(Color.FromRgb(240, 239, 235));

            FilterAll.Background = f == "all" ? dark : neutral;
            FilterOwn.Background = f == "own" ? dark : neutral;
            FilterSaved.Background = f == "saved" ? dark : neutral;

            PillFg(FilterAll, f == "all");
            PillFg(FilterOwn, f == "own");
            PillFg(FilterSaved, f == "saved");

            Render();
        }

        private static void PillFg(Border b, bool active)
            => ((TextBlock)b.Child).Foreground = active ? Brushes.White : (Brush)Application.Current.Resources["TextSecondary"];

        // ─────────────────────────────────────────────
        //  Поиск
        // ─────────────────────────────────────────────
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Render();

        // ─────────────────────────────────────────────
        //  Любимые
        // ─────────────────────────────────────────────
        private void FavoritesPlaylist_Click(object sender, MouseButtonEventArgs e)
            => MessageBox.Show("Открываем: Любимые треки", "Rewind");

        // ─────────────────────────────────────────────
        //  Модал создания
        // ─────────────────────────────────────────────
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

            bool isPrivate = PrivateToggle.IsChecked == true;

            // Создаём в кеше сессии — в БД уйдёт при FlushToDatabase()
            var newPl = Session.CreatePlaylist(name, _pendingCoverPath, isPrivate);

            var scheme = _colorSchemes[_all.Count % _colorSchemes.Length];
            _all.Insert(0, new PlaylistVM
            {
                Source = newPl,
                IsOwned = true,
                GradStart = scheme.Start,
                GradEnd = scheme.End,
                Emoji = scheme.Emoji
            });

            CreatePlaylistModal.Visibility = Visibility.Collapsed;
            SetFilter("own");
        }

        // ─────────────────────────────────────────────
        //  Вспомогательный градиент
        // ─────────────────────────────────────────────
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
