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
    }

    public class Favorite
    {
        public int UserID { get; set; }
        public User User { get; set; }
        public int TrackID { get; set; }
        public Track Track { get; set; }
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
            SeedDefaultAdmin();
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
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
            db.Subscriptions.Add(new Subscription { FollowerID = followerId, ArtistID = artistId });
            db.SaveChanges();
            return true;
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
}
