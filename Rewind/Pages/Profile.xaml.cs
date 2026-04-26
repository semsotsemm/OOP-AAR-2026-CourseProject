using System.Windows;
using System.Windows.Controls;

namespace Rewind.Pages
{
    public partial class Profile : Window
    {
        public Profile()
        {
            InitializeComponent();
        }

        // Обработчики нажатий на кнопки
        private void TabOverview_Click(object sender, RoutedEventArgs e) => SetActiveTab(TabOverview, PanelOverview);
        private void TabLiked_Click(object sender, RoutedEventArgs e) => SetActiveTab(TabLiked, PanelLiked);
        private void TabPlaylists_Click(object sender, RoutedEventArgs e) => SetActiveTab(TabPlaylists, PanelPlaylists);
        private void TabSettings_Click(object sender, RoutedEventArgs e) => SetActiveTab(TabSettings, PanelSettings);

        // Общий метод для переключения вкладок
        private void SetActiveTab(Button activeTabBtn, UIElement activePanel)
        {
            // 1. Сбрасываем стили ВСЕХ кнопок на базовые (неактивные)
            Style inactiveStyle = (Style)FindResource("ProfileTab");
            TabOverview.Style = inactiveStyle;
            TabLiked.Style = inactiveStyle;
            TabPlaylists.Style = inactiveStyle;
            TabSettings.Style = inactiveStyle;

            // 2. Скрываем ВСЕ панели
            PanelOverview.Visibility = Visibility.Collapsed;
            PanelLiked.Visibility = Visibility.Collapsed;
            PanelPlaylists.Visibility = Visibility.Collapsed;
            PanelSettings.Visibility = Visibility.Collapsed;

            // 3. Делаем активной только ВЫБРАННУЮ кнопку и панель
            activeTabBtn.Style = (Style)FindResource("ProfileTabActive");
            activePanel.Visibility = Visibility.Visible;
        }
    }
}