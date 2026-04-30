using System.IO;
using Rewind.Pages;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Rewind.Helpers;
using System.Linq;

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

            Loaded += (_, _) => SyncIslandSettingsFromMainWindow();
            LoadOverviewSection();

        }

        private void TabOverview_Click(object sender, RoutedEventArgs e)
        {
            LoadOverviewSection();
            SetActiveTab(TabOverview, PanelOverview);
        }
        private void TabLiked_Click(object sender, RoutedEventArgs e)
        {
            LoadLikedSection();
            SetActiveTab(TabLiked, PanelLiked);
        }
        private void TabPlaylists_Click(object sender, RoutedEventArgs e)
        {
            LoadPlaylistsSection();
            SetActiveTab(TabPlaylists, PanelPlaylists);
        }
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
                tempAvatarPath = CopyImageToProjectFolder(openFileDialog.FileName, "AvatarsLibrary", keepOriginalName: true, returnAbsolutePath: true);
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
                var uri = new Uri($"Resources/Themes/{themeFileName}", UriKind.Relative);
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
                string uniqueFileName = CopyAudioToMusicLibrary(_selectedAudioPath, trackName);
                string destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", uniqueFileName);

                int duration = GetTrackDuration(destPath);

                string? finalCoverPath = null;
                if (!string.IsNullOrWhiteSpace(_selectedCoverPath))
                {
                    finalCoverPath = CopyImageToProjectFolder(_selectedCoverPath, "CoversLibrary", keepOriginalName: true, returnAbsolutePath: true);
                }

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

        private static string CopyAudioToMusicLibrary(string sourcePath, string trackName)
        {
            string extension = Path.GetExtension(sourcePath);
            string safeName = SanitizeFileName(trackName);
            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "track";

            string musicFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary");
            if (!Directory.Exists(musicFolder)) Directory.CreateDirectory(musicFolder);

            string fileName = $"{safeName}{extension}";
            string fullDestPath = Path.Combine(musicFolder, fileName);
            int counter = 1;
            while (File.Exists(fullDestPath))
            {
                fileName = $"{safeName}_{counter}{extension}";
                fullDestPath = Path.Combine(musicFolder, fileName);
                counter++;
            }

            File.Copy(sourcePath, fullDestPath, false);
            return fileName;
        }

        private static string CopyImageToProjectFolder(string sourcePath, string folderName, bool keepOriginalName, bool returnAbsolutePath)
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string extension = Path.GetExtension(sourcePath);
            string baseName = keepOriginalName
                ? Path.GetFileNameWithoutExtension(sourcePath)
                : Guid.NewGuid().ToString();

            string fileName = $"{baseName}{extension}";
            string fullDestPath = Path.Combine(folder, fileName);
            int counter = 1;
            while (File.Exists(fullDestPath))
            {
                fileName = $"{baseName}_{counter}{extension}";
                fullDestPath = Path.Combine(folder, fileName);
                counter++;
            }

            File.Copy(sourcePath, fullDestPath, false);
            return returnAbsolutePath ? fullDestPath : fileName;
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var chars = name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
            return new string(chars).Trim();
        }
        private void SetActiveTab(Button activeTabBtn, UIElement activePanel)
        {
            Style inactiveStyle = (Style)FindResource("ProfileTab");

            TabOverview.Style = TabLiked.Style = TabPlaylists.Style =
            TabSettings.Style = inactiveStyle;

            if (Session.UserRole?.ToLower() == "исполнитель")
            {
                TabArtistStudio.Style = inactiveStyle;
            }

            PanelOverview.Visibility = PanelLiked.Visibility =
            PanelPlaylists.Visibility = PanelSettings.Visibility =
            PanelArtistStudio.Visibility = Visibility.Collapsed;

            activeTabBtn.Style = (Style)FindResource("ProfileTabActive");
            activePanel.Visibility = Visibility.Visible;
        }

        private void LoadOverviewSection()
        {
            OverviewPlaylistsContainer.Children.Clear();
            OverviewLikedContainer.Children.Clear();

            var ownPlaylists = Session.CachedPlaylists.Where(p => p.OwnerID == Session.UserId).Take(6).ToList();
            if (ownPlaylists.Count == 0)
            {
                OverviewPlaylistsContainer.Children.Add(MakeEmptyCard("Плейлистов пока нет"));
            }
            else
            {
                foreach (var playlist in ownPlaylists)
                {
                    var trackCount = playlist.PlaylistTracks?.Count ?? 0;
                    OverviewPlaylistsContainer.Children.Add(MakeOverviewCard(playlist.Title, $"{trackCount} треков"));
                }
            }

            var likedTracks = Session.LikedTrackIds
                .Take(6)
                .Select(id => TrackService.GetTrackById(id))
                .Where(t => t != null)
                .Cast<Track>()
                .ToList();

            if (likedTracks.Count == 0)
            {
                OverviewLikedContainer.Children.Add(MakeEmptyCard("Лайков пока нет"));
            }
            else
            {
                foreach (var track in likedTracks)
                {
                    var artist = UserService.GetUserById(track.ArtistID)?.Nickname ?? "Неизвестный артист";
                    OverviewLikedContainer.Children.Add(MakeOverviewCard(track.Title, artist));
                }
            }
        }

        private void LoadLikedSection()
        {
            LikedTracksContainer.Children.Clear();
            var tracks = TrackService.GetAllTracks().Take(25).ToList();
            if (tracks.Count == 0)
            {
                LikedTracksContainer.Children.Add(MakeEmptyCard("Треков пока нет"));
                return;
            }

            foreach (var track in tracks)
            {
                var artist = UserService.GetUserById(track.ArtistID)?.Nickname ?? "Неизвестный артист";
                LikedTracksContainer.Children.Add(MakeOverviewCard(track.Title, artist));
            }
        }

        private void LoadPlaylistsSection()
        {
            ProfilePlaylistsContainer.Children.Clear();
            var ownPlaylists = Session.CachedPlaylists.Where(p => p.OwnerID == Session.UserId).ToList();
            if (ownPlaylists.Count == 0)
            {
                ProfilePlaylistsContainer.Children.Add(MakeEmptyCard("Плейлистов пока нет"));
                return;
            }

            foreach (var playlist in ownPlaylists)
            {
                var trackCount = playlist.PlaylistTracks?.Count ?? 0;
                var likes = PlaylistAnalyticsService.GetLikesCount(playlist.PlaylistID);
                var listens = PlaylistAnalyticsService.GetListenersCount(playlist.PlaylistID);
                ProfilePlaylistsContainer.Children.Add(MakeOverviewCard(playlist.Title, $"{trackCount} треков • ♥ {likes} • ▶ {listens}"));
            }
        }

        private void OpenPlaylistsPage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.ShowPlaylistsPage();
        }

        private void SyncIslandSettingsFromMainWindow()
        {
            if (Window.GetWindow(this) is not MainWindow mainWindow) return;
            IslandEnabledProfileToggle.IsChecked = mainWindow.IslandEnabled;
            IslandSizeProfileSlider.Value = mainWindow.IslandScale;
            IslandOpacityProfileSlider.Value = mainWindow.IslandOpacity;
        }

        private void IslandProfileSettings_Changed(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is not MainWindow mainWindow) return;
            mainWindow.UpdateIslandSettings(
                IslandEnabledProfileToggle.IsChecked == true,
                IslandOpacityProfileSlider.Value);
        }

        private UIElement MakeOverviewCard(string title, string subtitle)
        {
            var card = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 244, 240)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            stack.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 12,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 128)),
                Margin = new Thickness(0, 4, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            card.Child = stack;
            return card;
        }

        private UIElement MakeEmptyCard(string text)
        {
            return new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 244, 240)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 12,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 128))
                }
            };
        }
    }
}