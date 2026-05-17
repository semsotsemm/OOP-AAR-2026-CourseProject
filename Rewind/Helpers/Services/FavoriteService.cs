using Microsoft.EntityFrameworkCore;

namespace Rewind.Helpers
{
    public static class FavoriteService
    {
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
}
