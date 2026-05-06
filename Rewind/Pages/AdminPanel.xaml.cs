using Rewind.Helpers;
using Rewind.MVVM.ViewModels.Pages;
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
    /// <summary>
    /// View поверх <see cref="AdminPanelViewModel"/>. VM держит активную вкладку,
    /// счётчики бейджей и команды. View отвечает за создание UserControl-вкладок
    /// и визуальную подсветку кнопок.
    /// </summary>
    public partial class AdminPanel : Window
    {
        private readonly AdminPanelViewModel _vm = new();
        private Button? _activeBtn;
        private readonly DispatcherTimer _refreshTimer;

        public AdminPanel()
        {
            InitializeComponent();
            DataContext = _vm;

            _vm.TabChanged += SwitchTabContent;
            _vm.ThemeChanged += theme =>
            {
                ApplyThemeFile(theme + ".xaml");
                MarkActiveThemeCircle();
                SwitchTabContent(_vm.ActiveTab); // пересоздаём UserControl, чтобы подхватить DynamicResource
            };
            _vm.LogoutRequested += DoLogout;

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
            _vm.RefreshBadges();
            ApplyBadgesToUi();
            if (AdminContentArea.Content is IAdminTab tab) tab.Refresh();
        }

        /// <summary>Переносит значения VM в существующие XAML-элементы бейджей.</summary>
        private void ApplyBadgesToUi()
        {
            RequestsBadgeText.Text = _vm.RequestsBadgeText;
            RequestsBadge.Visibility = _vm.RequestsBadgeVisible ? Visibility.Visible : Visibility.Collapsed;
            SubmissionsBadgeText.Text = _vm.SubmissionsBadgeText;
            SubmissionsBadge.Visibility = _vm.SubmissionsBadgeVisible ? Visibility.Visible : Visibility.Collapsed;
            ReportsBadgeText.Text = _vm.ReportsBadgeText;
            ReportsBadge.Visibility = _vm.ReportsBadgeVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Navigation ──

        private void ShowOverview_Click(object sender, RoutedEventArgs e)    => _vm.SelectTabCommand.Execute("overview");
        private void ShowUsers_Click(object sender, RoutedEventArgs e)       => _vm.SelectTabCommand.Execute("users");
        private void ShowTracks_Click(object sender, RoutedEventArgs e)      => _vm.SelectTabCommand.Execute("tracks");
        private void ShowSubmissions_Click(object sender, RoutedEventArgs e) => _vm.SelectTabCommand.Execute("submissions");
        private void ShowRequests_Click(object sender, RoutedEventArgs e)    => _vm.SelectTabCommand.Execute("requests");
        private void ShowReports_Click(object sender, RoutedEventArgs e)     => _vm.SelectTabCommand.Execute("reports");

        private void ShowOverview() => SwitchTabContent("overview");

        /// <summary>Меняет UserControl в области контента согласно ActiveTab из VM.</summary>
        private void SwitchTabContent(string tab)
        {
            (AdminContentArea.Content, var btn) = tab switch
            {
                "users"       => ((object)new UsersTab(), BtnUsers),
                "tracks"      => (new TracksTab(), BtnTracks),
                "submissions" => (new TrackSubmissionsTab(), BtnSubmissions),
                "requests"    => (new ArtistRequestsTab(), BtnRequests),
                "reports"     => (new TrackReportsTab(), BtnReports),
                _             => (new OverviewTab(), BtnOverview),
            };
            HighlightBtn(btn);
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
                // VM меняет Session.ActiveTheme и поднимает ThemeChanged → перерисовка
                _vm.SelectThemeCommand.Execute(themeFile.Replace(".xaml", ""));
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

        private void LogOut_Click(object sender, MouseButtonEventArgs e) => _vm.LogoutCommand.Execute(null);

        private void DoLogout()
        {
            _refreshTimer.Stop();
            var reg = new Registration();
            reg.Show();
            Application.Current.MainWindow = reg;
            Close();
        }
    }
}
