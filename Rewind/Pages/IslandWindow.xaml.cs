using Rewind.Tabs.UsersTabs;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;

namespace Rewind
{
    public partial class IslandWindow : Window
    {
        public IslandWindow()
        {
            InitializeComponent();
        }

        // Вспомогательный метод для поиска MainPage в любом открытом окне
        private MainPage GetActivePlayerPage()
        {
            // Ищем среди всех открытых окон то, в котором внутри сидит MainPage
            foreach (Window window in Application.Current.Windows)
            {
                // Проверяем разные варианты названий контейнеров (Frame или ContentControl)
                // Если у тебя плеер внутри Frame с именем MainContentFrame:
                if (window.FindName("MainContentFrame") is Frame frame && frame.Content is MainPage page)
                {
                    return page;
                }

                // Если у тебя плеер просто лежит как контент окна:
                if (window.Content is MainPage directPage)
                {
                    return directPage;
                }
            }
            return null;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            if (key == Key.Q && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                this.Hide();
                e.Handled = true;
            }
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            GetActivePlayerPage()?.PreviousTrack();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            // Убедись, что метод TogglePlayPause публичный (public) в MainPage.xaml.cs
            GetActivePlayerPage()?.TogglePlayPause();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            GetActivePlayerPage()?.NextTrack();
        }
    }
}