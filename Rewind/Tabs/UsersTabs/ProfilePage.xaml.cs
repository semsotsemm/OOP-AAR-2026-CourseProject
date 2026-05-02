using Microsoft.Win32;
using Rewind.Helpers;
using Rewind.Pages;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rewind.Controls
{
    public partial class ProfilePage : UserControl
    {
        private string? _selectedCoverPath;
        private string? _selectedAudioPath;
        private string tempAvatarPath = Session.AvatarPath;

        public event EventHandler ThemeChanged;

        public ProfilePage()
        {
            InitializeComponent();
            // Studio is now a separate sidebar nav item — always hide it in profile
            TabArtistStudio.Visibility = Visibility.Collapsed;

            Loaded += (_, _) =>
            {
                SyncIslandSettingsFromMainWindow();
                RestoreThemeCard();  // восстанавливаем активную тему из Session

                // Синхронизация тогглов уведомлений с Session
                Tog1.IsChecked = Session.NotifNewTracksEnabled;
                Tog2.IsChecked = Session.NotifPushEnabled;
                Tog1.Checked   += (_, _) => Session.NotifNewTracksEnabled = true;
                Tog1.Unchecked += (_, _) => Session.NotifNewTracksEnabled = false;
                Tog2.Checked   += (_, _) => Session.NotifPushEnabled = true;
                Tog2.Unchecked += (_, _) => Session.NotifPushEnabled = false;
            };

            PlaylistListenService.OnPlaylistListenChanged += OnPlaylistStatsChanged;
            SavedPlaylistService.OnPlaylistSavedChanged += OnPlaylistStatsChanged;
            Unloaded += (_, _) =>
            {
                PlaylistListenService.OnPlaylistListenChanged -= OnPlaylistStatsChanged;
                SavedPlaylistService.OnPlaylistSavedChanged -= OnPlaylistStatsChanged;
            };

            LoadOverviewSection();
        }

        private void OnPlaylistStatsChanged(int playlistId)
        {
            Dispatcher.Invoke(() =>
            {
                if (PanelOverview.Visibility == Visibility.Visible) LoadOverviewSection();
                if (PanelPlaylists.Visibility == Visibility.Visible) LoadPlaylistsSection();
            });
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
        private void TabAlbums_Click(object sender, RoutedEventArgs e)
        {
            LoadAlbumsSection();
            SetActiveTab(TabAlbums, PanelAlbums);
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
                tempAvatarPath = FileStorage.CopyAvatar(openFileDialog.FileName);
                AvatarPreview.ImageSource = new BitmapImage(new Uri(FileStorage.ResolvePath(tempAvatarPath)));
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

           Session.FlushToDatabase();

            Application.Current.MainWindow = registration_page;

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

                // Сохраняем выбранную тему в Session
                Session.ActiveTheme = clickedBorder.Name;

                ApplyTheme(themeFile);
            }
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Восстанавливает активную карточку темы согласно Session.ActiveTheme.</summary>
        private void RestoreThemeCard()
        {
            Border activeCard = Session.ActiveTheme switch
            {
                "ThemePink" => ThemePink,
                "ThemeMidnight" => ThemeMidnight,
                "ThemeLavender" => ThemeLavender,
                _ => ThemeClassic
            };
            UpdateActiveCard(activeCard);
        }
        private void TabArtistStudio_Click(object sender, RoutedEventArgs e)
        {
            ResetTabStyles();
            TabArtistStudio.Style = (Style)FindResource("ProfileTabActive");

            PanelOverview.Visibility = Visibility.Collapsed;
            PanelLiked.Visibility = Visibility.Collapsed;
            PanelPlaylists.Visibility = Visibility.Collapsed;
            PanelAlbums.Visibility = Visibility.Collapsed;
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
                string uniqueFileName = FileStorage.CopyTrackAudio(_selectedAudioPath, trackName);
                string destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", uniqueFileName);

                int duration = GetTrackDuration(destPath);

                string? finalCoverPath = null;
                if (!string.IsNullOrWhiteSpace(_selectedCoverPath))
                {
                    finalCoverPath = FileStorage.CopyTrackCover(_selectedCoverPath);
                }

                string genre = (GenreSelector.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "";

                Track newTrack = new Track
                {
                    Title = trackName,
                    FilePath = uniqueFileName,
                    CoverPath = finalCoverPath,
                    Duration = duration,
                    UploadDate = DateTime.UtcNow,
                    ArtistID = Session.UserId,
                    Genre = genre,
                    PublishStatus = "Pending"
                };

                TrackService.AddTrack(newTrack);
                MessageBox.Show("Трек отправлен на проверку администратору. После одобрения он появится на платформе.", "Заявка отправлена");

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
            TabAlbums.Style = TabSettings.Style = inactiveStyle;

            if (Session.UserRole?.ToLower() == "исполнитель")
            {
                TabArtistStudio.Style = inactiveStyle;
            }

            PanelOverview.Visibility = PanelLiked.Visibility =
            PanelPlaylists.Visibility = PanelAlbums.Visibility = PanelSettings.Visibility =
            PanelArtistStudio.Visibility = Visibility.Collapsed;

            activeTabBtn.Style = (Style)FindResource("ProfileTabActive");
            activePanel.Visibility = Visibility.Visible;
        }

        private void LoadOverviewSection()
        {
            OverviewPlaylistsContainer.Children.Clear();
            OverviewLikedContainer.Children.Clear();

            // Собственные + сохранённые плейлисты
            var ownPlaylists = Session.CachedPlaylists.Where(p => p.OwnerID == Session.UserId).Take(6).ToList();
            List<Playlist> savedPlaylists = new();
            try { savedPlaylists = SavedPlaylistService.GetSavedByUser(Session.UserId).Take(6).ToList(); } catch { }

            var allDisplayPlaylists = ownPlaylists
                .Concat(savedPlaylists.Where(sp => !ownPlaylists.Any(op => op.PlaylistID == sp.PlaylistID)))
                .Take(6).ToList();

            if (allDisplayPlaylists.Count == 0)
            {
                OverviewPlaylistsContainer.Children.Add(MakeEmptyCard("Плейлистов пока нет"));
            }
            else
            {
                foreach (var playlist in allDisplayPlaylists)
                {
                    var trackCount = playlist.PlaylistTracks?.Count ?? 0;
                    var isSaved = playlist.OwnerID != Session.UserId;
                    int saves = 0, listens = 0;
                    try { saves = SavedPlaylistService.GetSavedCount(playlist.PlaylistID); } catch { }
                    try { listens = PlaylistListenService.GetListenerCount(playlist.PlaylistID); } catch { }
                    var label = isSaved
                        ? $"{trackCount} тр.  •  ♥ {saves}  •  ► {listens}"
                        : $"{trackCount} тр.  •  ► {listens}";
                    OverviewPlaylistsContainer.Children.Add(MakePlaylistCard(playlist, label));
                }
            }

            // Лайкнутые треки — богатые карточки с обложкой и кнопкой воспроизведения
            var likedTracks = Session.LikedTrackIds
                .Take(6)
                .Select(id => TrackService.GetTrackById(id))
                .Where(t => t != null)
                .Cast<Track>()
                .ToList();

            // Build a play context list from all liked tracks for overview playback
            var allLikedForPlay = Session.LikedTrackIds
                .Select(id => TrackService.GetTrackById(id))
                .Where(t => t != null)
                .Cast<Track>()
                .Select(t =>
                {
                    var fp = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", t.FilePath);
                    var artist = UserService.GetUserById(t.ArtistID)?.Nickname ?? "Неизвестный";
                    return new Rewind.Contols.TrackItem(t.TrackID, t.Title, artist,
                        FormatDuration(t.Duration), fp, t.CoverPath, t.Duration);
                })
                .ToList();

            if (likedTracks.Count == 0)
            {
                OverviewLikedContainer.Children.Add(MakeEmptyCard("Лайков пока нет"));
            }
            else
            {
                foreach (var track in likedTracks)
                {
                    var artist = UserService.GetUserById(track.ArtistID)?.Nickname ?? "Неизвестный";
                    OverviewLikedContainer.Children.Add(
                        MakeLikedTrackCard(track, artist, allLikedForPlay));
                }
            }
        }

        private static string FormatDuration(int sec)
            => $"{sec / 60}:{sec % 60:D2}";

        /// <summary>Rich card for a liked track with cover, duration and play button.</summary>
        private static UIElement MakeLikedTrackCard(
            Track track, string artist,
            List<Rewind.Contols.TrackItem> playContext)
        {
            var card = new Border
            {

                Background = (Brush)Application.Current.TryFindResource("BgCard") ?? new SolidColorBrush(Color.FromRgb(245, 244, 240)),
                CornerRadius = new CornerRadius(12),    
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            // Cover
            var coverBorder = new Border
            {
                Width = 40, Height = 40, CornerRadius = new CornerRadius(8),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(200, 200, 195)),
                Margin = new Thickness(0, 0, 10, 0)
            };
            if (!string.IsNullOrEmpty(track.CoverPath))
            {
                try
                {
                    string fp = track.CoverPath.Contains(":")
                        ? track.CoverPath
                        : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoversLibrary", track.CoverPath);
                    if (System.IO.File.Exists(fp))
                    {
                        coverBorder.Background = new System.Windows.Media.ImageBrush(
                            new System.Windows.Media.Imaging.BitmapImage(new Uri(fp)))
                        { Stretch = System.Windows.Media.Stretch.UniformToFill };
                    }
                }
                catch { }
            }
            else
            {
                coverBorder.Child = new Image
                {
                    Source = IconAssets.LoadBitmap("music_note.png"),
                    Width = 22, Height = 22,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            Grid.SetColumn(coverBorder, 0);

            // Info
            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            infoStack.Children.Add(new TextBlock
            {
                Text = track.Title, FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextPrimary"],
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            infoStack.Children.Add(new TextBlock
            {
                Text = $"{artist}  •  {FormatDuration(track.Duration)}",
                FontSize = 11,
                Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextSecondary"],
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(infoStack, 1);

            // Play button
            var playBtn = new Border
            {
                Width = 32, Height = 32, CornerRadius = new CornerRadius(16),
                Background = (System.Windows.Media.Brush)Application.Current.Resources["AccentColor"],
                Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            playBtn.Child = new TextBlock
            {
                Text = "▶", FontSize = 12,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            int capturedId = track.TrackID;
            playBtn.MouseLeftButtonDown += (s, ev) =>
            {
                ev.Handled = true;
                if (Application.Current.MainWindow is MainWindow mw)
                {
                    var selected = playContext.FirstOrDefault(ti => ti.TrackId == capturedId);
                    if (selected != null) mw.PlayTrackFromContext(selected, playContext);
                }
            };
            Grid.SetColumn(playBtn, 2);

            // Card click also plays
            card.MouseLeftButtonDown += (s, ev) =>
            {
                if (Application.Current.MainWindow is MainWindow mw)
                {
                    var selected = playContext.FirstOrDefault(ti => ti.TrackId == capturedId);
                    if (selected != null) mw.PlayTrackFromContext(selected, playContext);
                }
            };

            grid.Children.Add(coverBorder);
            grid.Children.Add(infoStack);
            grid.Children.Add(playBtn);
            card.Child = grid;
            return card;
        }

        private void LoadLikedSection()
        {
            LikedTracksContainer.Children.Clear();

            var likedIds = Session.LikedTrackIds;
            var tracks = likedIds
                .Select(id => TrackService.GetTrackById(id))
                .Where(t => t != null)
                .Cast<Track>()
                .Take(25)
                .ToList();

            if (tracks.Count == 0)
            {
                LikedTracksContainer.Children.Add(MakeEmptyCard("Лайков пока нет"));
                return;
            }

            // Build play context for all liked tracks
            var playContext = tracks
                .Select(t =>
                {
                    var fp = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", t.FilePath);
                    var artist = UserService.GetUserById(t.ArtistID)?.Nickname ?? "Неизвестный";
                    return new Rewind.Contols.TrackItem(t.TrackID, t.Title, artist,
                        FormatDuration(t.Duration), fp, t.CoverPath, t.Duration);
                })
                .ToList();

            foreach (var track in tracks)
            {
                var artist = UserService.GetUserById(track.ArtistID)?.Nickname ?? "Неизвестный";
                LikedTracksContainer.Children.Add(MakeLikedTrackCard(track, artist, playContext));
            }
        }

        private void LoadPlaylistsSection()
        {
            ProfilePlaylistsContainer.Children.Clear();

            var ownPlaylists = Session.CachedPlaylists.Where(p => p.OwnerID == Session.UserId).ToList();
            List<Playlist> savedPlaylists = new();
            try { savedPlaylists = SavedPlaylistService.GetSavedByUser(Session.UserId); } catch { }

            if (ownPlaylists.Count == 0 && savedPlaylists.Count == 0)
            {
                ProfilePlaylistsContainer.Children.Add(MakeEmptyCard("Плейлистов пока нет"));
                return;
            }

            if (ownPlaylists.Count > 0)
            {
                ProfilePlaylistsContainer.Children.Add(MakeSectionHeader("Мои плейлисты"));
                foreach (var playlist in ownPlaylists)
                {
                    var trackCount = playlist.PlaylistTracks?.Count ?? 0;
                    int saves = 0, listens = 0;
                    try { saves = SavedPlaylistService.GetSavedCount(playlist.PlaylistID); } catch { }
                    try { listens = PlaylistListenService.GetListenerCount(playlist.PlaylistID); } catch { }
                    ProfilePlaylistsContainer.Children.Add(
                        MakePlaylistCard(playlist, $"{trackCount} тр.  •  ♥ {saves}  •  ► {listens}"));
                }
            }

            var uniqueSaved = savedPlaylists
                .Where(sp => !ownPlaylists.Any(op => op.PlaylistID == sp.PlaylistID))
                .ToList();
            if (uniqueSaved.Count > 0)
            {
                foreach (var playlist in uniqueSaved)
                {
                    var trackCount = playlist.PlaylistTracks?.Count ?? 0;
                    int listens = 0;
                    try { listens = PlaylistListenService.GetListenerCount(playlist.PlaylistID); } catch { }
                    var owner = UserService.GetUserById(playlist.OwnerID)?.Nickname ?? "Неизвестный";
                    ProfilePlaylistsContainer.Children.Add(
                        MakePlaylistCard(playlist, $"От {owner}  •  {trackCount} тр.  •  ► {listens}"));
                }
            }
        }

        private void LoadAlbumsSection()
        {
            ProfileAlbumsContainer.Children.Clear();
            List<Album> albums;
            try { albums = AlbumService.GetSavedByUser(Session.UserId); }
            catch { albums = new List<Album>(); }

            if (albums.Count == 0)
            {
                ProfileAlbumsContainer.Children.Add(MakeEmptyCard("Сохранённых альбомов пока нет"));
                return;
            }

            foreach (var album in albums)
                ProfileAlbumsContainer.Children.Add(MakeAlbumCard(album));
        }

        private UIElement MakeAlbumCard(Album album)
        {
            var card = new Border
            {
                Width = 170,
                Height = 205,
                CornerRadius = new CornerRadius(16),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 244, 240)),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 12, 12),
                Cursor = Cursors.Hand
            };
            var stack = new StackPanel();
            var cover = new Border
            {
                Height = 110,
                CornerRadius = new CornerRadius(12),
                ClipToBounds = true,
                Background = (Brush?)Application.Current.TryFindResource("GreenGradientStyle")
                             ?? new LinearGradientBrush(Color.FromRgb(42, 232, 118), Color.FromRgb(0, 77, 64), new Point(0, 0), new Point(1, 1))
            };
            bool hasCover = false;
            if (!string.IsNullOrWhiteSpace(album.CoverPath))
            {
                try
                {
                    string fp = FileStorage.ResolveImagePath(album.CoverPath, "AlbumCovers");
                    if (File.Exists(fp))
                    {
                        cover.Background = new ImageBrush(new BitmapImage(new Uri(fp))) { Stretch = Stretch.UniformToFill };
                        hasCover = true;
                    }
                }
                catch { }
            }
            if (!hasCover)
            {
                cover.Child = new Image
                {
                    Source = IconAssets.LoadBitmap("music_note.png"),
                    Width = 44, Height = 44,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            stack.Children.Add(cover);
            stack.Children.Add(new TextBlock { Text = album.Title, FontWeight = FontWeights.Bold, FontSize = 13, Margin = new Thickness(0, 10, 0, 2), TextTrimming = TextTrimming.CharacterEllipsis });
            var artist = album.Artist?.Nickname ?? UserService.GetUserById(album.ArtistId)?.Nickname ?? "Исполнитель";
            stack.Children.Add(new TextBlock { Text = $"{artist} • {album.AlbumTracks?.Count ?? 0} треков", FontSize = 11, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(136,136,128)) });
            card.Child = stack;
            int albumId = album.AlbumId;
            card.MouseLeftButtonDown += (_, _) =>
            {
                var full = AlbumService.GetById(albumId) ?? album;
                if (Window.GetWindow(this) is MainWindow mw) mw.OpenAlbumDetails(full);
            };
            return card;
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
            IslandOpacityProfileSlider.Value = mainWindow.IslandOpacity;
        }

        private void IslandProfileSettings_Changed(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is not MainWindow mainWindow) return;
            mainWindow.UpdateIslandSettings(
                IslandEnabledProfileToggle.IsChecked == true,
                IslandOpacityProfileSlider.Value);
        }

        private UIElement MakePlaylistCard(Playlist playlist, string subtitle)
        {
            var card = new Border
            {
                Background = (Brush)Application.Current.TryFindResource("BgCard") ?? new SolidColorBrush(Color.FromRgb(245, 244, 240)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Cover image
            var coverBorder = new Border
            {
                Width = 48, Height = 48, CornerRadius = new CornerRadius(8),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 200, 185)),
                Margin = new Thickness(0, 0, 8, 0)
            };
            if (!string.IsNullOrEmpty(playlist.CoverPath))
            {
                try
                {
                    string fp = playlist.CoverPath.Contains(":")
                        ? playlist.CoverPath
                        : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoversLibrary", playlist.CoverPath);
                    if (System.IO.File.Exists(fp))
                        coverBorder.Background = new System.Windows.Media.ImageBrush(
                            new BitmapImage(new Uri(fp)))
                        { Stretch = System.Windows.Media.Stretch.UniformToFill };
                    else
                        coverBorder.Child = new Image { Source = IconAssets.LoadBitmap("music_note.png"), Width = 24, Height = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                }
                catch { coverBorder.Child = new Image { Source = IconAssets.LoadBitmap("music_note.png"), Width = 24, Height = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }; }
            }
            else
            {
                coverBorder.Child = new Image { Source = IconAssets.LoadBitmap("music_note.png"), Width = 24, Height = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            }
            Grid.SetColumn(coverBorder, 0);

            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            info.Children.Add(new TextBlock
            {
                Text = playlist.Title,
                FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.TryFindResource("TextPrimary") ?? new SolidColorBrush(Color.FromRgb(245, 244, 240)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            info.Children.Add(new TextBlock
            {
                Text = subtitle, FontSize = 11,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 128)),
                Margin = new Thickness(0, 3, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            Grid.SetColumn(info, 1);

            grid.Children.Add(coverBorder);
            grid.Children.Add(info);
            card.Child = grid;

            // Navigate to playlist on click
            card.MouseLeftButtonDown += (s, ev) =>
            {
                try
                {
                    var full = playlist.PlaylistID > 0
                        ? PlaylistService.GetPlaylistById(playlist.PlaylistID) ?? playlist
                        : playlist;
                    if (Window.GetWindow(this) is MainWindow mw)
                        mw.OpenPlaylistDetails(full);
                }
                catch { }
            };

            return card;
        }

        private UIElement MakeSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14, 
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 24)),
                Margin = new Thickness(0, 10, 0, 8)
            };
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

                Background = (Brush)Application.Current.TryFindResource("BgCard") ?? new SolidColorBrush(Color.FromRgb(245, 244, 240)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 12,
                    Foreground = (Brush)Application.Current.TryFindResource("TextPrimary") ?? new SolidColorBrush(Color.FromRgb(245, 244, 240))
                }
            };
        }
    }
}
