using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text;

namespace Rewind.Helpers
{
    public class AppDbContext : DbContext
    {
        private static volatile bool _schemaInitialized;
        private static readonly object _initLock = new();

        public AppDbContext()
        {
            if (_schemaInitialized) return;
            lock (_initLock)
            {
                if (_schemaInitialized) return;
                EnsureDatabaseAndSchemaInitialized();
                _schemaInitialized = true;
            }
        }

        private void EnsureDatabaseAndSchemaInitialized()
        {
            EnsurePublicSchema();
            var creator = this.GetService<IRelationalDatabaseCreator>();

            try
            {
                Database.EnsureCreated();
            }
            catch
            {
            }

            try
            {
                if (!creator.HasTables())
                {
                    creator.CreateTables();
                }
            }
            catch
            {
            }

            EnsureBaseSchemaUpdates();
            EnsureArtistRequestsTable();
            EnsureTrackSchemaUpdates();
            EnsureAlbumsTables();
            EnsureSubscriptionTimestamp();
            SeedDefaultAdmin();
        }

        private void EnsurePublicSchema()
        {
            try
            {
                Database.ExecuteSqlRaw(@"CREATE SCHEMA IF NOT EXISTS public");
            }
            catch
            {
            }
        }

        private void EnsureBaseSchemaUpdates()
        {
            try { Database.ExecuteSqlRaw(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""ProfilePhotoPath"" TEXT"); } catch { }
            try { Database.ExecuteSqlRaw(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Status"" TEXT"); } catch { }
            try { Database.ExecuteSqlRaw(@"ALTER TABLE ""Playlists"" ADD COLUMN IF NOT EXISTS ""IsPrivate"" BOOLEAN NOT NULL DEFAULT FALSE"); } catch { }
            try { Database.ExecuteSqlRaw(@"ALTER TABLE ""Playlists"" ADD COLUMN IF NOT EXISTS ""CoverPath"" TEXT"); } catch { }
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
            => o.UseNpgsql(GetConnectionString());

        private static string GetConnectionString()
        {
            var fromEnv = Environment.GetEnvironmentVariable("REWIND_DB_CONNECTION");
            if (!string.IsNullOrWhiteSpace(fromEnv))
                return NormalizeConnectionString(fromEnv);

            return NormalizeConnectionString("postgresql://neondb_owner:npg_SzZQ1vfPTqN5@ep-raspy-waterfall-ap8gyjmk-pooler.c-7.us-east-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require");
        }

        private static string NormalizeConnectionString(string value)
        {
            var text = value.Trim();
            if (!(text.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                  text.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
            {
                return text;
            }

            var uri = new Uri(text);
            var userInfo = uri.UserInfo.Split(':', 2);
            var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var database = uri.AbsolutePath.Trim('/');
            var port = uri.IsDefaultPort ? 5432 : uri.Port;

            var sb = new StringBuilder();
            sb.Append("Host=").Append(uri.Host).Append(';');
            sb.Append("Port=").Append(port).Append(';');
            if (!string.IsNullOrWhiteSpace(database))
                sb.Append("Database=").Append(database).Append(';');
            if (!string.IsNullOrWhiteSpace(user))
                sb.Append("Username=").Append(user).Append(';');
            if (!string.IsNullOrWhiteSpace(password))
                sb.Append("Password=").Append(password).Append(';');

            if (!string.IsNullOrWhiteSpace(uri.Query))
            {
                var query = uri.Query.TrimStart('?');
                foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = part.Split('=', 2);
                    var key = NormalizeNpgsqlKeyword(Uri.UnescapeDataString(kv[0]));
                    var val = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    sb.Append(key).Append('=').Append(val).Append(';');
                }
            }

            return sb.ToString();
        }

        private static string NormalizeNpgsqlKeyword(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "";

            var compact = key.Replace("_", "").Replace("-", "").Replace(" ", "")
                .ToLowerInvariant();
            return compact switch
            {
                "sslmode" => "SSL Mode",
                "trustservercertificate" => "Trust Server Certificate",
                "channelbinding" => "Channel Binding",
                _ => key
            };
        }

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

        public override int SaveChanges()
        {
            EnsureDefaultRoles();
            return base.SaveChanges();
        }

        private void SeedDefaultAdmin()
        {
            try
            {
                EnsureDefaultRoles();
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
            }
        }

        private void EnsureDefaultRoles()
        {
            try
            {
                Database.ExecuteSqlRaw(@"INSERT INTO ""Roles"" (""RoleId"", ""RoleName"") VALUES (1, 'Admin') ON CONFLICT (""RoleId"") DO NOTHING");
                Database.ExecuteSqlRaw(@"INSERT INTO ""Roles"" (""RoleId"", ""RoleName"") VALUES (2, 'Artist') ON CONFLICT (""RoleId"") DO NOTHING");
                Database.ExecuteSqlRaw(@"INSERT INTO ""Roles"" (""RoleId"", ""RoleName"") VALUES (3, 'Listener') ON CONFLICT (""RoleId"") DO NOTHING");
            }
            catch
            {
            }
        }
    }
}