using Microsoft.EntityFrameworkCore;

namespace Rewind.Helpers
{
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
}
