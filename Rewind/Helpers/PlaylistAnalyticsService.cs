namespace Rewind.Helpers
{
    public static class PlaylistAnalyticsService
    {
        private static readonly Dictionary<int, HashSet<int>> _playlistLikes = new();
        private static readonly Dictionary<int, HashSet<int>> _playlistListeners = new();

        public static int GetLikesCount(int playlistId)
        {
            if (playlistId <= 0) return 0;
            return _playlistLikes.TryGetValue(playlistId, out var users) ? users.Count : 0;
        }

        public static int GetListenersCount(int playlistId)
        {
            if (playlistId <= 0) return 0;
            return _playlistListeners.TryGetValue(playlistId, out var users) ? users.Count : 0;
        }

        public static bool IsLikedByUser(int userId, int playlistId)
        {
            if (userId <= 0 || playlistId <= 0) return false;
            return _playlistLikes.TryGetValue(playlistId, out var users) && users.Contains(userId);
        }

        public static bool ToggleLike(int userId, int playlistId)
        {
            if (userId <= 0 || playlistId <= 0) return false;

            if (!_playlistLikes.TryGetValue(playlistId, out var users))
            {
                users = new HashSet<int>();
                _playlistLikes[playlistId] = users;
            }

            if (users.Contains(userId))
            {
                users.Remove(userId);
                return false;
            }

            users.Add(userId);
            return true;
        }

        public static void RegisterListen(int userId, int playlistId)
        {
            if (userId <= 0 || playlistId <= 0) return;

            if (!_playlistListeners.TryGetValue(playlistId, out var users))
            {
                users = new HashSet<int>();
                _playlistListeners[playlistId] = users;
            }

            users.Add(userId);
        }

        public static List<Playlist> GetTopPlaylists(int count = 10)
        {
            var all = PlaylistService.GetPublicPlaylists().Concat(Session.CachedPlaylists).GroupBy(p => p.PlaylistID).Select(g => g.First());
            return all
                .OrderByDescending(p => GetLikesCount(p.PlaylistID) + GetListenersCount(p.PlaylistID))
                .ThenByDescending(p => p.PlaylistTracks?.Count ?? 0)
                .Take(count)
                .ToList();
        }
    }
}
