using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Rewind.Pages
{
    public partial class Profile : Window
    {
        public Profile()
        {
            InitializeComponent();
        }

        private void TabOverview_Click(object sender, RoutedEventArgs e) => SetActiveTab(TabOverview, PanelOverview);
        private void TabLiked_Click(object sender, RoutedEventArgs e) => SetActiveTab(TabLiked, PanelLiked);
        private void TabPlaylists_Click(object sender, RoutedEventArgs e) => SetActiveTab(TabPlaylists, PanelPlaylists);
        private void TabSettings_Click(object sender, RoutedEventArgs e) => SetActiveTab(TabSettings, PanelSettings);

        public static void ChangeTheme(Uri themeUri)
        {
            ResourceDictionary dict = new ResourceDictionary { Source = themeUri };

            var oldDict = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.Contains("Themes/"));

            if (oldDict != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(oldDict);
            }
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
        private void LogOut_MouseDown(object sender, MouseButtonEventArgs e) 
        {
            Registration registration_page = new Registration();
            registration_page.Show();
            this.Close();
        }
        private void ThemeClassic_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedBorder)
            {
                UpdateActiveCard(clickedBorder);

                string themeFile = clickedBorder.Name switch
                {
                    "ThemeClassic" => "ThemeClassic.xaml",
                    "ThemePink" => "ThemePink.xaml",
                    "ThemeMidnight" => "ThemeMidnight.xaml",
                    "ThemeLavender" => "ThemeLavender.xaml",
                    _ => "ThemeClassic.xaml"
                };

                ApplyTheme(themeFile);
            }
        }

        private void UpdateActiveCard(Border selectedBorder)
        {
            var themeCards = new List<Border> { ThemeClassic, ThemePink, ThemeMidnight, ThemeLavender };

            var normalStyle = (Style)FindResource("ThemeCard");
            var activeStyle = (Style)FindResource("ThemeCardActive");

            foreach (var card in themeCards)
            {
                card.Style = (card == selectedBorder) ? activeStyle : normalStyle;
            }
        }

        private void ApplyTheme(string themeFileName)
        {
            try
            {
                var uri = new Uri($"Themes/{themeFileName}", UriKind.Relative);
                ResourceDictionary newDict = new ResourceDictionary { Source = uri };

                var oldDict = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Themes/"));

                if (oldDict != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                }
                Application.Current.Resources.MergedDictionaries.Add(newDict);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке темы: {ex.Message}");
            }
        }
        private void SetActiveTab(Button activeTabBtn, UIElement activePanel)
        {
            Style inactiveStyle = (Style)FindResource("ProfileTab");
            TabOverview.Style = inactiveStyle;
            TabLiked.Style = inactiveStyle;
            TabPlaylists.Style = inactiveStyle;
            TabSettings.Style = inactiveStyle;

            PanelOverview.Visibility = Visibility.Collapsed;
            PanelLiked.Visibility = Visibility.Collapsed;
            PanelPlaylists.Visibility = Visibility.Collapsed;
            PanelSettings.Visibility = Visibility.Collapsed;

            activeTabBtn.Style = (Style)FindResource("ProfileTabActive");
            activePanel.Visibility = Visibility.Visible;
        }
    }
}