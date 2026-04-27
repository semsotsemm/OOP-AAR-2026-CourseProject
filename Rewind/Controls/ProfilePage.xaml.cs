using System.IO;
using Rewind.Pages;
using System.Windows;
using Microsoft.Win32;
using Rewind.DbManager;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Rewind.Controls
{
    public partial class ProfilePage : UserControl
    {
        private string? _selectedCoverPath;
        private string? _selectedAudioPath; 
        private string tempAvatarPath = Session.AvatarPath;

        public ProfilePage()
        {
            InitializeComponent();
            if (Session.UserRole?.ToLower() == "исполнитель")
            {
                TabArtistStudio.Visibility = Visibility.Visible;
            }
        }

        private void TabOverview_Click(object sender, RoutedEventArgs e) => SetActiveTab(TabOverview, PanelOverview);
        private void TabLiked_Click(object sender, RoutedEventArgs e) => SetActiveTab(TabLiked, PanelLiked);
        private void TabPlaylists_Click(object sender, RoutedEventArgs e) => SetActiveTab(TabPlaylists, PanelPlaylists);
        private void TabSettings_Click(object sender, RoutedEventArgs e) => SetActiveTab(TabSettings, PanelSettings);

        private void EditBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            EditNameInput.Text = Session.UserName;
            EditEmailInput.Text = Session.Email;
            EditPassInput.Password = Session.Password;
            EditOverlay.Visibility = Visibility.Visible;
        }

        private void CloseEdit_Click(object sender, RoutedEventArgs e)
        {
            EditOverlay.Visibility = Visibility.Collapsed;
        }

        private void UploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Изображения (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";

            if (openFileDialog.ShowDialog() == true)
            {
                tempAvatarPath = openFileDialog.FileName;
                AvatarPreview.ImageSource = new BitmapImage(new Uri(tempAvatarPath));
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(EditNameInput.Text) && !string.IsNullOrWhiteSpace(EditEmailInput.Text))
            {
                Session.UserName = EditNameInput.Text;
                Session.Email = EditEmailInput.Text;
                Session.Password = EditPassInput.Password;
                Session.HidedPassword = new string('●', Session.Password.Length);
                Session.AvatarPath = tempAvatarPath;

                MessageBox.Show("Данные аккаунта успешно изменены");
                EditOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("Ошибка ввода, проверь данные");
            }
        }

        private void LogOut_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Registration registration_page = new Registration();
            registration_page.Show();

            Window.GetWindow(this)?.Close();
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
        private void TabArtistStudio_Click(object sender, RoutedEventArgs e)
        {
            ResetTabStyles();
            TabArtistStudio.Style = (Style)FindResource("ProfileTabActive");

            PanelOverview.Visibility = Visibility.Collapsed;
            PanelLiked.Visibility = Visibility.Collapsed;
            PanelPlaylists.Visibility = Visibility.Collapsed;
            PanelSettings.Visibility = Visibility.Collapsed;

            PanelArtistStudio.Visibility = Visibility.Visible;
        }
        private void ResetTabStyles()
        {
            TabOverview.Style = (Style)FindResource("ProfileTab");
            TabLiked.Style = (Style)FindResource("ProfileTab");
            TabPlaylists.Style = (Style)FindResource("ProfileTab");
            TabSettings.Style = (Style)FindResource("ProfileTab");

            TabArtistStudio.Style = (Style)FindResource("ProfileTab");
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
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);

                Application.Current.Resources.MergedDictionaries.Add(newDict);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке темы: {ex.Message}");
            }
        }

        private void SelectCover_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();

            dialog.Filter = "Images|*.jpg;*.png;*.jpeg";

            if (dialog.ShowDialog() == true)
            {
                _selectedCoverPath = dialog.FileName;
                NewTrackCoverPreview.Source = new BitmapImage(new Uri(_selectedCoverPath));
                CoverPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void SelectAudio_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Audio Files|*.mp3;*.wav";
            if (dialog.ShowDialog() == true)
            {
                _selectedAudioPath = dialog.FileName; // Сохраняем полный путь
                AudioFileName.Text = System.IO.Path.GetFileName(dialog.FileName); // Для UI только имя
            }
        }
        private void UploadTrack_Click(object sender, RoutedEventArgs e)
        {
            string trackName = NewTrackName.Text;

            // Проверка: выбраны ли все данные
            if (string.IsNullOrWhiteSpace(trackName) || string.IsNullOrEmpty(_selectedAudioPath))
            {
                MessageBox.Show("Введите название и выберите аудиофайл!");
                return;
            }

            try
            {
                // Используем сохраненный путь _selectedAudioPath
                string extension = System.IO.Path.GetExtension(_selectedAudioPath);

                string musicFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary");
                if (!Directory.Exists(musicFolder)) Directory.CreateDirectory(musicFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + extension;
                string destPath = Path.Combine(musicFolder, uniqueFileName);

                // Копируем файл
                File.Copy(_selectedAudioPath, destPath, true);

                int duration = GetTrackDuration(destPath);

                // Копирование обложки (советую тоже сделать копию в папку приложения)
                string? finalCoverPath = _selectedCoverPath;
                // Если хочешь, чтобы обложка тоже была относительной, сделай для неё такую же логику копирования

                Track newTrack = new Track
                {
                    Title = trackName,
                    FilePath = uniqueFileName,
                    CoverPath = finalCoverPath,
                    Duration = duration,
                    UploadDate = DateTime.UtcNow,
                    ArtistID = Session.UserId,
                };

                TrackService.AddTrack(newTrack);
                MessageBox.Show("Трек успешно добавлен!");

                // Очистка полей после успеха
                NewTrackName.Clear();
                AudioFileName.Text = "Файл не выбран";
                _selectedAudioPath = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private int GetTrackDuration(string path)
        {
            using (var file = TagLib.File.Create(path))
            {
                return (int)file.Properties.Duration.TotalSeconds;
            }
        }
        private void SetActiveTab(Button activeTabBtn, UIElement activePanel)
        {
            Style inactiveStyle = (Style)FindResource("ProfileTab");

            TabOverview.Style = TabLiked.Style = TabPlaylists.Style =
            TabSettings.Style = TabArtistStudio.Style = inactiveStyle;

            PanelOverview.Visibility = PanelLiked.Visibility =
            PanelPlaylists.Visibility = PanelSettings.Visibility =
            PanelArtistStudio.Visibility = Visibility.Collapsed;

            activeTabBtn.Style = (Style)FindResource("ProfileTabActive");
            activePanel.Visibility = Visibility.Visible;
        }
    }
}