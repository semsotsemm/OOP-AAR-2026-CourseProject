using Rewind.Helpers;
using Rewind.Tabs.AdminTabs;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Rewind.Pages
{
    public partial class AdminPanel : Window
    {
        private Button? _activeBtn;
        private readonly DispatcherTimer _refreshTimer;

        public AdminPanel()
        {
            InitializeComponent();
            ApplyCurrentTheme();
            ShowOverview();
            _activeBtn = BtnOverview;
            MarkActiveThemeCircle();

            // Реальное время: обновляем текущую вкладку каждые 5 секунд
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            Closing += (_, _) => _refreshTimer.Stop();
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            UpdateRequestsBadge();
            UpdateSubmissionsBadge();
            UpdateReportsBadge();
            if (AdminContentArea.Content is IAdminTab tab)
                tab.Refresh();
        }

        private void UpdateRequestsBadge()
        {
            try
            {
                int count = ArtistRequestService.PendingCount();
                RequestsBadgeText.Text = count > 99 ? "99+" : count.ToString();
                RequestsBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }

        private void UpdateSubmissionsBadge()
        {
            try
            {
                int count = TrackService.GetPendingTracks().Count;
                SubmissionsBadgeText.Text = count > 99 ? "99+" : count.ToString();
                SubmissionsBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }

        private void UpdateReportsBadge()
        {
            try
            {
                int count = TrackReportService.PendingCount();
                ReportsBadgeText.Text = count > 99 ? "99+" : count.ToString();
                ReportsBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }

        // ── Navigation ──

        private void ShowOverview_Click(object sender, RoutedEventArgs e)
        {
            ShowOverview();
            HighlightBtn(BtnOverview);
        }

        private void ShowUsers_Click(object sender, RoutedEventArgs e)
        {
            AdminContentArea.Content = new UsersTab();
            HighlightBtn(BtnUsers);
        }

        private void ShowTracks_Click(object sender, RoutedEventArgs e)
        {
            AdminContentArea.Content = new TracksTab();
            HighlightBtn(BtnTracks);
        }

        private void ShowSubmissions_Click(object sender, RoutedEventArgs e)
        {
            AdminContentArea.Content = new TrackSubmissionsTab();
            HighlightBtn(BtnSubmissions);
        }

        private void ShowRequests_Click(object sender, RoutedEventArgs e)
        {
            AdminContentArea.Content = new ArtistRequestsTab();
            HighlightBtn(BtnRequests);
        }

        private void ShowReports_Click(object sender, RoutedEventArgs e)
        {
            AdminContentArea.Content = new TrackReportsTab();
            HighlightBtn(BtnReports);
        }

        private void ShowOverview()
        {
            AdminContentArea.Content = new OverviewTab();
        }

        private void HighlightBtn(Button btn)
        {
            Button[] btns = { BtnOverview, BtnUsers, BtnTracks, BtnSubmissions, BtnRequests, BtnReports };

            var accentBrush = (Brush)FindResource("AccentColor");
            var bgMainBrush = (Brush)FindResource("BgMain");
            var textPrimaryBrush = (Brush)FindResource("TextPrimary");

            foreach (var b in btns)
            {
                bool isActive = b == btn;
                b.Style = (Style)FindResource(isActive ? "ActiveNavButtonStyle" : "NavButtonStyle");

                // BtnRequests uses a Grid as Content (to show badge), others use StackPanel
                var contentSp = b.Content is StackPanel sp ? sp
                    : b.Content is Grid g ? g.Children.OfType<StackPanel>().FirstOrDefault()
                    : null;

                if (contentSp != null)
                {
                    if (contentSp.Children.Count > 0 && contentSp.Children[0] is Border iconBorder)
                    {
                        iconBorder.Background = isActive ? accentBrush : bgMainBrush;

                        // Инвертируем заливку иконки: активный — белая, неактивный — зелёная
                        if (iconBorder.Child is System.Windows.Shapes.Rectangle iconRect)
                            iconRect.Fill = isActive ? bgMainBrush : accentBrush;
                    }

                    if (contentSp.Children.Count > 1 && contentSp.Children[1] is TextBlock tb)
                    {
                        tb.Foreground = isActive ? accentBrush : textPrimaryBrush;
                        tb.FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal;
                    }
                }
            }

            _activeBtn = btn;
        }

        // ── Theme switching ──

        private void ThemeCircle_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.Tag is string themeFile)
            {
                ApplyThemeFile(themeFile);
                Session.ActiveTheme = themeFile.Replace(".xaml", "");
                MarkActiveThemeCircle();

                // Пересоздаём текущую вкладку, чтобы она подхватила новые DynamicResource
                if (_activeBtn == BtnOverview) ShowOverview();
                else if (_activeBtn == BtnUsers) AdminContentArea.Content = new UsersTab();
                else if (_activeBtn == BtnTracks) AdminContentArea.Content = new TracksTab();
                else if (_activeBtn == BtnSubmissions) AdminContentArea.Content = new TrackSubmissionsTab();
                else if (_activeBtn == BtnRequests) AdminContentArea.Content = new ArtistRequestsTab();
                else if (_activeBtn == BtnReports) AdminContentArea.Content = new TrackReportsTab();
            }
        }

        private void ApplyThemeFile(string themeFile)
        {
            var uri = new Uri($"/Resources/Themes/{themeFile}", UriKind.Relative);
            var dicts = Application.Current.Resources.MergedDictionaries;
            var existing = dicts.FirstOrDefault(d =>
                d.Source?.OriginalString.Contains("Theme") == true);
            if (existing != null) dicts.Remove(existing);
            dicts.Add(new ResourceDictionary { Source = uri });
        }

        private void ApplyCurrentTheme()
        {
            var themeFile = (Session.ActiveTheme ?? "ThemeClassic") + ".xaml";
            ApplyThemeFile(themeFile);
        }

        private void MarkActiveThemeCircle()
        {
            var circles = new[]
            {
                (ThemeClassicCircle,  "ThemeClassic"),
                (ThemePinkCircle,     "ThemePink"),
                (ThemeMidnightCircle, "ThemeMidnight"),
                (ThemeLavenderCircle, "ThemeLavender")
            };

            foreach (var (circle, name) in circles)
            {
                bool isActive = Session.ActiveTheme == name;
                circle.BorderBrush = isActive
                    ? (Brush)FindResource("AccentColor")
                    : Brushes.Transparent;
                circle.BorderThickness = new Thickness(isActive ? 2.5 : 2);
            }
        }

        // ── Log out ──

        private void LogOut_Click(object sender, MouseButtonEventArgs e)
        {
            _refreshTimer.Stop();
            var reg = new Registration();
            reg.Show();
            Application.Current.MainWindow = reg;
            Close();
        }
    }
}
