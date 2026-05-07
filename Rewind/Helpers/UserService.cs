using Microsoft.EntityFrameworkCore;

namespace Rewind.Helpers
{
    public class UserStatisticsDto
    {
        public int SubscriptionsCount { get; set; }
        public int TotalTracksListened { get; set; }
        public int TotalTimeFormatted { get; set; }
        public int PlaylistsCount { get; set; }
        public int FavoritesCount { get; set; }

        public static UserStatisticsDto GetUserStats(int userId)
        {
            using var db = new AppDbContext();

            var subsCount = db.Subscriptions.Count(s => s.FollowerID == userId);
            var playlistsCount = db.Playlists.Count(p => p.OwnerID == userId);
            var favoritesCount = db.Favorites.Count(f => f.UserID == userId);

            var durations = db.History
                .Where(h => h.UserID == userId)
                .Select(h => h.Track.Duration)
                .ToList();

            int totalTracks = durations.Count;
            int totalSeconds = durations.Sum();
            int totalMinutes = (int)TimeSpan.FromSeconds(totalSeconds).TotalMinutes;

            return new UserStatisticsDto
            {
                SubscriptionsCount = subsCount,
                PlaylistsCount = playlistsCount,
                FavoritesCount = favoritesCount,
                TotalTracksListened = totalTracks,
                TotalTimeFormatted = totalMinutes
            };
        }
    }

    public static class UserService
    {
        public static List<User> GetAllUsers()
        {
            using var db = new AppDbContext();
            return db.Users.Include(u => u.Role).ToList();
        }

        public static User? GetUserById(int id)
        {
            using var db = new AppDbContext();
            return db.Users.Include(u => u.Role).Include(u => u.Tracks)
                           .FirstOrDefault(u => u.UserId == id);
        }

        public static User? GetUserByNickname(string nickname)
        {
            using var db = new AppDbContext();
            return db.Users.Include(u => u.Role).FirstOrDefault(u => u.Nickname == nickname);
        }

        public static User? GetUserByEmail(string email)
        {
            using var db = new AppDbContext();
            return db.Users.FirstOrDefault(u => u.Email == email);
        }

        public static int AddUser(User newUser)
        {
            using var db = new AppDbContext();
            db.Users.Add(newUser);
            db.SaveChanges();
            return newUser.UserId;
        }

        public static bool DeleteUser(int id)
        {
            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null) return false;
            db.Users.Remove(user);
            db.SaveChanges();
            return true;
        }

        public static void UpdateUser(User currentUser, User newUser)
        {
            using var db = new AppDbContext();
            var userInDb = db.Users.Find(currentUser.UserId)
                ?? throw new Exception("Пользователь не найден в базе данных.");

            userInDb.Nickname = newUser.Nickname;
            userInDb.Email = newUser.Email;
            userInDb.RoleId = newUser.RoleId;
            userInDb.ProfilePhotoPath = newUser.ProfilePhotoPath;

            if (!string.IsNullOrWhiteSpace(newUser.PasswordHash))
                userInDb.PasswordHash = newUser.PasswordHash;

            db.SaveChanges();
        }
    }
}
