using Microsoft.EntityFrameworkCore;

namespace Rewind.Helpers
{
    public static class PlaylistService
    {
        /// <summary>Плейлисты пользователя (его собственные — все, чужие — только публичные).</summary>
        public static List<Playlist> GetPlaylistsByUser(int userId)
        {
            using var db = new AppDbContext();
            return db.Playlists
                .Include(p => p.PlaylistTracks).ThenInclude(pt => pt.Track)
                .Where(p => p.OwnerID == userId)
                .ToList();
        }

        /// <summary>Публичные плейлисты всех пользователей (для раздела «Сохранённые»).</summary>
        public static List<Playlist> GetPublicPlaylists(int excludeUserId = 0)
        {
            using var db = new AppDbContext();
            return db.Playlists
                .Include(p => p.Owner)
                .Include(p => p.PlaylistTracks).ThenInclude(pt => pt.Track)
                .Where(p => !p.IsPrivate && p.OwnerID != excludeUserId)
                .ToList();
        }

        public static Playlist? GetPlaylistById(int id)
        {
            using var db = new AppDbContext();
            return db.Playlists
                .Include(p => p.Owner)
                .Include(p => p.PlaylistTracks).ThenInclude(pt => pt.Track)
                .FirstOrDefault(p => p.PlaylistID == id);
        }

        public static void AddPlaylist(Playlist playlist)
        {
            using var db = new AppDbContext();
            // Сбрасываем навигационные свойства чтобы EF не дублировал Owner
            playlist.Owner = null!;
            db.Playlists.Add(playlist);
            db.SaveChanges();
        }

        public static bool DeletePlaylist(int id)
        {
            using var db = new AppDbContext();
            var playlist = db.Playlists.FirstOrDefault(p => p.PlaylistID == id);
            if (playlist == null) return false;
            db.Playlists.Remove(playlist);
            db.SaveChanges();
            return true;
        }

        public static void UpdatePlaylist(Playlist updatedPlaylist)
        {
            using var db = new AppDbContext();
            db.Playlists.Update(updatedPlaylist);
            db.SaveChanges();
        }

        public static bool AddTrackToPlaylist(int playlistId, int trackId)
        {
            using var db = new AppDbContext();
            if (db.PlaylistTracks.Any(pt => pt.PlaylistID == playlistId && pt.TrackID == trackId))
                return false;
            db.PlaylistTracks.Add(new PlaylistTrack { PlaylistID = playlistId, TrackID = trackId });
            db.SaveChanges();
            return true;
        }

        public static bool RemoveTrackFromPlaylist(int playlistId, int trackId)
        {
            using var db = new AppDbContext();
            var entry = db.PlaylistTracks.FirstOrDefault(pt => pt.PlaylistID == playlistId && pt.TrackID == trackId);
            if (entry == null) return false;
            db.PlaylistTracks.Remove(entry);
            db.SaveChanges();
            return true;
        }
    }
}
