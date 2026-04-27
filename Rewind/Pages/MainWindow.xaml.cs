using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Rewind.Controls;
using Rewind.Tabs.UsersTabs;


namespace Rewind
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MainContentArea.Content = new MainPage();

            HighlightActiveButton(BtnHome);
        }

        private void ShowProfile_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new ProfilePage();
            HighlightActiveButton(BtnProfile);
        }
        private void ShowPlaylists_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new PlaylistsPage();
            HighlightActiveButton(BtnPlaylists);
        }
        private void ShowHome_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new MainPage();
            HighlightActiveButton(BtnHome);
        }
        private void ShowLiked_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new FavoritesPage();
            HighlightActiveButton(BtnFavorites);
        }
        private void HighlightActiveButton(Button activeBtn)
        {
            Button[] menuButtons = { BtnHome, BtnSearch, BtnFavorites, BtnPlaylists, BtnProfile };

            var activeAccent = (Brush)FindResource("AccentColor");
            var activeIconFill = Brushes.White;

            var inactiveBg = (Brush)new BrushConverter().ConvertFrom("#F0EFEB");
            var inactiveText = (Brush)new BrushConverter().ConvertFrom("#888880");

            foreach (var btn in menuButtons)
            {
                bool isActive = (btn == activeBtn);

                btn.Style = (Style)FindResource(isActive ? "ActiveNavButtonStyle" : "NavButtonStyle");

                if (btn.Content is StackPanel sp)
                {
                    if (sp.Children[0] is Border iconBorder)
                    {
                        iconBorder.Background = isActive ? activeAccent : inactiveBg;

                        if (iconBorder.Child is Path iconPath)
                        {
                            if (iconPath.Fill != null && iconPath.Fill != Brushes.Transparent)
                            {
                                iconPath.Fill = isActive ? activeIconFill : inactiveText;
                            }
                            if (iconPath.Stroke != null && iconPath.Stroke != Brushes.Transparent)
                            {
                                iconPath.Stroke = isActive ? activeIconFill : inactiveText;
                            }
                        }
                    }

                    if (sp.Children.Count > 1 && sp.Children[1] is TextBlock textBlock)
                    {
                        textBlock.Foreground = isActive ? activeAccent : inactiveText;
                        textBlock.FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal;
                    }
                }
            }
        }
    }
}