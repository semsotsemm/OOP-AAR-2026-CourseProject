using Microsoft.EntityFrameworkCore;

namespace Rewind.Helpers
{
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
}
