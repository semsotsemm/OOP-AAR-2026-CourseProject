using Microsoft.EntityFrameworkCore;
using Rewind.Controls;
using Rewind.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rewind.Tabs.AdminTabs
{
    public interface IAdminTab
    {
        void Refresh();
    }

    public partial class OverviewTab : UserControl, IAdminTab
    {
        private sealed class TopTrackItem
        {
            public int Rank { get; set; }
            public string Title { get; set; } = "";
            public string Artist { get; set; } = "";
            public int Plays { get; set; }
            public double RelativeValue { get; set; }
        }

        public OverviewTab()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadData();
        }

        public void Refresh() => LoadData();

        private void LoadData()
        {
            try
            {
                using var db = new AppDbContext();

                // ── Stat cards ──
                CardUsersCount.Text = db.Users.Count().ToString();
                CardTracksCount.Text = db.Tracks.Count().ToString();
                CardPlaylistsCount.Text = db.Playlists.Count().ToString();
                CardPlaysCount.Text = (db.Statistics.Sum(s => (long?)s.PlayCount) ?? 0).ToString("N0");

                // ── Activity chart (last 14 days) ──
                var today = DateTime.UtcNow.Date;
                var since = today.AddDays(-13);

                // Вытащиваем в память, потом группируем — так EF точно переведёт .Date на SQL
                var histByDay = db.History
                    .Where(h => h.ListenedAt >= since)
                    .Select(h => h.ListenedAt)
                    .ToList()
                    .GroupBy(dt => dt.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

                var activityData = Enumerable.Range(0, 14)
                    .Select(i =>
                    {
                        var d = since.AddDays(i);
                        return new ChartDataPoint
                        {
                            Label = d.ToString("dd.MM"),
                            Value = histByDay.TryGetValue(d, out var c) ? c : 0
                        };
                    })
                    .ToList();

                ActivityChart.SetData(activityData);

                // ── Roles chart ──
                var roleGroups = db.Users
                    .GroupBy(u => u.RoleId)
                    .Select(g => new { RoleId = g.Key, Count = g.Count() })
                    .ToList();

                var rolesData = new List<ChartDataPoint>
                {
                    new() { Label = "Слушатели", Value = roleGroups.FirstOrDefault(r => r.RoleId == 3)?.Count ?? 0 },
                    new() { Label = "Исполнители", Value = roleGroups.FirstOrDefault(r => r.RoleId == 2)?.Count ?? 0 },
                    new() { Label = "Админы", Value = roleGroups.FirstOrDefault(r => r.RoleId == 1)?.Count ?? 0 }
                };
                RolesChart.SetData(rolesData);

                // ── Top tracks ──
                var topTracks = db.Statistics
                    .Where(s => s.PlayCount > 0)
                    .OrderByDescending(s => s.PlayCount)
                    .Take(8)
                    .Join(db.Tracks, s => s.TrackID, t => t.TrackID,
                          (s, t) => new { t.Title, s.PlayCount, t.ArtistID, s.LikesCount })
                    .Join(db.Users, x => x.ArtistID, u => u.UserId,
                          (x, u) => new TopTrackItem
                          {
                              Title = x.Title,
                              Artist = u.Nickname,
                              Plays = x.PlayCount
                          })
                    .ToList();

                if (topTracks.Count == 0)
                {
                    TopTracksEmpty.Visibility = Visibility.Visible;
                }
                else
                {
                    double maxPlays = topTracks.Max(t => t.Plays);
                    for (int i = 0; i < topTracks.Count; i++)
                    {
                        topTracks[i].Rank = i + 1;
                        topTracks[i].RelativeValue = maxPlays > 0 ? topTracks[i].Plays / maxPlays : 0;
                    }
                    BuildTopTracksList(topTracks);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OverviewTab] {ex.Message}");
            }
        }

        private void BuildTopTracksList(List<TopTrackItem> tracks)
        {
            TopTracksList.Children.Clear();

            var accentBrush = (Brush)(Application.Current.TryFindResource("AccentColor")
                ?? new SolidColorBrush(Color.FromRgb(42, 232, 118)));
            var textPrimaryBrush = (Brush)(Application.Current.TryFindResource("TextPrimary")
                ?? new SolidColorBrush(Color.FromRgb(26, 26, 24)));
            var textSecondaryBrush = (Brush)(Application.Current.TryFindResource("TextSecondary")
                ?? new SolidColorBrush(Color.FromRgb(136, 136, 128)));
            var bgHoverBrush = (Brush)(Application.Current.TryFindResource("BgCardHover")
                ?? new SolidColorBrush(Color.FromRgb(240, 239, 235)));

            foreach (var track in tracks)
            {
                // Row grid
                var rowGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

                // Rank badge
                var rankBorder = new Border
                {
                    Width = 28, Height = 28, CornerRadius = new CornerRadius(8),
                    Background = track.Rank <= 3 ? accentBrush : bgHoverBrush,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var rankText = new TextBlock
                {
                    Text = track.Rank.ToString(),
                    FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = track.Rank <= 3 ? Brushes.White : textSecondaryBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                rankBorder.Child = rankText;
                Grid.SetColumn(rankBorder, 0);

                // Track info + progress bar
                var infoStack = new StackPanel { Margin = new Thickness(12, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center };

                var titleRow = new Grid();
                titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                var titleText = new TextBlock
                {
                    Text = track.Title,
                    FontSize = 13, FontWeight = FontWeights.SemiBold,
                    Foreground = textPrimaryBrush,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(titleText, 0);

                var artistText = new TextBlock
                {
                    Text = track.Artist,
                    FontSize = 11,
                    Foreground = textSecondaryBrush,
                    Margin = new Thickness(0, 3, 0, 5)
                };

                // Progress bar track
                var progressBg = new Border
                {
                    Height = 6, CornerRadius = new CornerRadius(3),
                    Background = bgHoverBrush
                };
                var progressGrid = new Grid();
                progressGrid.Children.Add(progressBg);

                // Use a Grid for the fill bar with proportional width
                var fillGrid = new Grid();
                fillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(track.RelativeValue, GridUnitType.Star) });
                fillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0 - track.RelativeValue, GridUnitType.Star) });
                var fillBar = new Border
                {
                    Height = 6, CornerRadius = new CornerRadius(3),
                    Background = accentBrush
                };
                Grid.SetColumn(fillBar, 0);
                fillGrid.Children.Add(fillBar);
                progressGrid.Children.Add(fillGrid);

                titleRow.Children.Add(titleText);
                infoStack.Children.Add(titleRow);
                infoStack.Children.Add(artistText);
                infoStack.Children.Add(progressGrid);

                Grid.SetColumn(infoStack, 1);

                // Plays count
                var playsStack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                var playsNum = new TextBlock
                {
                    Text = track.Plays.ToString("N0"),
                    FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = textPrimaryBrush,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                var playsLabel = new TextBlock
                {
                    Text = "прослуш.",
                    FontSize = 10,
                    Foreground = textSecondaryBrush,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                playsStack.Children.Add(playsNum);
                playsStack.Children.Add(playsLabel);
                Grid.SetColumn(playsStack, 2);

                rowGrid.Children.Add(rankBorder);
                rowGrid.Children.Add(infoStack);
                rowGrid.Children.Add(playsStack);

                TopTracksList.Children.Add(rowGrid);

                // Separator (except last)
                if (track.Rank < tracks.Count)
                {
                    var sep = new Border
                    {
                        Height = 1,
                        Background = (Brush)(Application.Current.TryFindResource("BorderColor")
                            ?? new SolidColorBrush(Color.FromRgb(235, 235, 231))),
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    TopTracksList.Children.Add(sep);
                }
            }
        }

        private void Refresh_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LoadData();
        }
    }
}
