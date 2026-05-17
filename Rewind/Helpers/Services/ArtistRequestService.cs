namespace Rewind.Helpers
{
    public static class ArtistRequestService
    {
        public static void CreateRequest(string nickname, string email, string passwordHash)
        {
            using var db = new AppDbContext();
            db.ArtistRequests.Add(new ArtistRequest
            {
                Nickname = nickname,
                Email = email,
                PasswordHash = passwordHash,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        public static List<ArtistRequest> GetAll()
        {
            using var db = new AppDbContext();
            return db.ArtistRequests
                     .OrderByDescending(r => r.CreatedAt)
                     .ToList();
        }

        public static List<ArtistRequest> GetPending()
        {
            using var db = new AppDbContext();
            return db.ArtistRequests
                     .Where(r => r.Status == "Pending")
                     .OrderByDescending(r => r.CreatedAt)
                     .ToList();
        }

        public static int PendingCount()
        {
            using var db = new AppDbContext();
            return db.ArtistRequests.Count(r => r.Status == "Pending");
        }

        public static bool Approve(int requestId)
        {
            using var db = new AppDbContext();
            var req = db.ArtistRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (req == null || req.Status != "Pending") return false;

            var user = new User
            {
                Nickname = req.Nickname,
                Email = req.Email,
                PasswordHash = req.PasswordHash,
                RoleId = 2, 
                Status = "Активен"
            };
            db.Users.Add(user);
            req.Status = "Approved";
            db.SaveChanges();
            return true;
        }

        public static bool Reject(int requestId)
        {
            using var db = new AppDbContext();
            var req = db.ArtistRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (req == null || req.Status != "Pending") return false;
            req.Status = "Rejected";
            db.SaveChanges();
            return true;
        }

        public static bool HasActiveRequest(string email, string nickname)
        {
            using var db = new AppDbContext();
            return db.ArtistRequests.Any(r =>
                (r.Email == email || r.Nickname == nickname) &&
                (r.Status == "Pending" || r.Status == "Approved"));
        }
    }
}
