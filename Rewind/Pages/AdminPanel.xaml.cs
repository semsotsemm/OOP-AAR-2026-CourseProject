using Rewind.Helpers;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Rewind.Pages
{
    public partial class AdminPanel : Window
    {
        private sealed class UserRow
        {
            public int UserId { get; set; }
            public string Nickname { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int TracksCount { get; set; }
        }

        public AdminPanel()
        {
            InitializeComponent();
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            using var db = new AppDbContext();

            var users = db.Users.ToList();
            var tracksCount = db.Tracks.Count();
            var playlistsCount = db.Playlists.Count();

            UsersCountText.Text = users.Count.ToString();
            TracksCountText.Text = tracksCount.ToString();
            PlaylistsCountText.Text = playlistsCount.ToString();

            UsersGrid.ItemsSource = users
                .OrderBy(u => u.UserId)
                .Select(u => new UserRow
                {
                    UserId = u.UserId,
                    Nickname = u.Nickname,
                    Email = u.Email,
                    Role = ResolveRole(u.RoleId),
                    Status = string.IsNullOrWhiteSpace(u.Status) ? "Активен" : u.Status,
                    TracksCount = db.Tracks.Count(t => t.ArtistID == u.UserId)
                })
                .ToList();
        }

        private static string ResolveRole(int roleId) => roleId switch
        {
            1 => "Администратор",
            2 => "Исполнитель",
            _ => "Слушатель"
        };

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadDashboard();

        private void BanUnban_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not UserRow row) return;
            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.UserId == row.UserId);
            if (user == null) return;

            user.Status = user.Status == "Заблокирован" ? "Активен" : "Заблокирован";
            db.SaveChanges();
            LoadDashboard();
        }

        private void Promote_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not UserRow row) return;
            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.UserId == row.UserId);
            if (user == null) return;

            user.RoleId = user.RoleId == 2 ? 1 : 2;
            db.SaveChanges();
            LoadDashboard();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not UserRow row) return;
            if (row.Role == "Администратор")
            {
                MessageBox.Show("Администратора удалять нельзя.");
                return;
            }

            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.UserId == row.UserId);
            if (user == null) return;
            db.Users.Remove(user);
            db.SaveChanges();
            LoadDashboard();
        }
    }
}
