using Microsoft.EntityFrameworkCore;

namespace Rewind.Helpers
{
    public static class TrackReportService
    {
        public static void CreateReport(int trackId, int reporterId, string reason)
        {
            using var db = new AppDbContext();
            // Prevent duplicate reports from the same user on same track
            if (db.TrackReports.Any(r => r.TrackId == trackId && r.ReporterId == reporterId && r.Status == "Pending"))
                return;
            db.TrackReports.Add(new TrackReport
            {
                TrackId = trackId, ReporterId = reporterId,
                Reason = reason, Status = "Pending", CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        public static List<TrackReport> GetPending()
        {
            using var db = new AppDbContext();
            return db.TrackReports
                .Where(r => r.Status == "Pending")
                .Include(r => r.Track).ThenInclude(t => t.Artist)
                .Include(r => r.Reporter)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public static List<TrackReport> GetAll()
        {
            using var db = new AppDbContext();
            return db.TrackReports
                .Include(r => r.Track).ThenInclude(t => t.Artist)
                .Include(r => r.Reporter)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public static int PendingCount()
        {
            using var db = new AppDbContext();
            return db.TrackReports.Count(r => r.Status == "Pending");
        }

        public static bool BanTrackFromReport(int reportId)
        {
            int trackId;
            using (var db = new AppDbContext())
            {
                var report = db.TrackReports.Find(reportId);
                if (report == null) return false;
                trackId = report.TrackId;
            }
            // TrackService.DeleteTrack удаляет трек и все связанные записи, включая этот репорт
            return TrackService.DeleteTrack(trackId);
        }

        public static bool Dismiss(int reportId)
        {
            using var db = new AppDbContext();
            var report = db.TrackReports.Find(reportId);
            if (report == null) return false;
            report.Status = "Dismissed";
            db.SaveChanges();
            return true;
        }
    }
}
