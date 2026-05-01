using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rewind.Helpers
{
    // ─────────────────────────────────────────────────────────────
    //  МОДЕЛИ
    // ─────────────────────────────────────────────────────────────

    public class Role
    {
        [Key] public int RoleId { get; set; }
        [Required, MaxLength(20)] public string RoleName { get; set; }
        public List<User> Users { get; set; } = new();
    }

    public class User
    {
        [Key] public int UserId { get; set; }
        [Required, MaxLength(50)] public string Nickname { get; set; }
        [Required, MaxLength(100)] public string Email { get; set; }
        [Required] public string PasswordHash { get; set; }
        public string? ProfilePhotoPath { get; set; }
        public string? Status { get; set; }
        public int RoleId { get; set; }
        public Role Role { get; set; }
        public List<Track> Tracks { get; set; } = new();
        public List<Playlist> Playlists { get; set; } = new();
        public List<Favorite> Favorites { get; set; } = new();
        public List<ListeningHistory> History { get; set; } = new();
    }

    public class Track
    {
        [Key] public int TrackID { get; set; }
        [Required, MaxLength(255)] public string Title { get; set; }
        [Required] public string FilePath { get; set; }
        public string? CoverPath { get; set; }
        public int Duration { get; set; }
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        public int ArtistID { get; set; }
        public string? Genre { get; set; }
        /// <summary>Published / Pending / Rejected / Banned</summary>
        public string PublishStatus { get; set; } = "Published";
        public string? RejectionReason { get; set; }
        public User Artist { get; set; }
        public Statistic Statistics { get; set; }
        public List<PlaylistTrack> PlaylistTracks { get; set; } = new();
        public List<Favorite> Favorites { get; set; } = new();
        public List<ListeningHistory> History { get; set; } = new();
    }

    public class ListeningHistory
    {
        [Key] public int HistoryId { get; set; }
        public int UserID { get; set; }
        public User User { get; set; }
        public int TrackID { get; set; }
        public Track Track { get; set; }
        public DateTime ListenedAt { get; set; } = DateTime.UtcNow;
    }

    public class Statistic
    {
        [Key, ForeignKey("Track")]
        public int TrackID { get; set; }
        public int PlayCount { get; set; } = 0;
        public int LikesCount { get; set; } = 0;
        public Track Track { get; set; }
    }

    public class Playlist
    {
        [Key] public int PlaylistID { get; set; }
        [Required, MaxLength(100)] public string Title { get; set; }
        public int OwnerID { get; set; }
        public User Owner { get; set; }
        /// <summary>true = виден только владельцу, false = публичный</summary>
        public bool IsPrivate { get; set; } = false;
        /// <summary>Путь к картинке обложки (локальный или относительный)</summary>
        public string? CoverPath { get; set; }
        public List<PlaylistTrack> PlaylistTracks { get; set; } = new();
    }

    public class PlaylistTrack
    {
        public int PlaylistID { get; set; }
        public Playlist Playlist { get; set; }
        public int TrackID { get; set; }
        public Track Track { get; set; }
    }

    public class Subscription
    {
        public int FollowerID { get; set; }
        public User Follower { get; set; }
        public int ArtistID { get; set; }
        public User Artist { get; set; }
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    }

    public class Favorite
    {
        public int UserID { get; set; }
        public User User { get; set; }
        public int TrackID { get; set; }
        public Track Track { get; set; }
    }

    public class Album
    {
        [Key] public int AlbumId { get; set; }
        [Required, MaxLength(100)] public string Title { get; set; } = "";
        public int ArtistId { get; set; }
        public User Artist { get; set; } = null!;
        public string? CoverPath { get; set; }
        public string? Genre { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<AlbumTrack> AlbumTracks { get; set; } = new();
    }

    public class AlbumTrack
    {
        public int AlbumId { get; set; }
        public Album Album { get; set; } = null!;
        public int TrackId { get; set; }
        public Track Track { get; set; } = null!;
    }

    public class SavedAlbum
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int AlbumId { get; set; }
        public Album Album { get; set; } = null!;
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }

    public class AlbumListenEvent
    {
        [Key] public int AlbumListenEventId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int AlbumId { get; set; }
        public Album Album { get; set; } = null!;
        public DateTime ListenedAt { get; set; } = DateTime.UtcNow;
    }

    public class ArtistRequest
    {
        [Key] public int RequestId { get; set; }
        [Required, MaxLength(50)] public string Nickname { get; set; } = "";
        [Required, MaxLength(100)] public string Email { get; set; } = "";
        [Required] public string PasswordHash { get; set; } = "";
        /// <summary>Pending / Approved / Rejected</summary>
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class TrackReport
    {
        [Key] public int ReportId { get; set; }
        public int TrackId { get; set; }
        public Track Track { get; set; } = null!;
        public int ReporterId { get; set; }
        public User Reporter { get; set; } = null!;
        [Required] public string Reason { get; set; } = "";
        /// <summary>Pending / Banned / Dismissed</summary>
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Уникальный слушатель плейлиста (один пользователь — один запись).</summary>
    public class PlaylistListen
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int PlaylistId { get; set; }
        public Playlist Playlist { get; set; } = null!;
        public DateTime ListenedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Каждое нажатие Play внутри плейлиста — отдельное событие прослушивания.</summary>
    public class PlaylistPlayEvent
    {
        [Key] public int PlaylistPlayEventId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int PlaylistId { get; set; }
        public Playlist Playlist { get; set; } = null!;
        public DateTime ListenedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Пользователь сохранил/лайкнул плейлист без копирования.</summary>
    public class SavedPlaylist
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int PlaylistId { get; set; }
        public Playlist Playlist { get; set; } = null!;
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }

    // ─────────────────────────────────────────────────────────────
    //  DTO СТАТИСТИКИ
    // ─────────────────────────────────────────────────────────────

    public class UserStatisticsDto
    {
        public int SubscriptionsCount { get; set; }
        public int TotalTracksListened { get; set; }
        public int TotalTimeFormatted { get; set; }
        public int PlaylistsCount { get; set; }
        public int FavoritesCount { get; set; }

        public static UserStatisticsDto GetUserStats(int userId)
        {
            using var db = new AppDbContext();

            var subsCount = db.Subscriptions.Count(s => s.FollowerID == userId);
            var playlistsCount = db.Playlists.Count(p => p.OwnerID == userId);
            var favoritesCount = db.Favorites.Count(f => f.UserID == userId);

            var durations = db.History
                .Where(h => h.UserID == userId)
                .Select(h => h.Track.Duration)
                .ToList();

            int totalTracks = durations.Count;
            int totalSeconds = durations.Sum();
            int totalMinutes = (int)TimeSpan.FromSeconds(totalSeconds).TotalMinutes;

            return new UserStatisticsDto
            {
                SubscriptionsCount = subsCount,
                PlaylistsCount = playlistsCount,
                FavoritesCount = favoritesCount,
                TotalTracksListened = totalTracks,
                TotalTimeFormatted = totalMinutes
            };
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  КОНТЕКСТ
    // ─────────────────────────────────────────────────────────────

    public class AppDbContext : DbContext
    {
        public AppDbContext()
        {
            Database.EnsureCreated();
            EnsureArtistRequestsTable();
            EnsureTrackSchemaUpdates();
            EnsureAlbumsTables();
            EnsureSubscriptionTimestamp();
            SeedDefaultAdmin();
        }

        private void EnsureTrackSchemaUpdates()
        {
            try { Database.ExecuteSqlRaw(@"ALTER TABLE ""Tracks"" ADD COLUMN IF NOT EXISTS ""Genre"" VARCHAR(100)"); } catch { }
            try { Database.ExecuteSqlRaw(@"ALTER TABLE ""Tracks"" ADD COLUMN IF NOT EXISTS ""PublishStatus"" VARCHAR(20) NOT NULL DEFAULT 'Published'"); } catch { }
            try { Database.ExecuteSqlRaw(@"ALTER TABLE ""Tracks"" ADD COLUMN IF NOT EXISTS ""RejectionReason"" TEXT"); } catch { }
        }

        private void EnsureSubscriptionTimestamp()
        {
            try
            {
                Database.ExecuteSqlRaw(@"
                    ALTER TABLE ""Subscriptions""
                    ADD COLUMN IF NOT EXISTS ""SubscribedAt""
                    TIMESTAMPTZ NOT NULL DEFAULT NOW()");
            }
            catch { /* column already exists or table not created yet */ }
        }

        private void EnsureAlbumsTables()
        {
            try
            {
                Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""Albums"" (
                        ""AlbumId""    SERIAL       PRIMARY KEY,
                        ""Title""      VARCHAR(100) NOT NULL,
                        ""ArtistId""   INT          NOT NULL REFERENCES ""Users""(""UserId"") ON DELETE CASCADE,
                        ""CoverPath""  TEXT,
                        ""Genre""      VARCHAR(100),
                        ""CreatedAt""  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
                    )");
                Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""AlbumTracks"" (
                        ""AlbumId""  INT NOT NULL REFERENCES ""Albums""(""AlbumId"") ON DELETE CASCADE,
                        ""TrackId""  INT NOT NULL REFERENCES ""Tracks""(""TrackID"") ON DELETE CASCADE,
                        PRIMARY KEY (""AlbumId"", ""TrackId"")
                    )");
                Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""SavedAlbums"" (
                        ""UserId"" INT NOT NULL REFERENCES ""Users""(""UserId"") ON DELETE CASCADE,
                        ""AlbumId"" INT NOT NULL REFERENCES ""Albums""(""AlbumId"") ON DELETE CASCADE,
                        ""SavedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        PRIMARY KEY (""UserId"", ""AlbumId"")
                    )");
                Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""AlbumListenEvents"" (
                        ""AlbumListenEventId"" SERIAL PRIMARY KEY,
                        ""UserId"" INT NOT NULL REFERENCES ""Users""(""UserId"") ON DELETE CASCADE,
                        ""AlbumId"" INT NOT NULL REFERENCES ""Albums""(""AlbumId"") ON DELETE CASCADE,
                        ""ListenedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW()
                    )");
                Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""TrackReports"" (
                        ""ReportId""    SERIAL       PRIMARY KEY,
                        ""TrackId""     INT          NOT NULL REFERENCES ""Tracks""(""TrackID"") ON DELETE CASCADE,
                        ""ReporterId""  INT          NOT NULL REFERENCES ""Users""(""UserId"") ON DELETE CASCADE,
                        ""Reason""      TEXT         NOT NULL,
                        ""Status""      VARCHAR(20)  NOT NULL DEFAULT 'Pending',
                        ""CreatedAt""   TIMESTAMPTZ  NOT NULL DEFAULT NOW()
                    )");
                Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""SavedPlaylists"" (
                        ""UserId""      INT         NOT NULL REFERENCES ""Users""(""UserId"") ON DELETE CASCADE,
                        ""PlaylistId""  INT         NOT NULL REFERENCES ""Playlists""(""PlaylistID"") ON DELETE CASCADE,
                        ""SavedAt""     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        PRIMARY KEY (""UserId"", ""PlaylistId"")
                    )");
                Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""PlaylistListens"" (
                        ""UserId""     INT         NOT NULL REFERENCES ""Users""(""UserId"") ON DELETE CASCADE,
                        ""PlaylistId"" INT         NOT NULL REFERENCES ""Playlists""(""PlaylistID"") ON DELETE CASCADE,
                        ""ListenedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        PRIMARY KEY (""UserId"", ""PlaylistId"")
                    )");
                Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""PlaylistPlayEvents"" (
                        ""PlaylistPlayEventId"" SERIAL PRIMARY KEY,
                        ""UserId""     INT         NOT NULL REFERENCES ""Users""(""UserId"") ON DELETE CASCADE,
                        ""PlaylistId"" INT         NOT NULL REFERENCES ""Playlists""(""PlaylistID"") ON DELETE CASCADE,
                        ""ListenedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW()
                    )");
            }
            catch { }
        }

        private void EnsureArtistRequestsTable()
        {
            try
            {
                Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""ArtistRequests"" (
                        ""RequestId""     SERIAL          PRIMARY KEY,
                        ""Nickname""      VARCHAR(50)     NOT NULL,
                        ""Email""         VARCHAR(100)    NOT NULL,
                        ""PasswordHash""  TEXT            NOT NULL,
                        ""Status""        VARCHAR(20)     NOT NULL DEFAULT 'Pending',
                        ""CreatedAt""     TIMESTAMPTZ     NOT NULL DEFAULT NOW()
                    )");
            }
            catch { /* уже существует */ }
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ArtistRequest> ArtistRequests { get; set; }
        public DbSet<Album> Albums { get; set; }
        public DbSet<AlbumTrack> AlbumTracks { get; set; }
        public DbSet<SavedAlbum> SavedAlbums { get; set; }
        public DbSet<AlbumListenEvent> AlbumListenEvents { get; set; }
        public DbSet<TrackReport> TrackReports { get; set; }
        public DbSet<SavedPlaylist> SavedPlaylists { get; set; }
        public DbSet<PlaylistListen> PlaylistListens { get; set; }
        public DbSet<PlaylistPlayEvent> PlaylistPlayEvents { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<Statistic> Statistics { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistTrack> PlaylistTracks { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<ListeningHistory> History { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder o)
            => o.UseNpgsql("Host=localhost;Port=5432;Database=rewinddb;Username=postgres;Password=5329965");

        protected override void OnModelCreating(ModelBuilder m)
        {
            m.Entity<User>().HasIndex(u => u.Nickname).IsUnique();
            m.Entity<User>().HasIndex(u => u.Email).IsUnique();

            m.Entity<PlaylistTrack>().HasKey(pt => new { pt.PlaylistID, pt.TrackID });
            m.Entity<Favorite>().HasKey(f => new { f.UserID, f.TrackID });
            m.Entity<Subscription>().HasKey(s => new { s.FollowerID, s.ArtistID });
            m.Entity<AlbumTrack>().HasKey(at => new { at.AlbumId, at.TrackId });
            m.Entity<SavedAlbum>().HasKey(sa => new { sa.UserId, sa.AlbumId });
            m.Entity<SavedPlaylist>().HasKey(sp => new { sp.UserId, sp.PlaylistId });
            m.Entity<PlaylistListen>().HasKey(pl => new { pl.UserId, pl.PlaylistId });

            m.Entity<ListeningHistory>().HasIndex(h => h.UserID);
            m.Entity<ListeningHistory>().HasIndex(h => h.TrackID);

            // Публичные плейлисты: один пользователь может видеть чужие
            // (фильтр применяется на уровне запросов, не FK)

            m.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Artist" },
                new Role { RoleId = 3, RoleName = "Listener" }
            );
        }

        private void SeedDefaultAdmin()
        {
            try
            {
                if (Users.Any(u => u.Nickname == "Alexey")) return;

                Users.Add(new User
                {
                    Nickname = "Alexey",
                    Email = "alexey@rewind.local",
                    PasswordHash = PasswordHelper.HashPassword("20062018no"),
                    ProfilePhotoPath = null,
                    RoleId = 1,
                    Status = "Активен"
                });
                SaveChanges();
            }
            catch
            {
                // Игнорируем на старте, чтобы не ронять приложение.
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  СЕРВИСЫ
    // ─────────────────────────────────────────────────────────────

    public static class GenreService
    {
        public static IReadOnlyList<string> DefaultGenres { get; } = new[]
        {
            "Pop", "Rock", "Hip-Hop", "Electronic", "R&B", "Jazz", "Classical", "Metal",
            "Folk", "Indie", "Alternative", "Dance", "Reggae", "Latin", "Other"
        };

        public static List<string> GetAllGenresFromTracks()
        {
            using var db = new AppDbContext();
            return db.Tracks
                .Select(t => t.Genre)
                .Where(g => g != null && g != "")
                .ToList()!
                .Select(g => g!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g)
                .ToList();
        }
    }

    public static class UserService
    {
        public static List<User> GetAllUsers()
        {
            using var db = new AppDbContext();
            return db.Users.Include(u => u.Role).ToList();
        }

        public static User? GetUserById(int id)
        {
            using var db = new AppDbContext();
            return db.Users.Include(u => u.Role).Include(u => u.Tracks)
                           .FirstOrDefault(u => u.UserId == id);
        }

        public static User? GetUserByNickname(string nickname)
        {
            using var db = new AppDbContext();
            return db.Users.Include(u => u.Role).FirstOrDefault(u => u.Nickname == nickname);
        }

        public static User? GetUserByEmail(string email)
        {
            using var db = new AppDbContext();
            return db.Users.FirstOrDefault(u => u.Email == email);
        }

        public static int AddUser(User newUser)
        {
            using var db = new AppDbContext();
            db.Users.Add(newUser);
            db.SaveChanges();
            return newUser.UserId;
        }

        public static bool DeleteUser(int id)
        {
            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null) return false;
            db.Users.Remove(user);
            db.SaveChanges();
            return true;
        }

        public static void UpdateUser(User currentUser, User newUser)
        {
            using var db = new AppDbContext();
            var userInDb = db.Users.Find(currentUser.UserId)
                ?? throw new Exception("Пользователь не найден в базе данных.");

            userInDb.Nickname = newUser.Nickname;
            userInDb.Email = newUser.Email;
            userInDb.RoleId = newUser.RoleId;
            userInDb.ProfilePhotoPath = newUser.ProfilePhotoPath;

            if (!string.IsNullOrWhiteSpace(newUser.PasswordHash))
                userInDb.PasswordHash = newUser.PasswordHash;

            db.SaveChanges();
        }
    }

    public static class TrackService
    {
        public static event Action<int, int>? OnPlayCountUpdated;
        /// <summary>artistId, artistName, trackTitle — файер при загрузке нового трека</summary>
        public static event Action<int, string, string>? NewTrackUploaded;
        /// <summary>Файерит NewTrackUploaded. Вызывать из любого места вместо прямого Invoke.</summary>
        public static void NotifyNewTrackUploaded(int artistId, string artistName, string trackTitle)
            => NewTrackUploaded?.Invoke(artistId, artistName, trackTitle);
        public static List<Track> GetAllTracks()
        {
            using var db = new AppDbContext();
            return db.Tracks.Include(t => t.Artist).Include(t => t.Statistics).ToList();
        }

        public static Track? GetTrackById(int id)
        {
            using var db = new AppDbContext();
            return db.Tracks.Include(t => t.Artist).Include(t => t.Statistics)
                            .FirstOrDefault(t => t.TrackID == id);
        }

        public static List<Track> GetTracksByArtist(int artistId)
        {
            using var db = new AppDbContext();
            return db.Tracks.Include(t => t.Statistics)
                            .Where(t => t.ArtistID == artistId).ToList();
        }

        public static Track? GetMostPopularTrack()
        {
            using var db = new AppDbContext();

            return db.Tracks.Include(t => t.Artist).Include(t => t.Statistics).OrderByDescending(t => (t.Statistics.PlayCount + (t.Statistics.LikesCount * 10))).FirstOrDefault();
        }
        public static void IncrementPlayCount(int trackId)
        {
            using var db = new AppDbContext();
            var stats = db.Statistics.FirstOrDefault(s => s.TrackID == trackId);

            if (stats != null)
            {
                stats.PlayCount++;
                db.SaveChanges();

                OnPlayCountUpdated?.Invoke(trackId, stats.PlayCount);
            }
        }

        public static void AddTrack(Track track)
        {
            using var db = new AppDbContext();
            db.Tracks.Add(track);
            db.SaveChanges();
            db.Statistics.Add(new Statistic { TrackID = track.TrackID });
            db.SaveChanges();
        }

        public static List<Track> GetPublishedTracks()
        {
            using var db = new AppDbContext();
            return db.Tracks
                .Where(t => t.PublishStatus == "Published")
                .Include(t => t.Artist).Include(t => t.Statistics).ToList();
        }

        public static List<Track> GetPendingTracks()
        {
            using var db = new AppDbContext();
            return db.Tracks
                .Where(t => t.PublishStatus == "Pending")
                .Include(t => t.Artist).Include(t => t.Statistics)
                .OrderByDescending(t => t.UploadDate).ToList();
        }

        public static List<Track> GetByArtistAll(int artistId)
        {
            using var db = new AppDbContext();
            return db.Tracks
                .Where(t => t.ArtistID == artistId)
                .Include(t => t.Statistics)
                .OrderByDescending(t => t.UploadDate).ToList();
        }

        public static bool ApproveTrack(int trackId, string? editedTitle, string? editedGenre)
        {
            using var db = new AppDbContext();
            var track = db.Tracks.Find(trackId);
            if (track == null) return false;
            track.PublishStatus = "Published";
            if (!string.IsNullOrWhiteSpace(editedTitle)) track.Title = editedTitle;
            if (!string.IsNullOrWhiteSpace(editedGenre)) track.Genre = editedGenre;
            db.SaveChanges();
            return true;
        }

        public static bool RejectTrack(int trackId, string reason)
        {
            using var db = new AppDbContext();
            var track = db.Tracks.Find(trackId);
            if (track == null) return false;
            track.PublishStatus = "Rejected";
            track.RejectionReason = reason;
            db.SaveChanges();
            return true;
        }

        public static bool BanUnbanTrack(int trackId)
        {
            using var db = new AppDbContext();
            var track = db.Tracks.Find(trackId);
            if (track == null) return false;
            track.PublishStatus = track.PublishStatus == "Banned" ? "Published" : "Banned";
            db.SaveChanges();
            return true;
        }

        public static bool DeleteTrack(int id)
        {
            using var db = new AppDbContext();
            var track = db.Tracks.FirstOrDefault(t => t.TrackID == id);
            if (track == null) return false;
            db.Tracks.Remove(track);
            db.SaveChanges();
            return true;
        }

        public static void UpdateTrack(Track updatedTrack)
        {
            using var db = new AppDbContext();
            db.Tracks.Update(updatedTrack);
            db.SaveChanges();
        }
    }

    public static class StatisticService
    {
        public static Statistic? GetStatsByTrack(int trackId)
        {
            using var db = new AppDbContext();
            return db.Statistics.FirstOrDefault(s => s.TrackID == trackId);
        }

        public static void IncrementPlayCount(int trackId)
        {
            using var db = new AppDbContext();
            var stat = db.Statistics.FirstOrDefault(s => s.TrackID == trackId);
            if (stat == null) return;
            stat.PlayCount++;
            db.SaveChanges();
        }

        public static void IncrementLikes(int trackId)
        {
            using var db = new AppDbContext();
            var stat = db.Statistics.FirstOrDefault(s => s.TrackID == trackId);
            if (stat == null) return;
            stat.LikesCount++;
            db.SaveChanges();
        }

        public static void DecrementLikes(int trackId)
        {
            using var db = new AppDbContext();
            var stat = db.Statistics.FirstOrDefault(s => s.TrackID == trackId);
            if (stat == null) return;
            if (stat.LikesCount > 0) stat.LikesCount--;
            db.SaveChanges();
        }

        public static List<Track> GetTopTracks(int count = 10)
        {
            using var db = new AppDbContext();
            return db.Statistics
                .OrderByDescending(s => s.PlayCount)
                .Take(count)
                .Include(s => s.Track).ThenInclude(t => t.Artist)
                .Select(s => s.Track)
                .ToList();
        }
    }

    public static class PlaylistService
    {
        /// <summary>Плейлисты пользователя (его собственные — все, чужие — только публичные).</summary>
        public static List<Playlist> GetPlaylistsByUser(int userId)
        {
            using var db = new AppDbContext();
            return db.Playlists
                .Include(p => p.PlaylistTracks).ThenInclude(pt => pt.Track)
                .Where(p => p.OwnerID == userId)
                .ToList();
        }

        /// <summary>Публичные плейлисты всех пользователей (для раздела «Сохранённые»).</summary>
        public static List<Playlist> GetPublicPlaylists(int excludeUserId = 0)
        {
            using var db = new AppDbContext();
            return db.Playlists
                .Include(p => p.Owner)
                .Include(p => p.PlaylistTracks).ThenInclude(pt => pt.Track)
                .Where(p => !p.IsPrivate && p.OwnerID != excludeUserId)
                .ToList();
        }

        public static Playlist? GetPlaylistById(int id)
        {
            using var db = new AppDbContext();
            return db.Playlists
                .Include(p => p.Owner)
                .Include(p => p.PlaylistTracks).ThenInclude(pt => pt.Track)
                .FirstOrDefault(p => p.PlaylistID == id);
        }

        public static void AddPlaylist(Playlist playlist)
        {
            using var db = new AppDbContext();
            // Сбрасываем навигационные свойства чтобы EF не дублировал Owner
            playlist.Owner = null!;
            db.Playlists.Add(playlist);
            db.SaveChanges();
        }

        public static bool DeletePlaylist(int id)
        {
            using var db = new AppDbContext();
            var playlist = db.Playlists.FirstOrDefault(p => p.PlaylistID == id);
            if (playlist == null) return false;
            db.Playlists.Remove(playlist);
            db.SaveChanges();
            return true;
        }

        public static void UpdatePlaylist(Playlist updatedPlaylist)
        {
            using var db = new AppDbContext();
            db.Playlists.Update(updatedPlaylist);
            db.SaveChanges();
        }

        public static bool AddTrackToPlaylist(int playlistId, int trackId)
        {
            using var db = new AppDbContext();
            if (db.PlaylistTracks.Any(pt => pt.PlaylistID == playlistId && pt.TrackID == trackId))
                return false;
            db.PlaylistTracks.Add(new PlaylistTrack { PlaylistID = playlistId, TrackID = trackId });
            db.SaveChanges();
            return true;
        }

        public static bool RemoveTrackFromPlaylist(int playlistId, int trackId)
        {
            using var db = new AppDbContext();
            var entry = db.PlaylistTracks.FirstOrDefault(pt => pt.PlaylistID == playlistId && pt.TrackID == trackId);
            if (entry == null) return false;
            db.PlaylistTracks.Remove(entry);
            db.SaveChanges();
            return true;
        }
    }

    public static class FavoriteService
    {
        /// <summary>Возвращает треки из избранного (для инициализации сессии).</summary>
        public static List<Favorite> GetFavoritesByUser(int userId)
        {
            using var db = new AppDbContext();
            return db.Favorites
                .Where(f => f.UserID == userId)
                .Include(f => f.Track).ThenInclude(t => t.Artist)
                .ToList();
        }

        public static bool AddFavorite(int userId, int trackId)
        {
            using var db = new AppDbContext();
            if (db.Favorites.Any(f => f.UserID == userId && f.TrackID == trackId)) return false;
            db.Favorites.Add(new Favorite { UserID = userId, TrackID = trackId });
            var stat = db.Statistics.FirstOrDefault(s => s.TrackID == trackId);
            if (stat != null) stat.LikesCount++;
            db.SaveChanges();
            return true;
        }

        public static bool RemoveFavorite(int userId, int trackId)
        {
            using var db = new AppDbContext();
            var fav = db.Favorites.FirstOrDefault(f => f.UserID == userId && f.TrackID == trackId);
            if (fav == null) return false;
            db.Favorites.Remove(fav);
            var stat = db.Statistics.FirstOrDefault(s => s.TrackID == trackId);
            if (stat != null && stat.LikesCount > 0) stat.LikesCount--;
            db.SaveChanges();
            return true;
        }

        public static bool IsFavorite(int userId, int trackId)
        {
            using var db = new AppDbContext();
            return db.Favorites.Any(f => f.UserID == userId && f.TrackID == trackId);
        }
    }

    public static class SubscriptionService
    {
        public static List<User> GetFollowing(int followerId)
        {
            using var db = new AppDbContext();
            return db.Subscriptions.Where(s => s.FollowerID == followerId)
                .Include(s => s.Artist).Select(s => s.Artist).ToList();
        }

        public static List<User> GetFollowers(int artistId)
        {
            using var db = new AppDbContext();
            return db.Subscriptions.Where(s => s.ArtistID == artistId)
                .Include(s => s.Follower).Select(s => s.Follower).ToList();
        }

        public static bool Subscribe(int followerId, int artistId)
        {
            using var db = new AppDbContext();
            if (followerId == artistId) return false;
            if (db.Subscriptions.Any(s => s.FollowerID == followerId && s.ArtistID == artistId)) return false;
            db.Subscriptions.Add(new Subscription
            {
                FollowerID = followerId,
                ArtistID = artistId,
                SubscribedAt = DateTime.UtcNow
            });
            db.SaveChanges();
            return true;
        }

        /// <summary>New subscriptions per day for the given artist over the last N days.</summary>
        public static Dictionary<DateTime, int> GetSubscriptionsByDay(int artistId, int days = 14)
        {
            using var db = new AppDbContext();
            var since = DateTime.UtcNow.Date.AddDays(-(days - 1));
            return db.Subscriptions
                .Where(s => s.ArtistID == artistId && s.SubscribedAt >= since)
                .Select(s => s.SubscribedAt)
                .ToList()
                .GroupBy(dt => dt.Date)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public static bool Unsubscribe(int followerId, int artistId)
        {
            using var db = new AppDbContext();
            var sub = db.Subscriptions.FirstOrDefault(s => s.FollowerID == followerId && s.ArtistID == artistId);
            if (sub == null) return false;
            db.Subscriptions.Remove(sub);
            db.SaveChanges();
            return true;
        }

        public static bool IsFollowing(int followerId, int artistId)
        {
            using var db = new AppDbContext();
            return db.Subscriptions.Any(s => s.FollowerID == followerId && s.ArtistID == artistId);
        }
    }

    public static class RoleService
    {
        public static List<Role> GetAllRoles()
        {
            using var db = new AppDbContext();
            return db.Roles.ToList();
        }

        public static Role? GetRoleById(int id)
        {
            using var db = new AppDbContext();
            return db.Roles.FirstOrDefault(r => r.RoleId == id);
        }

        public static Role? GetRoleByName(string name)
        {
            using var db = new AppDbContext();
            return db.Roles.FirstOrDefault(r => r.RoleName == name);
        }
    }

    public static class HistoryService
    {
        public static void RecordListen(int userId, int trackId)
        {
            if (userId <= 0 || trackId <= 0) return;
            try
            {
                using var db = new AppDbContext();
                db.History.Add(new ListeningHistory
                {
                    UserID = userId,
                    TrackID = trackId,
                    ListenedAt = DateTime.UtcNow
                });
                db.SaveChanges();
            }
            catch { }
        }

        /// <summary>Listens per day for last N days for a specific track.</summary>
        public static Dictionary<DateTime, int> GetListensByDay(int trackId, int days = 30)
        {
            using var db = new AppDbContext();
            var since = DateTime.UtcNow.Date.AddDays(-(days - 1));
            return db.History
                .Where(h => h.TrackID == trackId && h.ListenedAt >= since)
                .Select(h => h.ListenedAt)
                .ToList()
                .GroupBy(dt => dt.Date)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }

    public static class PlaylistListenService
    {
        public static event Action<int>? OnPlaylistListenChanged;

        /// <summary>Каждое нажатие Play внутри плейлиста = +1 прослушивание.</summary>
        public static void RegisterListen(int userId, int playlistId)
        {
            if (userId <= 0 || playlistId <= 0) return;
            try
            {
                using var db = new AppDbContext();
                db.PlaylistPlayEvents.Add(new PlaylistPlayEvent
                {
                    UserId = userId,
                    PlaylistId = playlistId,
                    ListenedAt = DateTime.UtcNow
                });
                db.SaveChanges();
                OnPlaylistListenChanged?.Invoke(playlistId);
            }
            catch { }
        }

        public static int GetListenerCount(int playlistId)
        {
            if (playlistId <= 0) return 0;
            try
            {
                using var db = new AppDbContext();
                // Старые уникальные прослушивания + новые события Play
                int oldUnique = db.PlaylistListens.Count(l => l.PlaylistId == playlistId);
                int playEvents = db.PlaylistPlayEvents.Count(l => l.PlaylistId == playlistId);
                return oldUnique + playEvents;
            }
            catch { return 0; }
        }
    }

    public static class SavedPlaylistService
    {
        public static event Action<int>? OnPlaylistSavedChanged;

        public static bool IsSaved(int userId, int playlistId)
        {
            using var db = new AppDbContext();
            return db.SavedPlaylists.Any(sp => sp.UserId == userId && sp.PlaylistId == playlistId);
        }

        public static bool Toggle(int userId, int playlistId)
        {
            using var db = new AppDbContext();
            var existing = db.SavedPlaylists.FirstOrDefault(sp => sp.UserId == userId && sp.PlaylistId == playlistId);
            if (existing != null)
            {
                db.SavedPlaylists.Remove(existing);
                db.SaveChanges();
                OnPlaylistSavedChanged?.Invoke(playlistId);
                return false; // removed
            }
            db.SavedPlaylists.Add(new SavedPlaylist { UserId = userId, PlaylistId = playlistId });
            db.SaveChanges();
            OnPlaylistSavedChanged?.Invoke(playlistId);
            return true; // saved
        }

        public static List<Playlist> GetSavedByUser(int userId)
        {
            using var db = new AppDbContext();
            return db.SavedPlaylists
                .Where(sp => sp.UserId == userId)
                .Include(sp => sp.Playlist).ThenInclude(p => p.PlaylistTracks).ThenInclude(pt => pt.Track)
                .Include(sp => sp.Playlist).ThenInclude(p => p.Owner)
                .OrderByDescending(sp => sp.SavedAt)
                .Select(sp => sp.Playlist)
                .Where(p => p != null)
                .ToList()!;
        }

        public static int GetSavedCount(int playlistId)
        {
            using var db = new AppDbContext();
            return db.SavedPlaylists.Count(sp => sp.PlaylistId == playlistId);
        }
    }

    public static class AlbumService
    {
        public static event Action<int>? OnAlbumSavedChanged;
        public static event Action<int>? OnAlbumListenChanged;

        public static Album? GetById(int albumId)
        {
            using var db = new AppDbContext();
            return db.Albums
                .Include(a => a.Artist)
                .Include(a => a.AlbumTracks).ThenInclude(at => at.Track).ThenInclude(t => t.Artist)
                .FirstOrDefault(a => a.AlbumId == albumId);
        }
        public static List<Album> GetByArtist(int artistId)
        {
            using var db = new AppDbContext();
            return db.Albums
                .Where(a => a.ArtistId == artistId)
                .Include(a => a.AlbumTracks).ThenInclude(at => at.Track)
                .OrderByDescending(a => a.CreatedAt).ToList();
        }

        public static int Create(string title, int artistId, string? genre, string? coverPath)
        {
            using var db = new AppDbContext();
            var album = new Album
            {
                Title = title, ArtistId = artistId,
                Genre = genre, CoverPath = coverPath,
                CreatedAt = DateTime.UtcNow
            };
            db.Albums.Add(album);
            db.SaveChanges();
            return album.AlbumId;
        }

        public static bool AddTrack(int albumId, int trackId)
        {
            using var db = new AppDbContext();
            if (db.AlbumTracks.Any(at => at.AlbumId == albumId && at.TrackId == trackId)) return false;
            db.AlbumTracks.Add(new AlbumTrack { AlbumId = albumId, TrackId = trackId });
            db.SaveChanges();
            return true;
        }

        public static bool RemoveTrack(int albumId, int trackId)
        {
            using var db = new AppDbContext();
            var e = db.AlbumTracks.FirstOrDefault(at => at.AlbumId == albumId && at.TrackId == trackId);
            if (e == null) return false;
            db.AlbumTracks.Remove(e);
            db.SaveChanges();
            return true;
        }

        public static bool Update(int albumId, string title, string? genre, string? coverPath)
        {
            using var db = new AppDbContext();
            var a = db.Albums.Find(albumId);
            if (a == null) return false;
            a.Title = title;
            a.Genre = genre;
            if (coverPath != null) a.CoverPath = coverPath;
            db.SaveChanges();
            return true;
        }

        public static bool ToggleSave(int userId, int albumId)
        {
            using var db = new AppDbContext();
            var existing = db.SavedAlbums.FirstOrDefault(sa => sa.UserId == userId && sa.AlbumId == albumId);
            if (existing != null)
            {
                db.SavedAlbums.Remove(existing);
                db.SaveChanges();
                OnAlbumSavedChanged?.Invoke(albumId);
                return false;
            }
            db.SavedAlbums.Add(new SavedAlbum { UserId = userId, AlbumId = albumId, SavedAt = DateTime.UtcNow });
            db.SaveChanges();
            OnAlbumSavedChanged?.Invoke(albumId);
            return true;
        }

        public static bool IsSaved(int userId, int albumId)
        {
            using var db = new AppDbContext();
            return db.SavedAlbums.Any(sa => sa.UserId == userId && sa.AlbumId == albumId);
        }

        public static int GetSavedCount(int albumId)
        {
            using var db = new AppDbContext();
            return db.SavedAlbums.Count(sa => sa.AlbumId == albumId);
        }

        public static List<Album> GetSavedByUser(int userId)
        {
            using var db = new AppDbContext();
            return db.SavedAlbums
                .Where(sa => sa.UserId == userId)
                .Include(sa => sa.Album).ThenInclude(a => a.Artist)
                .Include(sa => sa.Album).ThenInclude(a => a.AlbumTracks).ThenInclude(at => at.Track)
                .OrderByDescending(sa => sa.SavedAt)
                .Select(sa => sa.Album)
                .ToList();
        }

        public static void RegisterListen(int userId, int albumId)
        {
            if (userId <= 0 || albumId <= 0) return;
            using var db = new AppDbContext();
            db.AlbumListenEvents.Add(new AlbumListenEvent { UserId = userId, AlbumId = albumId, ListenedAt = DateTime.UtcNow });
            db.SaveChanges();
            OnAlbumListenChanged?.Invoke(albumId);
        }

        public static int GetListenCount(int albumId)
        {
            using var db = new AppDbContext();
            return db.AlbumListenEvents.Count(e => e.AlbumId == albumId);
        }

        public static bool Delete(int albumId)
        {
            using var db = new AppDbContext();
            var a = db.Albums.Find(albumId);
            if (a == null) return false;
            db.Albums.Remove(a);
            db.SaveChanges();
            return true;
        }
    }

    public static class ArtistRequestService
    {
        /// <summary>Создать новую заявку исполнителя.</summary>
        public static void CreateRequest(string nickname, string email, string passwordHash)
        {
            using var db = new AppDbContext();
            db.ArtistRequests.Add(new ArtistRequest
            {
                Nickname = nickname,
                Email = email,
                PasswordHash = passwordHash,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        /// <summary>Все заявки (для отображения в панели).</summary>
        public static List<ArtistRequest> GetAll()
        {
            using var db = new AppDbContext();
            return db.ArtistRequests
                     .OrderByDescending(r => r.CreatedAt)
                     .ToList();
        }

        /// <summary>Только ожидающие заявки.</summary>
        public static List<ArtistRequest> GetPending()
        {
            using var db = new AppDbContext();
            return db.ArtistRequests
                     .Where(r => r.Status == "Pending")
                     .OrderByDescending(r => r.CreatedAt)
                     .ToList();
        }

        public static int PendingCount()
        {
            using var db = new AppDbContext();
            return db.ArtistRequests.Count(r => r.Status == "Pending");
        }

        /// <summary>Подтвердить заявку: создаёт User с ролью Artist.</summary>
        public static bool Approve(int requestId)
        {
            using var db = new AppDbContext();
            var req = db.ArtistRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (req == null || req.Status != "Pending") return false;

            // Создаём аккаунт
            var user = new User
            {
                Nickname = req.Nickname,
                Email = req.Email,
                PasswordHash = req.PasswordHash,
                RoleId = 2, // Artist
                Status = "Активен"
            };
            db.Users.Add(user);
            req.Status = "Approved";
            db.SaveChanges();
            return true;
        }

        /// <summary>Отклонить заявку.</summary>
        public static bool Reject(int requestId)
        {
            using var db = new AppDbContext();
            var req = db.ArtistRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (req == null || req.Status != "Pending") return false;
            req.Status = "Rejected";
            db.SaveChanges();
            return true;
        }

        /// <summary>Проверка: есть ли уже ожидающая или одобренная заявка с таким email/ником.</summary>
        public static bool HasActiveRequest(string email, string nickname)
        {
            using var db = new AppDbContext();
            return db.ArtistRequests.Any(r =>
                (r.Email == email || r.Nickname == nickname) &&
                (r.Status == "Pending" || r.Status == "Approved"));
        }
    }

    public static class TrackReportService
    {
        public static void CreateReport(int trackId, int reporterId, string reason)
        {
            using var db = new AppDbContext();
            // Prevent duplicate reports from the same user on same track
            if (db.TrackReports.Any(r => r.TrackId == trackId && r.ReporterId == reporterId && r.Status == "Pending"))
                return;
            db.TrackReports.Add(new TrackReport
            {
                TrackId = trackId, ReporterId = reporterId,
                Reason = reason, Status = "Pending", CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        public static List<TrackReport> GetPending()
        {
            using var db = new AppDbContext();
            return db.TrackReports
                .Where(r => r.Status == "Pending")
                .Include(r => r.Track).ThenInclude(t => t.Artist)
                .Include(r => r.Reporter)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public static List<TrackReport> GetAll()
        {
            using var db = new AppDbContext();
            return db.TrackReports
                .Include(r => r.Track).ThenInclude(t => t.Artist)
                .Include(r => r.Reporter)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public static int PendingCount()
        {
            using var db = new AppDbContext();
            return db.TrackReports.Count(r => r.Status == "Pending");
        }

        public static bool BanTrackFromReport(int reportId)
        {
            using var db = new AppDbContext();
            var report = db.TrackReports.Find(reportId);
            if (report == null) return false;
            report.Status = "Banned";
            var track = db.Tracks.Find(report.TrackId);
            if (track != null) track.PublishStatus = "Banned";
            db.SaveChanges();
            return true;
        }

        public static bool Dismiss(int reportId)
        {
            using var db = new AppDbContext();
            var report = db.TrackReports.Find(reportId);
            if (report == null) return false;
            report.Status = "Dismissed";
            db.SaveChanges();
            return true;
        }
    }
}
