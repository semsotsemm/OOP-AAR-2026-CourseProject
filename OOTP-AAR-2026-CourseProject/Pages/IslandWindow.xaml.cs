using System.Windows;
using System.Windows.Input;

namespace OOTP_AAR_2026_CourseProject
{
    public partial class IslandWindow : Window
    {
        public IslandWindow()
        {
            InitializeComponent();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Для Alt + Q нужно проверять e.SystemKey
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

            if (key == Key.Q && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                this.Hide(); // Прячем окно
                e.Handled = true; // Помечаем событие как обработанное
            }
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as MainWindow)?.PreviousTrack();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as MainWindow)?.TogglePlayPause();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as MainWindow)?.NextTrack();
        }
    }
}