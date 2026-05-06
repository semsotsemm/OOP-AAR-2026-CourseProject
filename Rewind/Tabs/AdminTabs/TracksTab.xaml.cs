using Rewind.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rewind.Tabs.AdminTabs
{
    public partial class TracksTab : UserControl, IAdminTab
    {
        private sealed class TrackRow
        {
            public int Index { get; set; }
            public int TrackId { get; set; }
            public string Title { get; set; } = "";
            public string Artist { get; set; } = "";
            public string UploadDate { get; set; } = "";
            public int PlayCount { get; set; }
            public int LikesCount { get; set; }
            public string Duration { get; set; } = "";
            public string PublishStatus { get; set; } = "Published";
            public string? CoverPath { get; set; }
        }

        private List<TrackRow> _allTracks = new();
        private string _statusFilter = "All"; // All / Published / Pending / Banned

        public TracksTab()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadTracks();
        }

        public void Refresh() => LoadTracks();

        private void LoadTracks()
        {
            try
            {
                using var db = new AppDbContext();

                var tracks = db.Tracks
                    .Join(db.Users, t => t.ArtistID, u => u.UserId,
                          (t, u) => new { t.TrackID, t.Title, t.Duration, t.UploadDate, t.ArtistID, ArtistName = u.Nickname, t.PublishStatus, t.CoverPath })
                    .OrderByDescending(t => t.TrackID)
                    .ToList();

                var statsMap = db.Statistics.ToDictionary(s => s.TrackID);

                    _allTracks = tracks.Select((t, idx) => new TrackRow
                    {
                        Index = idx + 1,
                        TrackId = t.TrackID,
                        Title = t.Title,
                        Artist = t.ArtistName,
                        UploadDate = t.UploadDate.ToString("dd.MM.yyyy"),
                        PlayCount = statsMap.TryGetValue(t.TrackID, out var st) ? st.PlayCount : 0,
                        LikesCount = statsMap.TryGetValue(t.TrackID, out var st2) ? st2.LikesCount : 0,
                        Duration = FormatDuration(t.Duration),
                        PublishStatus = t.PublishStatus,
                        CoverPath = t.CoverPath
                    }).ToList();

                RenderTracks(_allTracks);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TracksTab] {ex.Message}");
            }
        }

        private void RenderTracks(List<TrackRow> tracks)
        {
            TracksContainer.Children.Clear();

            var textPrimary = GetBrush("TextPrimary", Color.FromRgb(26, 26, 24));
            var textSecondary = GetBrush("TextSecondary", Color.FromRgb(136, 136, 128));
            var bgCard = GetBrush("BgCard", Colors.White);
            var bgHover = GetBrush("BgCardHover", Color.FromRgb(240, 239, 235));
            var accentBrush = GetBrush("AccentColor", Color.FromRgb(42, 232, 118));

            foreach (var track in tracks)
            {
                var rowBg = track.PublishStatus == "Pending" ? new SolidColorBrush(Color.FromArgb(40, 255, 200, 0))
                           : track.Index % 2 == 0 ? bgHover : bgCard;

                var rowBorder = new Border
                {
                    Background = rowBg,
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(0, 12, 0, 12),
                    Margin = new Thickness(0, 0, 0, 2)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

                // Index
                var indexText = new TextBlock
                {
                    Text = track.Index.ToString(),
                    FontSize = 12, Foreground = textSecondary,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(indexText, 0);

                // Title (cover + name + duration)
                var titleGrid = new Grid { Margin = new Thickness(8, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
                titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
                titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var coverBorder = new Border
                {
                    Width = 40, Height = 40,
                    CornerRadius = new CornerRadius(6),
                    Background = GetBrush("BgCardHover", Color.FromRgb(240, 239, 235)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                if (!string.IsNullOrEmpty(track.CoverPath))
                {
                    try
                    {
                        string fullPath = FileStorage.ResolveImagePath(track.CoverPath);
                        if (File.Exists(fullPath))
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.UriSource = new Uri(fullPath);
                            bmp.DecodePixelWidth = 40;
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.EndInit();
                            bmp.Freeze();
                            coverBorder.Child = new Image { Source = bmp, Stretch = Stretch.UniformToFill };
                        }
                    }
                    catch { }
                }
                Grid.SetColumn(coverBorder, 0);

                var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                var titleText = new TextBlock
                {
                    Text = track.Title,
                    FontSize = 13, FontWeight = FontWeights.SemiBold,
                    Foreground = textPrimary,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                var durationText = new TextBlock
                {
                    Text = track.Duration,
                    FontSize = 11, Foreground = textSecondary
                };
                titleStack.Children.Add(titleText);
                titleStack.Children.Add(durationText);
                Grid.SetColumn(titleStack, 1);

                titleGrid.Children.Add(coverBorder);
                titleGrid.Children.Add(titleStack);
                Grid.SetColumn(titleGrid, 1);

                // Artist
                var artistText = new TextBlock
                {
                    Text = track.Artist,
                    FontSize = 12, Foreground = textSecondary,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(artistText, 2);

                // Upload date
                var dateText = new TextBlock
                {
                    Text = track.UploadDate,
                    FontSize = 12, Foreground = textSecondary,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(dateText, 3);

                // Play count
                var playsText = new TextBlock
                {
                    Text = track.PlayCount.ToString("N0"),
                    FontSize = 13, FontWeight = FontWeights.Bold,
                    Foreground = track.PlayCount > 0 ? accentBrush : textSecondary,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(playsText, 4);

                // Likes
                var likesText = new TextBlock
                {
                    Text = track.LikesCount.ToString("N0"),
                    FontSize = 13, FontWeight = FontWeights.Bold,
                    Foreground = textPrimary,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(likesText, 5);

                // Ban button (ban = permanent delete, no unban)
                var banBtn = new Button
                {
                    Content = "Забанить",
                    Tag = track.TrackId,
                    FontSize = 11, FontWeight = FontWeights.SemiBold,
                    Padding = new Thickness(10, 5, 10, 5),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    BorderThickness = new Thickness(0),
                    Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)),
                    Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                banBtn.Click += BanBtn_Click;
                Grid.SetColumn(banBtn, 6);

                grid.Children.Add(indexText);
                grid.Children.Add(titleGrid);
                grid.Children.Add(artistText);
                grid.Children.Add(dateText);
                grid.Children.Add(playsText);
                grid.Children.Add(likesText);
                grid.Children.Add(banBtn);

                rowBorder.Child = grid;
                TracksContainer.Children.Add(rowBorder);
            }

            if (tracks.Count == 0)
            {
                var empty = new TextBlock
                {
                    Text = "Треков не найдено",
                    FontSize = 15,
                    Foreground = textSecondary,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                };
                TracksContainer.Children.Add(empty);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

        private void ApplyFilters()
        {
            var q = SearchBox.Text?.Trim() ?? "";
            var filtered = _allTracks.Where(t =>
                (_statusFilter == "All" || t.PublishStatus == _statusFilter) &&
                (string.IsNullOrWhiteSpace(q) ||
                 t.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                 t.Artist.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();

            for (int i = 0; i < filtered.Count; i++) filtered[i].Index = i + 1;
            RenderTracks(filtered);
        }

        private void FilterAll_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            { _statusFilter = "All"; UpdateFilterUI(); ApplyFilters(); }
        private void FilterPublished_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            { _statusFilter = "Published"; UpdateFilterUI(); ApplyFilters(); }
        private void FilterPending_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            { _statusFilter = "Pending"; UpdateFilterUI(); ApplyFilters(); }
        private void FilterBanned_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            { _statusFilter = "Banned"; UpdateFilterUI(); ApplyFilters(); }

        private void UpdateFilterUI()
        {
            var dark = new SolidColorBrush(Color.FromRgb(26, 26, 24));
            var card = GetBrush("BgCard", Colors.White);
            var bord = GetBrush("BorderColor", Color.FromRgb(235, 235, 231));
            var sec = GetBrush("TextSecondary", Color.FromRgb(136, 136, 128));

            var map = new[] {
                (FilterAll, "All"), (FilterPublished, "Published"),
                (FilterPending, "Pending"), (FilterBanned, "Banned")
            };
            foreach (var (b, key) in map)
            {
                bool a = _statusFilter == key;
                b.Background = a ? dark : card;
                b.BorderBrush = a ? Brushes.Transparent : bord;
                b.BorderThickness = a ? new Thickness(0) : new Thickness(1.5);
                if (b.Child is TextBlock tb) tb.Foreground = a ? Brushes.White : sec;
            }
        }

        private void BanBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int trackId)
            {
                TrackService.BanUnbanTrack(trackId);
                LoadTracks();
            }
        }

        private static string FormatDuration(int seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes}:{ts.Seconds:D2}";
        }

        private static Brush GetBrush(string key, Color fallback)
        {
            return (Application.Current.TryFindResource(key) as Brush)
                ?? new SolidColorBrush(fallback);
        }
    }
}
