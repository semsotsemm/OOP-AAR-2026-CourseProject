using Microsoft.EntityFrameworkCore;

namespace Rewind.Helpers
{
    public static class TrackService
    {
        public static event Action<int, int>? OnPlayCountUpdated;
        public static event Action<int, string, string>? NewTrackUploaded;
        public static void NotifyNewTrackUploaded(int artistId, string artistName, string trackTitle)
            => NewTrackUploaded?.Invoke(artistId, artistName, trackTitle);

        public static List<Track> GetAllTracks()
        {
            using var db = new AppDbContext();
            return db.Tracks.Include(t => t.Artist).Include(t => t.Statistics).ToList();
        }

        public static Track? GetTrackById(int id)
        {
            using var db = new AppDbContext();
            return db.Tracks.Include(t => t.Artist).Include(t => t.Statistics)
                            .FirstOrDefault(t => t.TrackID == id);
        }

        public static List<Track> GetTracksByArtist(int artistId)
        {
            using var db = new AppDbContext();
            return db.Tracks.Include(t => t.Statistics)
                            .Where(t => t.ArtistID == artistId).ToList();
        }

        public static Track? GetMostPopularTrack()
        {
            using var db = new AppDbContext();
            return db.Tracks
                .Where(t => t.PublishStatus == "Published")
                .Include(t => t.Artist).Include(t => t.Statistics)
                .OrderByDescending(t => t.Statistics.PlayCount + t.Statistics.LikesCount * 10)
                .FirstOrDefault();
        }

        public static void IncrementPlayCount(int trackId)
        {
            using var db = new AppDbContext();
            var stats = db.Statistics.FirstOrDefault(s => s.TrackID == trackId);

            if (stats != null)
            {
                stats.PlayCount++;
                db.SaveChanges();

                OnPlayCountUpdated?.Invoke(trackId, stats.PlayCount);
            }
        }

        public static void AddTrack(Track track)
        {
            using var db = new AppDbContext();
            db.Tracks.Add(track);
            db.SaveChanges();
            db.Statistics.Add(new Statistic { TrackID = track.TrackID });
            db.SaveChanges();
        }

        public static List<Track> GetPublishedTracks()
        {
            using var db = new AppDbContext();
            return db.Tracks
                .Where(t => t.PublishStatus == "Published")
                .Include(t => t.Artist).Include(t => t.Statistics).ToList();
        }

        public static List<Track> GetPendingTracks()
        {
            using var db = new AppDbContext();
            return db.Tracks
                .Where(t => t.PublishStatus == "Pending")
                .Include(t => t.Artist).Include(t => t.Statistics)
                .OrderByDescending(t => t.UploadDate).ToList();
        }

        public static List<Track> GetByArtistAll(int artistId)
        {
            using var db = new AppDbContext();
            return db.Tracks
                .Where(t => t.ArtistID == artistId)
                .Include(t => t.Statistics)
                .OrderByDescending(t => t.UploadDate).ToList();
        }

        public static bool ApproveTrack(int trackId, string? editedTitle, string? editedGenre)
        {
            using var db = new AppDbContext();
            var track = db.Tracks.Find(trackId);
            if (track == null) return false;
            track.PublishStatus = "Published";
            if (!string.IsNullOrWhiteSpace(editedTitle)) track.Title = editedTitle;
            if (!string.IsNullOrWhiteSpace(editedGenre)) track.Genre = editedGenre;
            db.SaveChanges();
            return true;
        }

        public static bool RejectTrack(int trackId, string reason)
        {
            using var db = new AppDbContext();
            var track = db.Tracks.Find(trackId);
            if (track == null) return false;
            track.PublishStatus = "Rejected";
            track.RejectionReason = reason;
            db.SaveChanges();
            return true;
        }

        public static bool BanUnbanTrack(int trackId) => DeleteTrack(trackId);

        public static bool DeleteTrack(int id)
        {
            using var db = new AppDbContext();
            var track = db.Tracks.FirstOrDefault(t => t.TrackID == id);
            if (track == null) return false;

            db.Favorites.RemoveRange(db.Favorites.Where(f => f.TrackID == id));
            db.PlaylistTracks.RemoveRange(db.PlaylistTracks.Where(pt => pt.TrackID == id));
            db.History.RemoveRange(db.History.Where(h => h.TrackID == id));
            db.AlbumTracks.RemoveRange(db.AlbumTracks.Where(at => at.TrackId == id));
            db.TrackReports.RemoveRange(db.TrackReports.Where(r => r.TrackId == id));
            var stat = db.Statistics.Find(id);
            if (stat != null) db.Statistics.Remove(stat);

            db.Tracks.Remove(track);
            db.SaveChanges();
            return true;
        }

        public static void UpdateTrack(Track updatedTrack)
        {
            using var db = new AppDbContext();
            db.Tracks.Update(updatedTrack);
            db.SaveChanges();
        }
    }
}
