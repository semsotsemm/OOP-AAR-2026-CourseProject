using Rewind.Helpers;
using Rewind.MVVM.Core;
using Rewind.MVVM.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Pages
{
    /// <summary>
    /// VM страницы профиля. Управляет:
    ///   • активной вкладкой (Overview/Liked/Playlists/Albums/Settings/ArtistStudio);
    ///   • редактированием профиля (имя/email/пароль/аватар);
    ///   • выбором темы;
    ///   • загрузкой трека (для исполнителей);
    ///   • перезагрузкой данных вкладок (плейлисты/лайки/альбомы).
    /// View занимается только императивным рендером карточек.
    /// </summary>
    public sealed class ProfilePageViewModel : ViewModelBase, IDisposable
    {
        private readonly IDialogService _dialog;

        public ProfilePageViewModel(IDialogService dialog)
        {
            _dialog = dialog;

            OverviewPlaylists = new ObservableCollection<Playlist>();
            OverviewLikedTracks = new ObservableCollection<Track>();
            ProfilePlaylistsOwn = new ObservableCollection<Playlist>();
            ProfilePlaylistsSaved = new ObservableCollection<Playlist>();
            LikedTracks = new ObservableCollection<Track>();
            SavedAlbums = new ObservableCollection<Album>();

            SelectTabCommand = new RelayCommand<string>(name => { if (name != null) ActiveTab = name; });
            SaveProfileCommand = new RelayCommand(SaveProfile);
            LogoutCommand = new RelayCommand(Logout);
            UploadTrackCommand = new RelayCommand(UploadTrack);
            SelectThemeCommand = new RelayCommand<string>(SelectTheme);

            PlaylistListenService.OnPlaylistListenChanged += OnPlaylistStatsChanged;
            SavedPlaylistService.OnPlaylistSavedChanged += OnPlaylistStatsChanged;

            LoadOverview();
        }

        // ─── Активная вкладка ────────────────────────

        private string _activeTab = "overview";
        public string ActiveTab
        {
            get => _activeTab;
            set
            {
                if (!SetProperty(ref _activeTab, value)) return;
                switch (value)
                {
                    case "overview": LoadOverview(); break;
                    case "liked":    LoadLiked(); break;
                    case "playlists":LoadPlaylists(); break;
                    case "albums":   LoadAlbums(); break;
                }
                TabChanged?.Invoke();
            }
        }

        public event Action? TabChanged;
        public event Action? OverviewChanged;
        public event Action? LikedChanged;
        public event Action? PlaylistsChanged;
        public event Action? AlbumsChanged;

        // ─── Профиль ─────────────────────────────────

        public string UserName => Session.UserName;
        public string Email => Session.Email;
        public string Role => Session.UserRole;
        public string AvatarPath => Session.AvatarPath;

        // Поля редактирования
        public string EditName { get; set; } = "";
        public string EditEmail { get; set; } = "";
        public string EditPassword { get; set; } = "";
        public string EditAvatarPath { get; set; } = "";

        public ICommand SelectTabCommand { get; }
        public ICommand SaveProfileCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand UploadTrackCommand { get; }
        public ICommand SelectThemeCommand { get; }

        // ─── Загрузка трека (артист) ────────────────

        public string NewTrackName { get; set; } = "";
        public string? NewTrackCoverPath { get; set; }
        public string? NewTrackAudioPath { get; set; }
        public string NewTrackGenre { get; set; } = "";

        public event Action? TrackUploadFinished;

        // ─── Коллекции вкладок ──────────────────────

        public ObservableCollection<Playlist> OverviewPlaylists { get; }
        public ObservableCollection<Track> OverviewLikedTracks { get; }
        public ObservableCollection<Playlist> ProfilePlaylistsOwn { get; }
        public ObservableCollection<Playlist> ProfilePlaylistsSaved { get; }
        public ObservableCollection<Track> LikedTracks { get; }
        public ObservableCollection<Album> SavedAlbums { get; }

        // ─── Активная тема ───────────────────────────

        public event Action<string>? ThemeSelected;

        // ─── Реализация ─────────────────────────────

        private void LoadOverview()
        {
            OverviewPlaylists.Clear();
            OverviewLikedTracks.Clear();

            var own = Session.CachedPlaylists.Where(p => p.OwnerID == Session.UserId).Take(6).ToList();
            List<Playlist> saved = new();
            try { saved = SavedPlaylistService.GetSavedByUser(Session.UserId).Take(6).ToList(); } catch { }
            foreach (var p in own.Concat(saved.Where(s => !own.Any(o => o.PlaylistID == s.PlaylistID))).Take(6))
                OverviewPlaylists.Add(p);

            var liked = Session.LikedTrackIds.Take(6)
                .Select(id => TrackService.GetTrackById(id))
                .Where(t => t != null).Cast<Track>();
            foreach (var t in liked) OverviewLikedTracks.Add(t);
            OverviewChanged?.Invoke();
        }

        private void LoadLiked()
        {
            LikedTracks.Clear();
            var ids = Session.LikedTrackIds;
            foreach (var t in ids.Select(id => TrackService.GetTrackById(id)).Where(t => t != null).Cast<Track>().Take(25))
                LikedTracks.Add(t);
            LikedChanged?.Invoke();
        }

        private void LoadPlaylists()
        {
            ProfilePlaylistsOwn.Clear();
            ProfilePlaylistsSaved.Clear();
            var own = Session.CachedPlaylists.Where(p => p.OwnerID == Session.UserId).ToList();
            List<Playlist> saved = new();
            try { saved = SavedPlaylistService.GetSavedByUser(Session.UserId); } catch { }
            foreach (var p in own) ProfilePlaylistsOwn.Add(p);
            foreach (var p in saved.Where(s => !own.Any(o => o.PlaylistID == s.PlaylistID)))
                ProfilePlaylistsSaved.Add(p);
            PlaylistsChanged?.Invoke();
        }

        private void LoadAlbums()
        {
            SavedAlbums.Clear();
            try
            {
                foreach (var a in AlbumService.GetSavedByUser(Session.UserId)) SavedAlbums.Add(a);
            }
            catch { }
            AlbumsChanged?.Invoke();
        }

        public void RefreshActiveTab()
        {
            switch (ActiveTab)
            {
                case "overview": LoadOverview(); break;
                case "liked":    LoadLiked(); break;
                case "playlists":LoadPlaylists(); break;
                case "albums":   LoadAlbums(); break;
            }
        }

        private void OnPlaylistStatsChanged(int _)
        {
            if (ActiveTab == "overview" || ActiveTab == "playlists") RefreshActiveTab();
        }

        private void SaveProfile()
        {
            if (string.IsNullOrWhiteSpace(EditName) || string.IsNullOrWhiteSpace(EditEmail))
            { _dialog.Error("Ошибка ввода, проверь данные"); return; }

            Session.UserName = EditName;
            Session.Email = EditEmail;
            Session.Password = EditPassword;
            Session.HidedPassword = new string('●', Session.Password.Length);
            if (!string.IsNullOrEmpty(EditAvatarPath)) Session.AvatarPath = EditAvatarPath;

            OnPropertyChanged(nameof(UserName));
            OnPropertyChanged(nameof(Email));
            OnPropertyChanged(nameof(AvatarPath));

            _dialog.Info("Данные аккаунта успешно изменены");
        }

        private void Logout()
        {
            // Закрытие окна и возврат на регистрацию делает code-behind,
            // потому что VM не должен напрямую открывать Window-ы.
            LogoutRequested?.Invoke();
        }

        public event Action? LogoutRequested;

        private void UploadTrack()
        {
            if (string.IsNullOrWhiteSpace(NewTrackName) || string.IsNullOrEmpty(NewTrackAudioPath))
            { _dialog.Error("Введите название и выберите аудиофайл!"); return; }

            try
            {
                string uniqueFileName = FileStorage.CopyTrackAudio(NewTrackAudioPath, NewTrackName);
                string destPath = Path.Combine(Rewind.Helpers.FileStorage.DataRoot, "MusicLibrary", uniqueFileName);
                int duration;
                using (var file = TagLib.File.Create(destPath))
                    duration = (int)file.Properties.Duration.TotalSeconds;

                string? finalCoverPath = null;
                if (!string.IsNullOrWhiteSpace(NewTrackCoverPath))
                    finalCoverPath = FileStorage.CopyTrackCover(NewTrackCoverPath);

                var newTrack = new Track
                {
                    Title = NewTrackName,
                    FilePath = uniqueFileName,
                    CoverPath = finalCoverPath,
                    Duration = duration,
                    UploadDate = DateTime.UtcNow,
                    ArtistID = Session.UserId,
                    Genre = NewTrackGenre,
                    PublishStatus = "Pending"
                };

                TrackService.AddTrack(newTrack);
                _dialog.Info("Трек отправлен на проверку администратору. После одобрения он появится на платформе.",
                    "Заявка отправлена");

                NewTrackName = "";
                NewTrackAudioPath = null;
                NewTrackCoverPath = null;
                TrackUploadFinished?.Invoke();
            }
            catch (Exception ex)
            {
                _dialog.Error($"Ошибка: {ex.Message}");
            }
        }

        private void SelectTheme(string? name)
        {
            if (string.IsNullOrEmpty(name)) return;
            Session.ActiveTheme = name;
            ThemeSelected?.Invoke(name);
        }

        public void Dispose()
        {
            PlaylistListenService.OnPlaylistListenChanged -= OnPlaylistStatsChanged;
            SavedPlaylistService.OnPlaylistSavedChanged -= OnPlaylistStatsChanged;
        }
    }
}
