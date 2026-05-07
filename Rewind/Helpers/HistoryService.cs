namespace Rewind.Helpers
{
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
}
