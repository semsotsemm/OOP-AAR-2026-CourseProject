using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Rewind.Helpers
{
    /// <summary>
    /// Статический класс сессии.
    /// Хранит все данные текущего пользователя в памяти.
    /// Сброс в БД происходит один раз — при вызове FlushToDatabase(),
    /// который нужно вызвать из App.xaml.cs в Application.Exit или OnExit.
    /// </summary>
    public static class Session
    {
        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
            => StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));

        // ─────────────────────────────────────────────
        //  Данные пользователя
        // ─────────────────────────────────────────────
        public static int UserId { get; set; }
        public static string UserRole { get; set; } = "Слушатель";
        public static string ActiveTheme { get; set; } = "ThemeClassic";

        private static string _userName = "Алексей Антипов";
        public static string UserName
        {
            get => _userName;
            set { _userName = value; OnStaticPropertyChanged(); }
        }

        private static string _email = "antipovalexey@mail.ru";
        public static string Email
        {
            get => _email;
            set { _email = value; OnStaticPropertyChanged(); }
        }

        private static string _password = "12345678";
        public static string Password
        {
            get => _password;
            set { _password = value; OnStaticPropertyChanged(); }
        }

        private static string _hidedPassword = "********";
        public static string HidedPassword
        {
            get => _hidedPassword;
            set { _hidedPassword = value; OnStaticPropertyChanged(); }
        }

        private static string _avatarPath = FileStorage.DefaultAvatar;
        public static string AvatarPath
        {
            get => _avatarPath;
            set { _avatarPath = value; OnStaticPropertyChanged(); }
        }

        // ─────────────────────────────────────────────
        //  Счётчики (отображение в UI)
        // ─────────────────────────────────────────────
        private static int _tracksListened;
        public static int TracksListened
        {
            get => _tracksListened;
            set { _tracksListened = value; OnStaticPropertyChanged(); }
        }

        private static int _listened;
        public static int Listened
        {
            get => _listened;
            set { _listened = value; OnStaticPropertyChanged(); }
        }

        private static int _playlists;
        public static int Playlists
        {
            get => _playlists;
            set { _playlists = value; OnStaticPropertyChanged(); }
        }

        private static int _liked;
        public static int Liked
        {
            get => _liked;
            set { _liked = value; OnStaticPropertyChanged(); }
        }

        private static int _subscriptions;
        public static int Subscriptions
        {
            get => _subscriptions;
            set { _subscriptions = value; OnStaticPropertyChanged(); }
        }

        public static bool _isPlaing;

        // Текущий контекст прослушивания для подсчёта плейлистов/альбомов.
        // +1 даётся только один раз, пока пользователь не выйдет из этого контекста.
        private static string? _playbackScopeType;
        private static int? _playbackScopeId;

        public static bool TryEnterPlaybackScope(string scopeType, int scopeId)
        {
            if (scopeId <= 0) return false;
            if (_playbackScopeType == scopeType && _playbackScopeId == scopeId)
                return false;
            _playbackScopeType = scopeType;
            _playbackScopeId = scopeId;
            return true;
        }

        public static void ResetPlaybackScope()
        {
            _playbackScopeType = null;
            _playbackScopeId = null;
        }

        // Настройки уведомлений (хранятся между переходампо страницам)
        public static bool NotifNewTracksEnabled { get; set; } = true;
        public static bool NotifPushEnabled { get; set; } = true;
        public static int NotificationCount { get; set; } = 0;
        public static DateTime? LastNotificationCheck { get; set; } = null;
        // ─────────────────────────────────────────────
        //  КЕШ ЛАЙКОВ
        //  Треки, которые пользователь лайкнул/снял в текущей сессии.
        //  _likedTrackIds  — id треков, у которых лайк СТОИТ (итоговое состояние)
        //  _addedInSession / _removedInSession — дельта для сброса в БД
        // ─────────────────────────────────────────────
        private static readonly HashSet<int> _likedTrackIds = new();
        private static readonly HashSet<int> _addedInSession = new();
        private static readonly HashSet<int> _removedInSession = new();

        /// <summary>Загружается один раз при входе.</summary>
        public static void InitFavorites(IEnumerable<int> trackIdsFromDb)
        {
            _likedTrackIds.Clear();
            _addedInSession.Clear();
            _removedInSession.Clear();
            foreach (var id in trackIdsFromDb)
                _likedTrackIds.Add(id);
            Liked = _likedTrackIds.Count;
        }

        public static bool IsLiked(int trackId) => _likedTrackIds.Contains(trackId);

        /// <summary>Fires when any track is liked or unliked — trackId + isNowLiked.</summary>
        public static event Action<int, bool>? LikeChanged;

        /// <summary>Переключает лайк и возвращает новое состояние.
        /// Лайк записывается в БД немедленно в фоновом потоке (fire-and-forget).</summary>
        public static bool ToggleLike(int trackId)
        {
            if (_likedTrackIds.Contains(trackId))
            {
                _likedTrackIds.Remove(trackId);
                _removedInSession.Add(trackId);
                _addedInSession.Remove(trackId);
                Liked = _likedTrackIds.Count;
                if (UserId > 0) System.Threading.Tasks.Task.Run(() => { try { FavoriteService.RemoveFavorite(UserId, trackId); } catch { } });
                LikeChanged?.Invoke(trackId, false);
                return false;
            }
            else
            {
                _likedTrackIds.Add(trackId);
                _addedInSession.Add(trackId);
                _removedInSession.Remove(trackId);
                Liked = _likedTrackIds.Count;
                if (UserId > 0) System.Threading.Tasks.Task.Run(() => { try { FavoriteService.AddFavorite(UserId, trackId); } catch { } });
                LikeChanged?.Invoke(trackId, true);
                return true;
            }
        }

        /// <summary>Все залайканные треки (для FavoritesPage).</summary>
        public static IReadOnlyCollection<int> LikedTrackIds => _likedTrackIds;

        // ─────────────────────────────────────────────
        //  КЕШ ПЛЕЙЛИСТОВ
        //  Полные объекты плейлистов текущего пользователя.
        // ─────────────────────────────────────────────
        private static readonly List<Playlist> _cachedPlaylists = new();
        private static readonly List<Playlist> _playlistsToAdd = new();
        private static readonly List<int> _playlistsToDel = new();
        private static readonly List<(Playlist Playlist, int TrackId)> _playlistTracksToAdd = new();

        public static IReadOnlyList<Playlist> CachedPlaylists => _cachedPlaylists;

        public static void InitPlaylists(IEnumerable<Playlist> playlistsFromDb)
        {
            _cachedPlaylists.Clear();
            _playlistsToAdd.Clear();
            _playlistsToDel.Clear();
            _playlistTracksToAdd.Clear();
            _cachedPlaylists.AddRange(playlistsFromDb);
            Playlists = _cachedPlaylists.Count;
        }

        /// <summary>Создаёт плейлист в кеше (в БД уйдёт при Flush).</summary>
        public static Playlist CreatePlaylist(string title, string? coverPath, bool isPrivate)
        {
            var pl = new Playlist
            {
                // Id = 0 означает «ещё не в БД»
                Title = title,
                OwnerID = UserId,
                IsPrivate = isPrivate,
                CoverPath = coverPath
            };
            _cachedPlaylists.Insert(0, pl);
            _playlistsToAdd.Add(pl);
            Playlists = _cachedPlaylists.Count;
            return pl;
        }

        public static void DeletePlaylist(Playlist pl)
        {
            _cachedPlaylists.Remove(pl);
            if (_playlistsToAdd.Contains(pl))
                _playlistsToAdd.Remove(pl);   // не дошло до БД — просто забываем
            else if (pl.PlaylistID > 0)
                _playlistsToDel.Add(pl.PlaylistID);

            _playlistTracksToAdd.RemoveAll(x => x.Playlist == pl || x.Playlist.PlaylistID == pl.PlaylistID);
            Playlists = _cachedPlaylists.Count;
        }

        /// <summary>Добавляет трек в плейлист в кеше (в БД уйдёт при Flush).</summary>
        public static bool AddTrackToPlaylist(Playlist playlist, int trackId)
        {
            if (playlist == null || trackId <= 0) return false;

            playlist.PlaylistTracks ??= new List<PlaylistTrack>();
            if (playlist.PlaylistTracks.Any(pt => pt.TrackID == trackId)) return false;

            // Подгружаем сам Track, чтобы PlaylistDetailsPage сразу его видел
            // (иначе он появится только после рестарта, когда EF подтянет навигацию).
            var track = TrackService.GetTrackById(trackId);

            playlist.PlaylistTracks.Add(new PlaylistTrack
            {
                PlaylistID = playlist.PlaylistID,
                TrackID = trackId,
                Track = track!
            });

            _playlistTracksToAdd.Add((playlist, trackId));
            PlaylistChanged?.Invoke(playlist.PlaylistID);
            return true;
        }

        /// <summary>Срабатывает при изменении состава плейлиста (добавлении/удалении треков).</summary>
        public static event Action<int>? PlaylistChanged;
        public static void AddListenedTrack(int trackId, double durationSeconds)
        {
            TracksListened++;
            Listened += (int)Math.Round(durationSeconds / 60.0);
            TrackService.IncrementPlayCount(trackId);
            // Записываем в историю для графиков аналитики
            HistoryService.RecordListen(UserId, trackId);
        }

        // ─────────────────────────────────────────────
        //  История прослушивания
        // ─────────────────────────────────────────────
        public static void AddListenedTrack(double durationSeconds)
        {
            TracksListened++;
            Listened += (int)Math.Round(durationSeconds / 60.0);
        }

        // ─────────────────────────────────────────────
        //  СБРОС В БД — вызвать один раз при закрытии
        // ─────────────────────────────────────────────
        public static void FlushToDatabase()
        {
            if (UserId == 0) return;

            try
            {
                // 1. Лайки: добавляем новые
                foreach (var trackId in _addedInSession)
                    FavoriteService.AddFavorite(UserId, trackId);

                // 2. Лайки: удаляем снятые
                foreach (var trackId in _removedInSession)
                    FavoriteService.RemoveFavorite(UserId, trackId);

                // 3. Плейлисты: сохраняем новые
                foreach (var pl in _playlistsToAdd)
                {
                    PlaylistService.AddPlaylist(pl);
                }

                // 4. Плейлисты: удаляем удалённые
                foreach (var id in _playlistsToDel)
                    PlaylistService.DeletePlaylist(id);

                // 5. Связи треков с плейлистами
                foreach (var item in _playlistTracksToAdd.ToList())
                {
                    var playlistId = item.Playlist.PlaylistID;
                    if (playlistId <= 0) continue;
                    if (_playlistsToDel.Contains(playlistId)) continue;
                    PlaylistService.AddTrackToPlaylist(playlistId, item.TrackId);
                }

                // 6. Обновляем данные пользователя
                var userInDb = UserService.GetUserById(UserId);
                if (userInDb != null)
                {
                    userInDb.Nickname = UserName;
                    userInDb.Email = Email;
                    userInDb.ProfilePhotoPath = string.IsNullOrWhiteSpace(AvatarPath) ? FileStorage.DefaultAvatar : AvatarPath;
                    if (!string.IsNullOrWhiteSpace(Password))
                        userInDb.PasswordHash = PasswordHelper.HashPassword(Password);
                    UserService.UpdateUser(userInDb, userInDb);
                }

                _addedInSession.Clear();
                _removedInSession.Clear();
                _playlistsToAdd.Clear();
                _playlistsToDel.Clear();
                _playlistTracksToAdd.Clear();
            }
            catch (Exception ex)
            {
                // Тихо — приложение уже закрывается
                System.Diagnostics.Debug.WriteLine($"[Session.Flush] {ex.Message}");
            }
        }

        /// <summary>
        /// Вызвать после успешного входа — загружает все данные из БД в кеш.
        /// </summary>
        public static void LoadFromDatabase()
        {
            if (UserId == 0) return;
            try
            {
                var favTrackIds = FavoriteService.GetFavoritesByUser(UserId)
                                                 .Select(t => t.TrackID)
                                                 .ToList();
                InitFavorites(favTrackIds);

                if (string.IsNullOrWhiteSpace(AvatarPath))
                    AvatarPath = FileStorage.DefaultAvatar;

                var playlists = PlaylistService.GetPlaylistsByUser(UserId);
                InitPlaylists(playlists);

                var stats = UserStatisticsDto.GetUserStats(UserId);
                Subscriptions = stats.SubscriptionsCount;
                TracksListened = stats.TotalTracksListened;
                Listened = stats.TotalTimeFormatted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Session.Load] {ex.Message}");
                InitFavorites(Array.Empty<int>());
                InitPlaylists(Array.Empty<Playlist>());
            }
        }
    }
}
