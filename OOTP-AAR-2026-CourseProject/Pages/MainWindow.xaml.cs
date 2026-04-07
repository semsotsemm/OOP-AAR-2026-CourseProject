using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace OOTP_AAR_2026_CourseProject
{
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private DispatcherTimer timer;
        public bool isPlaying = false;
        private string currentTrackTitle = "Нет трека";

        // Поле для хранения одного экземпляра окна
        private IslandWindow island;

        public MainWindow()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            // Подписки на события
            this.StateChanged += MainWindow_StateChanged;
            this.Deactivated += MainWindow_Deactivated;
            this.Activated += MainWindow_Activated;

            LoadMusicFromFolder();
        }

        // --- Логика работы Островка ---

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            UpdateIslandVisibility();
        }

        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            // Показываем островок только если музыка действительно играет
            if (isPlaying)
            {
                ShowIsland();
            }
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            // Когда главное окно активно, островок всегда прячется
            HideIsland();
        }

        // Вставьте этот метод или обновите существующий в MainWindow.xaml.cs

        private void ShowIsland()
        {
            // Если музыка на паузе, островок показывать не нужно (опционально)
            if (!isPlaying) return;

            if (island == null)
            {
                island = new IslandWindow();
                island.Topmost = true;
                island.ShowInTaskbar = false;

                // Важно: если окно все-таки будет закрыто (например, системно), 
                // обнуляем переменную, чтобы создать его заново в следующий раз.
                island.Closed += (s, e) => island = null;
            }

            // Передаем данные
            island.TrackTitle.Text = currentTrackTitle;

            // Позиционирование
            island.Left = (SystemParameters.PrimaryScreenWidth - island.Width) / 2;
            island.Top = 0;

            // Безопасный вызов Show
            island.Show();
        }

        private void HideIsland()
        {
            // Просто скрываем, не закрывая
            if (island != null)
            {
                island.Hide();
            }
        }

        private void UpdateIslandVisibility()
        {
            if (this.WindowState == WindowState.Minimized && isPlaying)
            {
                ShowIsland();
            }
            else if (this.WindowState == WindowState.Normal)
            {
                HideIsland();
            }
        }

        // --- Управление воспроизведением ---

        public void TogglePlayPause()
        {
            if (isPlaying)
            {
                mediaPlayer.Pause();
                isPlaying = false;
                HideIsland(); // Прячем островок при паузе
            }
            else if (mediaPlayer.Source != null)
            {
                mediaPlayer.Play();
                isPlaying = true;
                // Если мы запустили музыку, находясь не в фокусе (через островок)
                if (!this.IsActive) ShowIsland();
            }
        }

        public void PlayMusic(string path, string title, string artist)
        {
            currentTrackTitle = title;
            mediaPlayer.Open(new Uri(path));
            mediaPlayer.Play();
            isPlaying = true;
            timer.Start();

            if (island != null && island.IsVisible)
            {
                island.TrackTitle.Text = title;
            }
        }

        // --- Вспомогательные методы ---

        private void LoadMusicFromFolder()
        {
            try
            {
                string musicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "music");
                if (!Directory.Exists(musicPath)) Directory.CreateDirectory(musicPath);

                var files = Directory.GetFiles(musicPath, "*.mp3");
                MusicContainer.Children.Clear();

                foreach (var file in files)
                {
                    var trackControl = new TrackItem(Path.GetFileNameWithoutExtension(file), "Artist", "mp3", file);
                    MusicContainer.Children.Add(trackControl);
                }
            }
            catch { /* Игнорируем ошибки доступа к папкам */ }
        }

        public void NextTrack() { /* Твоя логика */ }
        public void PreviousTrack() { /* Твоя логика */ }

        private void PlayButton_Click(object sender, RoutedEventArgs e) => TogglePlayPause();

        private void Timer_Tick(object sender, EventArgs e) { }
    }
}