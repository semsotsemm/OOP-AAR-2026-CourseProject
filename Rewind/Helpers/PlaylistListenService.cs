namespace Rewind.Helpers
{
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
}
