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
    public partial class ArtistRequestsTab : UserControl, IAdminTab
    {
        private string _currentFilter = "Pending"; // Pending | All | Approved | Rejected

        public ArtistRequestsTab()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadRequests();
        }

        public void Refresh() => LoadRequests();

        private void LoadRequests()
        {
            try
            {
                var all = ArtistRequestService.GetAll();
                var pending = all.Count(r => r.Status == "Pending");

                PendingCountText.Text = pending == 0
                    ? "Нет ожидающих заявок"
                    : $"{pending} {PluralForms(pending, "заявка", "заявки", "заявок")} ожидает рассмотрения";

                var filtered = _currentFilter switch
                {
                    "Pending" => all.Where(r => r.Status == "Pending").ToList(),
                    "Approved" => all.Where(r => r.Status == "Approved").ToList(),
                    "Rejected" => all.Where(r => r.Status == "Rejected").ToList(),
                    _ => all
                };

                RenderRequests(filtered);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ArtistRequestsTab] {ex.Message}");
            }
        }

        private void RenderRequests(List<ArtistRequest> requests)
        {
            RequestsContainer.Children.Clear();

            if (requests.Count == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
                EmptyStateText.Text = _currentFilter switch
                {
                    "Pending" => "Нет ожидающих заявок",
                    "Approved" => "Нет одобренных заявок",
                    "Rejected" => "Нет отклонённых заявок",
                    _ => "Заявок нет"
                };
                return;
            }

            EmptyState.Visibility = Visibility.Collapsed;

            var textPrimary = GetBrush("TextPrimary", Color.FromRgb(26, 26, 24));
            var textSecondary = GetBrush("TextSecondary", Color.FromRgb(136, 136, 128));
            var bgCard = GetBrush("BgCard", Colors.White);
            var borderBrush = GetBrush("BorderColor", Color.FromRgb(235, 235, 231));
            var accentBrush = GetBrush("AccentColor", Color.FromRgb(42, 232, 118));

            foreach (var req in requests)
            {
                var card = new Border
                {
                    Background = bgCard,
                    CornerRadius = new CornerRadius(16),
                    BorderBrush = borderBrush,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(24, 18, 24, 18),
                    Margin = new Thickness(0, 0, 0, 12)
                };

                var outerGrid = new Grid();
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                // ── Left: info ──
                var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

                // Nickname + status badge row
                var nicknameRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
                var nicknameText = new TextBlock
                {
                    Text = req.Nickname,
                    FontSize = 18, FontWeight = FontWeights.Bold,
                    Foreground = textPrimary, VerticalAlignment = VerticalAlignment.Center
                };
                nicknameRow.Children.Add(nicknameText);

                // Status badge
                var (bgColor, fgColor, label) = req.Status switch
                {
                    "Approved" => (Color.FromRgb(220, 252, 231), Color.FromRgb(21, 128, 61), "✓ Одобрено"),
                    "Rejected" => (Color.FromRgb(254, 226, 226), Color.FromRgb(185, 28, 28), "✕ Отклонено"),
                    _ => (Color.FromRgb(254, 243, 199), Color.FromRgb(146, 64, 14), "Ожидает")
                };
                var badge = new Border
                {
                    Background = new SolidColorBrush(bgColor),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 4, 10, 4),
                    Margin = new Thickness(12, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                badge.Child = new TextBlock
                {
                    Text = label, FontSize = 11, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(fgColor)
                };
                nicknameRow.Children.Add(badge);

                var emailText = new TextBlock
                {
                    Text = req.Email, FontSize = 13,
                    Foreground = textSecondary, Margin = new Thickness(0, 0, 0, 4)
                };
                var dateText = new TextBlock
                {
                    Text = "Заявка от " + req.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy  HH:mm"),
                    FontSize = 11, Foreground = textSecondary
                };

                infoStack.Children.Add(nicknameRow);
                infoStack.Children.Add(emailText);
                infoStack.Children.Add(dateText);
                Grid.SetColumn(infoStack, 0);

                // ── Right: action buttons (only for Pending) ──
                if (req.Status == "Pending")
                {
                    var btnPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var approveBtn = new Button
                    {
                        Content = "✓  Одобрить",
                        Style = (Style)FindResource("ApproveBtn"),
                        Tag = req.RequestId
                    };
                    approveBtn.Click += Approve_Click;

                    var rejectBtn = new Button
                    {
                        Content = "✕  Отклонить",
                        Style = (Style)FindResource("RejectBtn"),
                        Tag = req.RequestId
                    };
                    rejectBtn.Click += Reject_Click;

                    btnPanel.Children.Add(approveBtn);
                    btnPanel.Children.Add(rejectBtn);
                    Grid.SetColumn(btnPanel, 1);
                    outerGrid.Children.Add(btnPanel);
                }

                outerGrid.Children.Add(infoStack);
                card.Child = outerGrid;
                RequestsContainer.Children.Add(card);
            }
        }

        // ── Actions ──

        private void Approve_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int requestId)
            {
                ArtistRequestService.Approve(requestId);
                LoadRequests();
            }
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int requestId)
            {
                var result = MessageBox.Show("Отклонить эту заявку? Аккаунт исполнителя создан не будет.",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    ArtistRequestService.Reject(requestId);
                    LoadRequests();
                }
            }
        }

        // ── Filters ──

        private void FilterPending_Click(object sender, MouseButtonEventArgs e)
        {
            _currentFilter = "Pending";
            UpdateFilterStyles();
            LoadRequests();
        }

        private void FilterAll_Click(object sender, MouseButtonEventArgs e)
        {
            _currentFilter = "All";
            UpdateFilterStyles();
            LoadRequests();
        }

        private void FilterApproved_Click(object sender, MouseButtonEventArgs e)
        {
            _currentFilter = "Approved";
            UpdateFilterStyles();
            LoadRequests();
        }

        private void FilterRejected_Click(object sender, MouseButtonEventArgs e)
        {
            _currentFilter = "Rejected";
            UpdateFilterStyles();
            LoadRequests();
        }

        private void UpdateFilterStyles()
        {
            var darkBg = new SolidColorBrush(Color.FromRgb(26, 26, 24));
            var cardBg = GetBrush("BgCard", Colors.White);
            var border = GetBrush("BorderColor", Color.FromRgb(235, 235, 231));
            var textSec = GetBrush("TextSecondary", Color.FromRgb(136, 136, 128));

            var filters = new[]
            {
                (FilterPending,  "Pending"),
                (FilterAll,      "All"),
                (FilterApproved, "Approved"),
                (FilterRejected, "Rejected")
            };

            foreach (var (f, key) in filters)
            {
                bool active = _currentFilter == key;
                f.Background = active ? darkBg : cardBg;
                f.BorderBrush = active ? Brushes.Transparent : border;
                f.BorderThickness = active ? new Thickness(0) : new Thickness(1.5);
                if (f.Child is TextBlock tb)
                    tb.Foreground = active ? Brushes.White : textSec;
            }
        }

        // ── Helpers ──

        private static Brush GetBrush(string key, Color fallback) =>
            (Application.Current.TryFindResource(key) as Brush)
            ?? new SolidColorBrush(fallback);

        private static string PluralForms(int n, string one, string few, string many)
        {
            var abs = Math.Abs(n) % 100;
            var mod10 = abs % 10;
            if (abs is > 10 and < 20) return many;
            if (mod10 == 1) return one;
            if (mod10 is >= 2 and <= 4) return few;
            return many;
        }
    }
}
