using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rewind.Controls
{
    public partial class PlaylistsPage : UserControl
    {
        // ───────────────────────────────────────────────
        //  Модель данных
        // ───────────────────────────────────────────────
        public class PlaylistModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public int TrackCount { get; set; }
            public bool IsPrivate { get; set; }
            public bool IsOwned { get; set; }   // true = мой, false = сохранённый
            public string GradStart { get; set; } = "#2AE876";
            public string GradEnd { get; set; } = "#004D40";
            public string Emoji { get; set; } = "🎵";
        }

        // ───────────────────────────────────────────────
        //  Состояние
        // ───────────────────────────────────────────────
        private List<PlaylistModel> _allPlaylists = new();
        private List<PlaylistModel> _shown = new();
        private bool _isGridView = true;
        private string _activeFilter = "all";   // "all" | "own" | "saved"

        // Цветовые схемы для новых плейлистов
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

        // ───────────────────────────────────────────────
        //  Конструктор
        // ───────────────────────────────────────────────
        public PlaylistsPage()
        {
            InitializeComponent();
            LoadPlaylists();
            Render();
        }

        // ───────────────────────────────────────────────
        //  Загрузка данных (заглушка — замените на БД/API)
        // ───────────────────────────────────────────────
        private void LoadPlaylists()
        {
            _allPlaylists = new List<PlaylistModel>
            {
                new() { Id=1, Title="Для работы",      Description="Фокус и продуктивность",
                        TrackCount=24, IsPrivate=false, IsOwned=true,
                        GradStart="#667EEA", GradEnd="#764BA2", Emoji="💻" },
                new() { Id=2, Title="Утренний заряд",  Description="Бодрость с первых нот",
                        TrackCount=18, IsPrivate=false, IsOwned=true,
                        GradStart="#F093FB", GradEnd="#F5576C", Emoji="☀️" },
                new() { Id=3, Title="Вечерний чилл",   Description="Расслабься после дня",
                        TrackCount=31, IsPrivate=true,  IsOwned=true,
                        GradStart="#43C6AC", GradEnd="#191654", Emoji="🌙" },
                new() { Id=4, Title="Хиты 2000-х",     Description="Ностальгия",
                        TrackCount=56, IsPrivate=false, IsOwned=false,
                        GradStart="#E8462A", GradEnd="#FF8C42", Emoji="📼" },
                new() { Id=5, Title="Тренировка",      Description="Максимальная энергия",
                        TrackCount=42, IsPrivate=false, IsOwned=true,
                        GradStart="#11998e", GradEnd="#38ef7d", Emoji="💪" },
                new() { Id=6, Title="Акустика",        Description="Живой звук",
                        TrackCount=15, IsPrivate=false, IsOwned=false,
                        GradStart="#FFB347", GradEnd="#FF6B6B", Emoji="🎸" },
            };

            FavoritesCountText.Text = "128 треков";
        }

        // ───────────────────────────────────────────────
        //  Рендер
        // ───────────────────────────────────────────────
        private void Render()
        {
            // Применяем фильтр поиска + фильтр категории
            var query = SearchBox.Text.Trim().ToLower();
            _shown = _allPlaylists
                .Where(p => _activeFilter == "all" ||
                            (_activeFilter == "own" && p.IsOwned) ||
                            (_activeFilter == "saved" && !p.IsOwned))
                .Where(p => string.IsNullOrEmpty(query) ||
                            p.Title.ToLower().Contains(query) ||
                            p.Description.ToLower().Contains(query))
                .ToList();

            PlaylistsGridPanel.Children.Clear();
            PlaylistsListPanel.Children.Clear();

            EmptyState.Visibility = _shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            PlaylistsGridPanel.Visibility = _isGridView && _shown.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            PlaylistsListPanel.Visibility = !_isGridView && _shown.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

          
        }

        // ───────────────────────────────────────────────
        //  Построение карточки (сетка)
        // ───────────────────────────────────────────────
        private UIElement BuildGridCard(PlaylistModel pl)
        {
            var card = new Border
            {
                Width = 175,
                Height = 210,
                CornerRadius = new CornerRadius(18),
                Margin = new Thickness(0, 0, 14, 14),
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(Colors.White),
                Tag = pl.Id
            };

            card.MouseLeftButtonDown += (s, e) => OpenPlaylist(pl);

            var inner = new Grid();

            // Обложка
            var coverBorder = new Border
            {
                Height = 140,
                CornerRadius = new CornerRadius(14, 14, 0, 0),
                Background = GradBrush(pl.GradStart, pl.GradEnd),
                VerticalAlignment = VerticalAlignment.Top
            };
            var emojiBlock = new TextBlock
            {
                Text = pl.Emoji,
                FontSize = 44,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            coverBorder.Child = emojiBlock;

            // Если приватный — замок
            if (pl.IsPrivate)
            {
                var lockPanel = new Border
                {
                    Width = 26,
                    Height = 26,
                    CornerRadius = new CornerRadius(13),
                    Background = new SolidColorBrush(Color.FromArgb(180, 26, 26, 24)),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 8, 8, 0)
                };
                lockPanel.Child = new TextBlock
                {
                    Text = "🔒",
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var coverGrid = new Grid();
                coverGrid.Children.Add(emojiBlock);
                coverGrid.Children.Add(lockPanel);
                coverBorder.Child = coverGrid;
            }

            // Текст снизу карточки
            var infoPanel = new StackPanel
            {
                Margin = new Thickness(12, 8, 12, 10),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            infoPanel.Children.Add(new TextBlock
            {
                Text = pl.Title,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)Application.Current.Resources["TextPrimary"],
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"]
            });
            infoPanel.Children.Add(new TextBlock
            {
                Text = $"{pl.TrackCount} треков",
                FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["TextSecondary"],
                Margin = new Thickness(0, 3, 0, 0),
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"]
            });

            inner.Children.Add(coverBorder);
            inner.Children.Add(new Border
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Child = infoPanel
            });

            card.Child = inner;
            return card;
        }

        // ───────────────────────────────────────────────
        //  Построение строки (список)
        // ───────────────────────────────────────────────
        private UIElement BuildListRow(PlaylistModel pl)
        {
            var row = new Border
            {
                Style = (Style)Application.Current.Resources["PlaylistCard"],
                Tag = pl.Id,
                Cursor = Cursors.Hand
            };
            row.MouseLeftButtonDown += (s, e) => OpenPlaylist(pl);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Мини-обложка
            var thumb = new Border
            {
                Width = 52,
                Height = 52,
                CornerRadius = new CornerRadius(12),
                Background = GradBrush(pl.GradStart, pl.GradEnd),
                Margin = new Thickness(0, 0, 14, 0)
            };
            Grid.SetColumn(thumb, 0);
            thumb.Child = new TextBlock
            {
                Text = pl.Emoji,
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Инфо
            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(info, 1);
            info.Children.Add(new TextBlock
            {
                Text = pl.Title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextPrimary"],
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"]
            });
            info.Children.Add(new TextBlock
            {
                Text = $"{pl.TrackCount} треков  •  {(pl.IsPrivate ? "Приватный" : "Публичный")}",
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextSecondary"],
                Margin = new Thickness(0, 3, 0, 0),
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"]
            });

            // Кнопка воспроизведения
            var playBtn = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(18),
                Background = GradBrush(pl.GradStart, pl.GradEnd),
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

        // ───────────────────────────────────────────────
        //  Вспомогательный метод градиента
        // ───────────────────────────────────────────────
        private static LinearGradientBrush GradBrush(string c1, string c2)
        {
            return new LinearGradientBrush
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

        // ───────────────────────────────────────────────
        //  Открыть плейлист
        // ───────────────────────────────────────────────
        private void OpenPlaylist(PlaylistModel pl)
        {
            // Здесь навигация — например:
            // NavigationHelper.Navigate(new PlaylistDetailPage(pl.Id));
            MessageBox.Show($"Открываем: {pl.Title}", "Rewind");
        }

        // ───────────────────────────────────────────────
        //  Переключение вида
        // ───────────────────────────────────────────────
        private void SwitchToGrid_Click(object sender, MouseButtonEventArgs e)
        {
            _isGridView = true;
            SetViewButtons();
            Render();
        }

        private void SwitchToList_Click(object sender, MouseButtonEventArgs e)
        {
            _isGridView = false;
            SetViewButtons();
            Render();
        }

        private void SetViewButtons()
        {
            var accent = (Color)ColorConverter.ConvertFromString("#2AE876");
            var neutral = (Color)ColorConverter.ConvertFromString("#F0EFEB");

            ViewGrid.Background = new SolidColorBrush(_isGridView ? accent : neutral);
            ViewList.Background = new SolidColorBrush(_isGridView ? neutral : accent);

            var gridTb = (TextBlock)ViewGrid.Child;
            var listTb = (TextBlock)ViewList.Child;
            gridTb.Foreground = _isGridView ? Brushes.White : (Brush)Application.Current.Resources["TextSecondary"];
            listTb.Foreground = _isGridView ? (Brush)Application.Current.Resources["TextSecondary"] : Brushes.White;
        }

        // ───────────────────────────────────────────────
        //  Фильтры
        // ───────────────────────────────────────────────
        private void FilterAll_Click(object sender, MouseButtonEventArgs e) => SetFilter("all");
        private void FilterOwn_Click(object sender, MouseButtonEventArgs e) => SetFilter("own");
        private void FilterSaved_Click(object sender, MouseButtonEventArgs e) => SetFilter("saved");

        private void SetFilter(string filter)
        {
            _activeFilter = filter;

            var dark = Color.FromRgb(26, 26, 24);
            var neutral = Color.FromRgb(240, 239, 235);

            FilterAll.Background = new SolidColorBrush(filter == "all" ? dark : neutral);
            FilterOwn.Background = new SolidColorBrush(filter == "own" ? dark : neutral);
            FilterSaved.Background = new SolidColorBrush(filter == "saved" ? dark : neutral);

            var allText = (TextBlock)FilterAll.Child;
            var ownText = (TextBlock)FilterOwn.Child;
            var savedText = (TextBlock)FilterSaved.Child;

            allText.Foreground = filter == "all" ? Brushes.White : (Brush)Application.Current.Resources["TextSecondary"];
            ownText.Foreground = filter == "own" ? Brushes.White : (Brush)Application.Current.Resources["TextSecondary"];
            savedText.Foreground = filter == "saved" ? Brushes.White : (Brush)Application.Current.Resources["TextSecondary"];

            Render();
        }

        // ───────────────────────────────────────────────
        //  Поиск
        // ───────────────────────────────────────────────
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Render();

        // ───────────────────────────────────────────────
        //  Любимые
        // ───────────────────────────────────────────────
        private void FavoritesPlaylist_Click(object sender, MouseButtonEventArgs e)
        {
            // NavigationHelper.Navigate(new FavoritesPage());
            MessageBox.Show("Открываем: Любимые треки", "Rewind");
        }

        // ───────────────────────────────────────────────
        //  Модал создания плейлиста
        // ───────────────────────────────────────────────
        private void CreatePlaylist_Click(object sender, MouseButtonEventArgs e)
        {
            PlaylistNameBox.Text = "";
            PlaylistDescBox.Text = "";
            PrivateToggle.IsChecked = false;
            CreatePlaylistModal.Visibility = Visibility.Visible;
        }

        private void CloseModal_Click(object sender, MouseButtonEventArgs e)
        {
            CreatePlaylistModal.Visibility = Visibility.Collapsed;
        }

        private void PickCover_Click(object sender, MouseButtonEventArgs e)
        {
            // Здесь можно открыть FileDialog для выбора обложки
        }

        private void ConfirmCreatePlaylist_Click(object sender, RoutedEventArgs e)
        {
            var name = PlaylistNameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                PlaylistNameBox.BorderBrush = new SolidColorBrush(Color.FromRgb(232, 70, 42));
                return;
            }

            var scheme = _colorSchemes[_allPlaylists.Count % _colorSchemes.Length];

            var newPl = new PlaylistModel
            {
                Id = _allPlaylists.Count + 1,
                Title = name,
                Description = PlaylistDescBox.Text.Trim(),
                TrackCount = 0,
                IsPrivate = PrivateToggle.IsChecked == true,
                IsOwned = true,
                GradStart = scheme.Start,
                GradEnd = scheme.End,
                Emoji = scheme.Emoji
            };

            _allPlaylists.Insert(0, newPl);

            CreatePlaylistModal.Visibility = Visibility.Collapsed;
            SetFilter("own");   // показываем вкладку «Мои»
        }
    }
}
