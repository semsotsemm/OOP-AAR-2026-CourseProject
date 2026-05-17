namespace Rewind.Helpers
{
    public static class PlaylistListenService
    {
        public static event Action<int>? OnPlaylistListenChanged;

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
                int oldUnique = db.PlaylistListens.Count(l => l.PlaylistId == playlistId);
                int playEvents = db.PlaylistPlayEvents.Count(l => l.PlaylistId == playlistId);
                return oldUnique + playEvents;
            }
            catch { return 0; }
        }

        public static Dictionary<int, int> GetListenerCounts(IEnumerable<int> playlistIds)
        {
            var ids = playlistIds.ToList();
            if (ids.Count == 0) return new Dictionary<int, int>();
            try
            {
                using var db = new AppDbContext();
                var oldUnique = db.PlaylistListens
                    .Where(l => ids.Contains(l.PlaylistId))
                    .GroupBy(l => l.PlaylistId)
                    .Select(g => new { Id = g.Key, Count = g.Count() })
                    .ToDictionary(x => x.Id, x => x.Count);
                var playEvents = db.PlaylistPlayEvents
                    .Where(l => ids.Contains(l.PlaylistId))
                    .GroupBy(l => l.PlaylistId)
                    .Select(g => new { Id = g.Key, Count = g.Count() })
                    .ToDictionary(x => x.Id, x => x.Count);

                var result = new Dictionary<int, int>(ids.Count);
                foreach (var id in ids)
                    result[id] = oldUnique.GetValueOrDefault(id, 0) + playEvents.GetValueOrDefault(id, 0);
                return result;
            }
            catch { return new Dictionary<int, int>(); }
        }
    }
}
