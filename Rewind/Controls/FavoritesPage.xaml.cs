using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Rewind.Controls
{
    public partial class FavoritesPage : UserControl
    {
        // ───────────────────────────────────────────────
        //  Модель трека
        // ───────────────────────────────────────────────
        public class TrackModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Artist { get; set; } = "";
            public string Album { get; set; } = "";
            public string Genre { get; set; } = "";
            public string Duration { get; set; } = "0:00";
            public int DurationSec { get; set; }
            public string AddedDate { get; set; } = "";
            public bool IsLiked { get; set; } = true;
            public string GradStart { get; set; } = "#2AE876";
            public string GradEnd { get; set; } = "#004D40";
            public string Emoji { get; set; } = "🎵";
        }

        // ───────────────────────────────────────────────
        //  Состояние
        // ───────────────────────────────────────────────
        private List<TrackModel> _allTracks = new();
        private List<TrackModel> _shown = new();
        private int? _playingId = null;
        private string _activeGenre = "Все";
        private string _sortMode = "recent";  // "recent" | "az" | "artist" | "duration"

        private static readonly string[] SortModes = { "recent", "az", "artist", "duration" };
        private static readonly string[] SortLabels = { "Недавние", "А → Я", "Исполнитель", "Длительность" };

        // ───────────────────────────────────────────────
        //  Конструктор
        // ───────────────────────────────────────────────
        public FavoritesPage()
        {
            InitializeComponent();
            LoadTracks();
            BuildGenreFilters();
            Render();
        }

        // ───────────────────────────────────────────────
        //  Загрузка данных (заглушка)
        // ───────────────────────────────────────────────
        private void LoadTracks()
        {
            _allTracks = new List<TrackModel>
            {
                new() { Id=1,  Title="Blinding Lights",    Artist="The Weeknd",      Album="After Hours",       Genre="Pop",      Duration="3:20", DurationSec=200, AddedDate="Сегодня",   GradStart="#E8462A", GradEnd="#FF8C42", Emoji="🔥" },
                new() { Id=2,  Title="Levitating",         Artist="Dua Lipa",        Album="Future Nostalgia",  Genre="Pop",      Duration="3:23", DurationSec=203, AddedDate="Вчера",     GradStart="#F093FB", GradEnd="#F5576C", Emoji="✨" },
                new() { Id=3,  Title="As It Was",          Artist="Harry Styles",    Album="Harry's House",     Genre="Indie",    Duration="2:37", DurationSec=157, AddedDate="2 дня назад", GradStart="#667EEA", GradEnd="#764BA2", Emoji="🌙" },
                new() { Id=4,  Title="Stay",               Artist="The Kid LAROI",   Album="F*CK LOVE 3",       Genre="Hip-Hop",  Duration="2:21", DurationSec=141, AddedDate="3 дня назад", GradStart="#11998e", GradEnd="#38ef7d", Emoji="💚" },
                new() { Id=5,  Title="Anti-Hero",          Artist="Taylor Swift",    Album="Midnights",         Genre="Pop",      Duration="3:20", DurationSec=200, AddedDate="Неделю назад", GradStart="#43C6AC", GradEnd="#191654", Emoji="🌊" },
                new() { Id=6,  Title="Bad Habit",          Artist="Steve Lacy",      Album="Gemini Rights",     Genre="R&B",      Duration="3:52", DurationSec=232, AddedDate="Неделю назад", GradStart="#FFB347", GradEnd="#FF6B6B", Emoji="⚡" },
                new() { Id=7,  Title="Heat Waves",         Artist="Glass Animals",   Album="Dreamland",         Genre="Indie",    Duration="3:59", DurationSec=239, AddedDate="2 нед. назад", GradStart="#2AE876", GradEnd="#004D40", Emoji="🌿" },
                new() { Id=8,  Title="Unholy",             Artist="Sam Smith",       Album="Gloria",            Genre="Pop",      Duration="2:37", DurationSec=157, AddedDate="2 нед. назад", GradStart="#E8462A", GradEnd="#FF8C42", Emoji="🔥" },
                new() { Id=9,  Title="Flowers",            Artist="Miley Cyrus",     Album="Endless Summer",    Genre="Pop",      Duration="3:21", DurationSec=201, AddedDate="Месяц назад",  GradStart="#F093FB", GradEnd="#F5576C", Emoji="🌸" },
                new() { Id=10, Title="Escapism",           Artist="RAYE",            Album="My 21st Century",   Genre="R&B",      Duration="3:44", DurationSec=224, AddedDate="Месяц назад",  GradStart="#667EEA", GradEnd="#764BA2", Emoji="🎶" },
                new() { Id=11, Title="Calm Down",          Artist="Rema",            Album="Rave & Roses",      Genre="Afrobeats",Duration="3:38", DurationSec=218, AddedDate="Месяц назад",  GradStart="#11998e", GradEnd="#38ef7d", Emoji="🌴" },
                new() { Id=12, Title="Creepin'",           Artist="Metro Boomin",    Album="Heroes & Villains", Genre="Hip-Hop",  Duration="3:55", DurationSec=235, AddedDate="Давно",        GradStart="#1A1A18", GradEnd="#333330", Emoji="🌑" },
            };

            UpdateStats();
        }

        // ───────────────────────────────────────────────
        //  Обновление статистики в баннере
        // ───────────────────────────────────────────────
        private void UpdateStats()
        {
            StatTracksCount.Text = _allTracks.Count.ToString();
            StatArtistsCount.Text = _allTracks.Select(t => t.Artist).Distinct().Count().ToString();

            int totalSec = _allTracks.Sum(t => t.DurationSec);
            int h = totalSec / 3600;
            int m = (totalSec % 3600) / 60;
            StatDuration.Text = h > 0 ? $"{h}ч {m}м" : $"{m}м";
        }

        // ───────────────────────────────────────────────
        //  Фильтры жанров
        // ───────────────────────────────────────────────
        private void BuildGenreFilters()
        {
            GenreFilters.Children.Clear();

            var genres = new[] { "Все" }
                .Concat(_allTracks.Select(t => t.Genre).Distinct().OrderBy(g => g))
                .ToArray();

            foreach (var genre in genres)
            {
                var isActive = genre == _activeGenre;
                var pill = new Border
                {
                    CornerRadius = new CornerRadius(20),
                    Padding = new Thickness(16, 7, 16, 7),
                    Margin = new Thickness(0, 0, 8, 0),
                    Cursor = Cursors.Hand,
                    Background = isActive
                        ? new SolidColorBrush(Color.FromRgb(26, 26, 24))
                        : new SolidColorBrush(Color.FromRgb(240, 239, 235)),
                    Tag = genre
                };
                pill.Child = new TextBlock
                {
                    Text = genre,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                    Foreground = isActive ? Brushes.White
                                          : (Brush)Application.Current.Resources["TextSecondary"]
                };
                pill.MouseLeftButtonDown += (s, e) =>
                {
                    _activeGenre = (string)((Border)s).Tag;
                    BuildGenreFilters();
                    Render();
                };
                GenreFilters.Children.Add(pill);
            }
        }

        // ───────────────────────────────────────────────
        //  Рендер списка
        // ───────────────────────────────────────────────
        private void Render()
        {
            var query = SearchBox.Text.Trim().ToLower();

            _shown = _allTracks
                .Where(t => _activeGenre == "Все" || t.Genre == _activeGenre)
                .Where(t => string.IsNullOrEmpty(query) ||
                            t.Title.ToLower().Contains(query) ||
                            t.Artist.ToLower().Contains(query) ||
                            t.Album.ToLower().Contains(query))
                .ToList();

            _shown = _sortMode switch
            {
                "az" => _shown.OrderBy(t => t.Title).ToList(),
                "artist" => _shown.OrderBy(t => t.Artist).ToList(),
                "duration" => _shown.OrderBy(t => t.DurationSec).ToList(),
                _ => _shown  // "recent" — порядок из загрузки
            };

            TracksContainer.Children.Clear();

            EmptyState.Visibility = _shown.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;

            for (int i = 0; i < _shown.Count; i++)
                TracksContainer.Children.Add(BuildTrackRow(_shown[i], i + 1));
        }

        // ───────────────────────────────────────────────
        //  Построение строки трека
        // ───────────────────────────────────────────────
        private UIElement BuildTrackRow(TrackModel t, int index)
        {
            bool isPlaying = _playingId == t.Id;

            var row = new Border
            {
                CornerRadius = new CornerRadius(14),
                Margin = new Thickness(0, 0, 0, 5),
                Cursor = Cursors.Hand,
                Background = isPlaying
                    ? (Brush)Application.Current.Resources["ActiveCardBackground"]
                    : new SolidColorBrush(Colors.White),
                BorderBrush = isPlaying
                    ? (Brush)Application.Current.Resources["AccentColor"]
                    : new SolidColorBrush(Color.FromRgb(224, 223, 217)),
                BorderThickness = new Thickness(isPlaying ? 1.5 : 1),
                Tag = t.Id
            };

            row.MouseLeftButtonDown += (s, e) => PlayTrack(t);
            row.MouseEnter += TrackRow_MouseEnter;
            row.MouseLeave += TrackRow_MouseLeave;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

            // Номер / анимация волн
            var numBorder = new Border
            {
                Width = 32,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (isPlaying)
            {
                numBorder.Child = new TextBlock
                {
                    Text = "▶",
                    FontSize = 14,
                    Foreground = (Brush)Application.Current.Resources["AccentColor"],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            else
            {
                numBorder.Child = new TextBlock
                {
                    Text = index.ToString(),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)Application.Current.Resources["TextMuted"],
                    FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            Grid.SetColumn(numBorder, 0);

            // Трек: обложка + название + исполнитель
            var trackInfo = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 0, 0) };
            Grid.SetColumn(trackInfo, 1);

            var thumb = new Border
            {
                Width = 42,
                Height = 42,
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 12, 0),
                Background = GradBrush(t.GradStart, t.GradEnd)
            };
            thumb.Child = new TextBlock
            {
                Text = t.Emoji,
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleStack.Children.Add(new TextBlock
            {
                Text = t.Title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = isPlaying
                    ? (Brush)Application.Current.Resources["AccentDark"]
                    : (Brush)Application.Current.Resources["TextPrimary"],
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 220
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = t.Artist,
                FontSize = 12,
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = (Brush)Application.Current.Resources["TextSecondary"],
                Margin = new Thickness(0, 3, 0, 0)
            });

            trackInfo.Children.Add(thumb);
            trackInfo.Children.Add(titleStack);

            // Альбом
            var albumText = new TextBlock
            {
                Text = t.Album,
                FontSize = 13,
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = (Brush)Application.Current.Resources["TextSecondary"],
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 150
            };
            Grid.SetColumn(albumText, 2);

            // Дата
            var dateText = new TextBlock
            {
                Text = t.AddedDate,
                FontSize = 12,
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = (Brush)Application.Current.Resources["TextMuted"],
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(dateText, 3);

            // Правая секция: длительность + лайк
            var rightPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(rightPanel, 4);

            var durText = new TextBlock
            {
                Text = t.Duration,
                FontSize = 13,
                FontFamily = (FontFamily)Application.Current.Resources["HeaderFont"],
                Foreground = (Brush)Application.Current.Resources["TextSecondary"],
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var heartBtn = new Border
            {
                Width = 30,
                Height = 30,
                CornerRadius = new CornerRadius(15),
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                Cursor = Cursors.Hand,
                Tag = t.Id
            };
            heartBtn.Child = new TextBlock
            {
                Text = "♥",
                FontSize = 15,
                Foreground = (Brush)Application.Current.Resources["AccentColor"],
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            heartBtn.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                UnlikeTrack(t.Id);
            };

            rightPanel.Children.Add(durText);
            rightPanel.Children.Add(heartBtn);

            grid.Children.Add(numBorder);
            grid.Children.Add(trackInfo);
            grid.Children.Add(albumText);
            grid.Children.Add(dateText);
            grid.Children.Add(rightPanel);

            row.Child = grid;
            return row;
        }

        // ───────────────────────────────────────────────
        //  Ховер-эффекты строки
        // ───────────────────────────────────────────────
        private void TrackRow_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border row && _playingId != (int?)row.Tag)
                row.Background = new SolidColorBrush(Color.FromRgb(245, 244, 240));
        }

        private void TrackRow_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border row && _playingId != (int?)row.Tag)
                row.Background = new SolidColorBrush(Colors.White);
        }

        // ───────────────────────────────────────────────
        //  Воспроизведение
        // ───────────────────────────────────────────────
        private void PlayTrack(TrackModel t)
        {
            _playingId = (_playingId == t.Id) ? null : (int?)t.Id;
            Render();

            // Здесь интеграция с PlayerBar:
            // BottomPlayerBar.SetTrack(t.Title, t.Artist, t.Duration);
            // BottomPlayerBar.Visibility = Visibility.Visible;
        }

        private void PlayAll_Click(object sender, MouseButtonEventArgs e)
        {
            if (_shown.Count == 0) return;
            PlayTrack(_shown[0]);
        }

        // ───────────────────────────────────────────────
        //  Снять лайк (анимация + удаление)
        // ───────────────────────────────────────────────
        private void UnlikeTrack(int id)
        {
            var track = _allTracks.FirstOrDefault(t => t.Id == id);
            if (track == null) return;

            // Ищем строку в UI
            Border? rowBorder = null;
            foreach (var child in TracksContainer.Children)
            {
                if (child is Border b && (int?)b.Tag == id)
                {
                    rowBorder = b;
                    break;
                }
            }

            if (rowBorder != null)
            {
                // Анимация исчезновения
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
                fadeOut.Completed += (s, e) =>
                {
                    _allTracks.Remove(track);
                    UpdateStats();
                    BuildGenreFilters();
                    Render();
                };
                rowBorder.BeginAnimation(OpacityProperty, fadeOut);
            }
            else
            {
                _allTracks.Remove(track);
                UpdateStats();
                BuildGenreFilters();
                Render();
            }
        }

        // ───────────────────────────────────────────────
        //  Поиск
        // ───────────────────────────────────────────────
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Render();

        // ───────────────────────────────────────────────
        //  Сортировка (цикл по режимам)
        // ───────────────────────────────────────────────
        private void ToggleSort_Click(object sender, MouseButtonEventArgs e)
        {
            int idx = Array.IndexOf(SortModes, _sortMode);
            _sortMode = SortModes[(idx + 1) % SortModes.Length];
            SortLabel.Text = SortLabels[(idx + 1) % SortLabels.Length];
            Render();
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
    }
}
