using Rewind.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rewind.Tabs.AdminTabs
{
    public partial class TrackReportsTab : UserControl, IAdminTab
    {
        private string _filter = "Pending";
        private readonly MediaPlayer _preview = new();

        public TrackReportsTab()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadReports();
        }

        public void Refresh() => LoadReports();

        private void LoadReports()
        {
            try
            {
                var all = TrackReportService.GetAll();
                var pending = all.Count(r => r.Status == "Pending");

                CountText.Text = pending == 0
                    ? "Нет активных жалоб"
                    : $"{pending} активных жалоб";

                var filtered = _filter == "Pending"
                    ? all.Where(r => r.Status == "Pending").ToList()
                    : all;

                Render(filtered);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TrackReportsTab] {ex.Message}");
            }
        }

        private void Render(List<TrackReport> reports)
        {
            ReportsContainer.Children.Clear();
            EmptyState.Visibility = reports.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            var textPrimary   = B("TextPrimary",   Color.FromRgb(26,  26,  24));
            var textSecondary = B("TextSecondary",  Color.FromRgb(136, 136, 128));
            var bgCard        = B("BgCard",         Colors.White);
            var borderBrush   = B("BorderColor",    Color.FromRgb(235, 235, 231));

            foreach (var report in reports)
            {
                var card = new Border
                {
                    Background      = bgCard,
                    CornerRadius    = new CornerRadius(16),
                    BorderBrush     = borderBrush,
                    BorderThickness = new Thickness(1),
                    Padding         = new Thickness(24, 18, 24, 18),
                    Margin          = new Thickness(0, 0, 0, 12)
                };

                var outerGrid = new Grid();
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var info = new StackPanel();

                // Track name + status badge row
                var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
                titleRow.Children.Add(new TextBlock
                {
                    Text              = report.Track?.Title ?? "Неизвестный трек",
                    FontSize          = 16,
                    FontWeight        = FontWeights.Bold,
                    Foreground        = textPrimary,
                    VerticalAlignment = VerticalAlignment.Center
                });

                var (bgC, fgC, lbl) = report.Status switch
                {
                    "Banned"    => (Color.FromRgb(254, 226, 226), Color.FromRgb(185, 28,  28),  "Забанен"),
                    "Dismissed" => (Color.FromRgb(240, 239, 235), Color.FromRgb(136, 136, 128), "✓ Отклонена"),
                    _           => (Color.FromRgb(254, 243, 199), Color.FromRgb(146, 64,  14),  "Активна")
                };

                var badge = new Border
                {
                    Background        = new SolidColorBrush(bgC),
                    CornerRadius      = new CornerRadius(8),
                    Padding           = new Thickness(8, 3, 8, 3),
                    Margin            = new Thickness(10, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                badge.Child = new TextBlock
                {
                    Text       = lbl,
                    FontSize   = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(fgC)
                };
                titleRow.Children.Add(badge);
                info.Children.Add(titleRow);

                info.Children.Add(new TextBlock
                {
                    Text       = $"Исполнитель: {report.Track?.Artist?.Nickname ?? "—"}",
                    FontSize   = 12,
                    Foreground = textSecondary,
                    Margin     = new Thickness(0, 0, 0, 2)
                });
                info.Children.Add(new TextBlock
                {
                    Text       = $"Жалоба от: {report.Reporter?.Nickname ?? "—"}",
                    FontSize   = 12,
                    Foreground = textSecondary,
                    Margin     = new Thickness(0, 0, 0, 4)
                });
                info.Children.Add(new TextBlock
                {
                    Text            = report.Reason,
                    FontSize        = 13,
                    Foreground      = textPrimary,
                    TextWrapping    = TextWrapping.Wrap,
                    Margin          = new Thickness(0, 0, 0, 4)
                });
                info.Children.Add(new TextBlock
                {
                    Text       = report.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy  HH:mm"),
                    FontSize   = 11,
                    Foreground = textSecondary
                });

                Grid.SetColumn(info, 0);

                if (report.Status == "Pending")
                {
                    var btns = new StackPanel
                    {
                        Orientation       = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    // Preview button (only if file exists)
                    var track = report.Track;
                    if (track != null)
                    {
                        var fp = System.IO.Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            "MusicLibrary",
                            track.FilePath ?? "");

                        if (System.IO.File.Exists(fp))
                        {
                            var previewBtn = new Button
                            {
                                Content         = "▶  Слушать",
                                Tag             = fp,
                                Background      = B("BgCardHover", Color.FromRgb(240, 239, 235)),
                                Foreground      = textPrimary,
                                FontSize        = 12,
                                FontWeight      = FontWeights.SemiBold,
                                BorderThickness = new Thickness(0),
                                Padding         = new Thickness(12, 7, 12, 7),
                                Margin          = new Thickness(0, 0, 8, 0),
                                Cursor          = Cursors.Hand
                            };
                            previewBtn.Click += (s, _) =>
                            {
                                _preview.Stop();
                                _preview.Open(new Uri((string)((Button)s).Tag));
                                _preview.Play();
                            };
                            btns.Children.Add(previewBtn);
                        }
                    }

                    var banBtn = new Button
                    {
                        Content         = "Забанить трек",
                        Tag             = report.ReportId,
                        Background      = new SolidColorBrush(Color.FromRgb(254, 226, 226)),
                        Foreground      = new SolidColorBrush(Color.FromRgb(185, 28, 28)),
                        FontSize        = 12,
                        FontWeight      = FontWeights.SemiBold,
                        BorderThickness = new Thickness(0),
                        Padding         = new Thickness(12, 7, 12, 7),
                        Margin          = new Thickness(0, 0, 8, 0),
                        Cursor          = Cursors.Hand
                    };
                    banBtn.Click += BanTrack_Click;
                    btns.Children.Add(banBtn);

                    var dismissBtn = new Button
                    {
                        Content         = "✓  Отклонить жалобу",
                        Tag             = report.ReportId,
                        Background      = B("BgCardHover", Color.FromRgb(240, 239, 235)),
                        Foreground      = textSecondary,
                        FontSize        = 12,
                        FontWeight      = FontWeights.SemiBold,
                        BorderThickness = new Thickness(0),
                        Padding         = new Thickness(12, 7, 12, 7),
                        Cursor          = Cursors.Hand
                    };
                    dismissBtn.Click += Dismiss_Click;
                    btns.Children.Add(dismissBtn);

                    Grid.SetColumn(btns, 1);
                    outerGrid.Children.Add(btns);
                }

                outerGrid.Children.Add(info);
                card.Child = outerGrid;
                ReportsContainer.Children.Add(card);
            }
        }

        private void BanTrack_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                TrackReportService.BanTrackFromReport(id);
                _preview.Stop();
                LoadReports();
            }
        }

        private void Dismiss_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                TrackReportService.Dismiss(id);
                LoadReports();
            }
        }

        private void FilterPending_Click(object sender, MouseButtonEventArgs e)
        {
            _filter = "Pending";
            UpdateFilterUI();
            LoadReports();
        }

        private void FilterAll_Click(object sender, MouseButtonEventArgs e)
        {
            _filter = "All";
            UpdateFilterUI();
            LoadReports();
        }

        private void UpdateFilterUI()
        {
            var dark   = new SolidColorBrush(Color.FromRgb(26,  26,  24));
            var card   = B("BgCard",       Colors.White);
            var border = B("BorderColor",  Color.FromRgb(235, 235, 231));
            var sec    = B("TextSecondary", Color.FromRgb(136, 136, 128));

            var map = new[] { (FilterPending, "Pending"), (FilterAll, "All") };
            foreach (var (f, k) in map)
            {
                bool active = _filter == k;
                f.Background      = active ? dark : card;
                f.BorderBrush     = active ? Brushes.Transparent : border;
                f.BorderThickness = active ? new Thickness(0) : new Thickness(1.5);
                if (f.Child is TextBlock tb)
                    tb.Foreground = active ? Brushes.White : sec;
            }
        }

        private static Brush B(string key, Color fallback) =>
            (Application.Current.TryFindResource(key) as Brush) ?? new SolidColorBrush(fallback);
    }
}
