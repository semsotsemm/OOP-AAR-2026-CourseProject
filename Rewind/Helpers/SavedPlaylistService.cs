using Microsoft.EntityFrameworkCore;

namespace Rewind.Helpers
{
    public static class SavedPlaylistService
    {
        public static event Action<int>? OnPlaylistSavedChanged;

        public static bool IsSaved(int userId, int playlistId)
        {
            using var db = new AppDbContext();
            return db.SavedPlaylists.Any(sp => sp.UserId == userId && sp.PlaylistId == playlistId);
        }

        public static bool Toggle(int userId, int playlistId)
        {
            using var db = new AppDbContext();
            var existing = db.SavedPlaylists.FirstOrDefault(sp => sp.UserId == userId && sp.PlaylistId == playlistId);
            if (existing != null)
            {
                db.SavedPlaylists.Remove(existing);
                db.SaveChanges();
                OnPlaylistSavedChanged?.Invoke(playlistId);
                return false; // removed
            }
            db.SavedPlaylists.Add(new SavedPlaylist { UserId = userId, PlaylistId = playlistId });
            db.SaveChanges();
            OnPlaylistSavedChanged?.Invoke(playlistId);
            return true; // saved
        }

        public static List<Playlist> GetSavedByUser(int userId)
        {
            using var db = new AppDbContext();
            return db.SavedPlaylists
                .Where(sp => sp.UserId == userId)
                .Include(sp => sp.Playlist).ThenInclude(p => p.PlaylistTracks).ThenInclude(pt => pt.Track)
                .Include(sp => sp.Playlist).ThenInclude(p => p.Owner)
                .OrderByDescending(sp => sp.SavedAt)
                .Select(sp => sp.Playlist)
                .Where(p => p != null)
                .ToList()!;
        }

        public static int GetSavedCount(int playlistId)
        {
            using var db = new AppDbContext();
            return db.SavedPlaylists.Count(sp => sp.PlaylistId == playlistId);
        }

        /// <summary>Батч-версия: количество сохранений для списка плейлистов одним запросом.</summary>
        public static Dictionary<int, int> GetSavedCounts(IEnumerable<int> playlistIds)
        {
            var ids = playlistIds.ToList();
            if (ids.Count == 0) return new Dictionary<int, int>();
            using var db = new AppDbContext();
            return db.SavedPlaylists
                .Where(sp => ids.Contains(sp.PlaylistId))
                .GroupBy(sp => sp.PlaylistId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Id, x => x.Count);
        }
    }
}
