using Microsoft.Win32;
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
    public partial class TrackSubmissionsTab : UserControl, IAdminTab
    {
        private int _pendingRejectTrackId;
        private readonly MediaPlayer _preview = new();

        public TrackSubmissionsTab()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadSubmissions();
        }

        public void Refresh() => LoadSubmissions();

        private void LoadSubmissions()
        {
            var tracks = TrackService.GetPendingTracks();

            PendingCountText.Text = tracks.Count == 0
                ? "Нет треков, ожидающих публикации"
                : $"{tracks.Count} {Plural(tracks.Count, "трек", "трека", "треков")} ожидает проверки";

            TracksContainer.Children.Clear();
            EmptyState.Visibility = tracks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            var textPrimary = Brush("TextPrimary", Color.FromRgb(26, 26, 24));
            var textSecondary = Brush("TextSecondary", Color.FromRgb(136, 136, 128));
            var bgCard = Brush("BgCard", Colors.White);
            var border = Brush("BorderColor", Color.FromRgb(235, 235, 231));
            var accent = Brush("AccentColor", Color.FromRgb(42, 232, 118));

            foreach (var track in tracks)
            {
                var card = new Border
                {
                    Background = bgCard, CornerRadius = new CornerRadius(16),
                    BorderBrush = border, BorderThickness = new Thickness(1),
                    Padding = new Thickness(24, 20, 24, 20), Margin = new Thickness(0, 0, 0, 14)
                };

                var outer = new StackPanel();

                // ── Header row ──
                var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                var infoStack = new StackPanel();

                var titleEditBox = new TextBox
                {
                    Text = track.Title, FontSize = 18, FontWeight = FontWeights.Bold,
                    Foreground = textPrimary,
                    Background = Brush("BgCardHover", Color.FromRgb(240, 239, 235)),
                    BorderBrush = border, BorderThickness = new Thickness(1),
                    Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 0, 6),
                    Tag = track.TrackID
                };

                var artistText = new TextBlock
                {
                    Text = $"Исполнитель: {track.Artist?.Nickname ?? "—"}",
                    FontSize = 13, Foreground = textSecondary, Margin = new Thickness(0, 0, 0, 2)
                };
                var genreEditBox = new TextBox
                {
                    Text = track.Genre ?? "",
                    FontSize = 12, Foreground = textSecondary,
                    Background = Brush("BgCardHover", Color.FromRgb(240, 239, 235)),
                    BorderBrush = border, BorderThickness = new Thickness(1),
                    Padding = new Thickness(8, 3, 8, 3), Width = 200,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Tag = "genre_" + track.TrackID
                };
                var genreLabel = new TextBlock
                {
                    Text = "Жанр:", FontSize = 11, Foreground = textSecondary, Margin = new Thickness(0, 4, 0, 2)
                };

                var dateText = new TextBlock
                {
                    Text = $"Загружен: {track.UploadDate:dd.MM.yyyy HH:mm}",
                    FontSize = 11, Foreground = textSecondary, Margin = new Thickness(0, 6, 0, 0)
                };

                infoStack.Children.Add(titleEditBox);
                infoStack.Children.Add(artistText);
                infoStack.Children.Add(genreLabel);
                infoStack.Children.Add(genreEditBox);
                infoStack.Children.Add(dateText);
                Grid.SetColumn(infoStack, 0);

                // Cover image
                var coverBorder = new Border
                {
                    Width = 80, Height = 80, CornerRadius = new CornerRadius(10),
                    Background = Brush("BgCardHover", Color.FromRgb(240, 239, 235)),
                    Margin = new Thickness(16, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Top
                };
                if (!string.IsNullOrEmpty(track.CoverPath) && File.Exists(track.CoverPath))
                {
                    try
                    {
                        coverBorder.Child = new Image
                        {
                            Source = new BitmapImage(new Uri(track.CoverPath)),
                            Stretch = Stretch.UniformToFill
                        };
                    }
                    catch { }
                }
                Grid.SetColumn(coverBorder, 1);

                headerGrid.Children.Add(infoStack);
                headerGrid.Children.Add(coverBorder);

                // ── Action row ──
                var actionRow = new StackPanel { Orientation = Orientation.Horizontal };

                // Preview play button
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", track.FilePath);
                if (File.Exists(filePath))
                {
                    var previewBtn = new Button
                    {
                        Content = "▶  Прослушать",
                        Tag = filePath,
                        Background = Brush("BgCardHover", Color.FromRgb(240, 239, 235)),
                        Foreground = textPrimary, FontSize = 12, FontWeight = FontWeights.SemiBold,
                        BorderThickness = new Thickness(0), Padding = new Thickness(14, 8, 14, 8),
                        Margin = new Thickness(0, 0, 10, 0), Cursor = System.Windows.Input.Cursors.Hand
                    };
                    previewBtn.Click += (s, _) =>
                    {
                        _preview.Stop();
                        _preview.Open(new Uri((string)((Button)s).Tag));
                        _preview.Play();
                    };
                    ApplyRoundedTemplate(previewBtn, 10);
                    actionRow.Children.Add(previewBtn);

                    var stopBtn = new Button
                    {
                        Content = "⏹  Стоп",
                        Background = Brush("BgCardHover", Color.FromRgb(240, 239, 235)),
                        Foreground = textSecondary, FontSize = 12, FontWeight = FontWeights.SemiBold,
                        BorderThickness = new Thickness(0), Padding = new Thickness(14, 8, 14, 8),
                        Margin = new Thickness(0, 0, 16, 0), Cursor = System.Windows.Input.Cursors.Hand
                    };
                    stopBtn.Click += (_, __) => _preview.Stop();
                    ApplyRoundedTemplate(stopBtn, 10);
                    actionRow.Children.Add(stopBtn);
                }

                // Approve
                var approveBtn = new Button
                {
                    Content = "✓  Опубликовать",
                    Tag = new object[] { track.TrackID, titleEditBox, genreEditBox },
                    Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)),
                    Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61)),
                    FontSize = 13, FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(0), Padding = new Thickness(18, 9, 18, 9),
                    Margin = new Thickness(0, 0, 10, 0), Cursor = System.Windows.Input.Cursors.Hand
                };
                approveBtn.Click += Approve_Click;
                ApplyRoundedTemplate(approveBtn, 10);

                // Reject
                var rejectBtn = new Button
                {
                    Content = "✕  Отклонить",
                    Tag = track.TrackID,
                    Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)),
                    Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28)),
                    FontSize = 13, FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(0), Padding = new Thickness(18, 9, 18, 9),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                rejectBtn.Click += Reject_Click;
                ApplyRoundedTemplate(rejectBtn, 10);

                actionRow.Children.Add(approveBtn);
                actionRow.Children.Add(rejectBtn);

                outer.Children.Add(headerGrid);

                // Separator
                outer.Children.Add(new Border
                {
                    Height = 1, Background = border, Margin = new Thickness(0, 0, 0, 14)
                });
                outer.Children.Add(actionRow);

                card.Child = outer;
                TracksContainer.Children.Add(card);
            }
        }

        private void Approve_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is object[] args)
            {
                int trackId = (int)args[0];
                string title = (args[1] as TextBox)?.Text ?? "";
                string genre = (args[2] as TextBox)?.Text ?? "";
                TrackService.ApproveTrack(trackId, title, genre);
                _preview.Stop();
                LoadSubmissions();
            }
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int trackId)
            {
                _pendingRejectTrackId = trackId;
                RejectReasonBox.Clear();
                RejectModal.Visibility = Visibility.Visible;
            }
        }

        private void ConfirmReject_Click(object sender, RoutedEventArgs e)
        {
            var reason = RejectReasonBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Укажите причину отклонения.", "Rewind Admin",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            TrackService.RejectTrack(_pendingRejectTrackId, reason);
            RejectModal.Visibility = Visibility.Collapsed;
            _preview.Stop();
            LoadSubmissions();
        }

        private void CancelReject_Click(object sender, RoutedEventArgs e)
            => RejectModal.Visibility = Visibility.Collapsed;

        // ── Helpers ──
        private static Brush Brush(string key, Color fallback) =>
            (Application.Current.TryFindResource(key) as Brush) ?? new SolidColorBrush(fallback);

        private static string Plural(int n, string one, string few, string many)
        {
            var a = Math.Abs(n) % 100;
            var m = a % 10;
            if (a is > 10 and < 20) return many;
            if (m == 1) return one;
            if (m is >= 2 and <= 4) return few;
            return many;
        }

        private static void ApplyRoundedTemplate(Button btn, double radius)
        {
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
            factory.SetBinding(Border.BackgroundProperty,
                new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            factory.SetBinding(Border.PaddingProperty,
                new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.AppendChild(cp);
            btn.Template = new ControlTemplate(typeof(Button)) { VisualTree = factory };
        }
    }
}
