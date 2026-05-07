using Microsoft.EntityFrameworkCore;

namespace Rewind.Helpers
{
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
            catch
            {

            }
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
            catch
            {

            }
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
            => o.UseNpgsql("Host=localhost;Port=5433;Database=rewinddb;Username=postgres;Password=5329965");

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
}
