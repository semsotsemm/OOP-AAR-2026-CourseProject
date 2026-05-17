using Microsoft.EntityFrameworkCore;

namespace Rewind.Helpers
{
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
}
