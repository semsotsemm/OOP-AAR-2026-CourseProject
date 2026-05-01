using Rewind.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rewind.Tabs.AdminTabs
{
    public partial class UsersTab : UserControl, IAdminTab
    {
        private sealed class UserRow
        {
            public int UserId { get; set; }
            public string Nickname { get; set; } = "";
            public string Email { get; set; } = "";
            public string Role { get; set; } = "";
            public string Status { get; set; } = "";
            public int TracksCount { get; set; }

            // Badge colors
            public Brush RoleBg { get; set; } = Brushes.Transparent;
            public Brush RoleFg { get; set; } = Brushes.Black;
            public Brush StatusBg { get; set; } = Brushes.Transparent;
            public Brush StatusFg { get; set; } = Brushes.Black;
        }

        private List<UserRow> _allUsers = new();

        public UsersTab()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadUsers();
        }

        public void Refresh() => LoadUsers();

        private void LoadUsers()
        {
            try
            {
                using var db = new AppDbContext();
                _allUsers = db.Users
                    .OrderBy(u => u.UserId)
                    .Select(u => new
                    {
                        u.UserId, u.Nickname, u.Email, u.RoleId, u.Status,
                        TracksCount = db.Tracks.Count(t => t.ArtistID == u.UserId)
                    })
                    .ToList()
                    .Select(u => new UserRow
                    {
                        UserId = u.UserId,
                        Nickname = u.Nickname,
                        Email = u.Email,
                        Role = ResolveRole(u.RoleId),
                        Status = string.IsNullOrWhiteSpace(u.Status) ? "Активен" : u.Status,
                        TracksCount = u.TracksCount,
                        RoleBg = GetRoleBg(u.RoleId),
                        RoleFg = GetRoleFg(u.RoleId),
                        StatusBg = GetStatusBg(string.IsNullOrWhiteSpace(u.Status) ? "Активен" : u.Status),
                        StatusFg = GetStatusFg(string.IsNullOrWhiteSpace(u.Status) ? "Активен" : u.Status)
                    })
                    .ToList();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UsersTab] {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            var q = SearchBox.Text?.Trim().ToLower() ?? "";
            var filtered = string.IsNullOrWhiteSpace(q)
                ? _allUsers
                : _allUsers.Where(u =>
                    u.Nickname.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    u.Role.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    u.Status.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

            UsersGrid.ItemsSource = filtered;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void BanUnban_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not UserRow row) return;
            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.UserId == row.UserId);
            if (user == null) return;
            user.Status = user.Status == "Заблокирован" ? "Активен" : "Заблокирован";
            db.SaveChanges();
            LoadUsers();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not UserRow row) return;
            if (row.Role == "Администратор")
            {
                MessageBox.Show("Администратора удалять нельзя.", "Rewind Admin",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var result = MessageBox.Show($"Удалить пользователя «{row.Nickname}»?", "Rewind Admin",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.UserId == row.UserId);
            if (user == null) return;
            db.Users.Remove(user);
            db.SaveChanges();
            LoadUsers();
        }

        // ── Helpers ──

        private static string ResolveRole(int roleId) => roleId switch
        {
            1 => "Администратор",
            2 => "Исполнитель",
            _ => "Слушатель"
        };

        private static Brush GetRoleBg(int roleId) => roleId switch
        {
            1 => new SolidColorBrush(Color.FromRgb(255, 243, 205)),
            2 => new SolidColorBrush(Color.FromRgb(219, 234, 254)),
            _ => new SolidColorBrush(Color.FromRgb(232, 245, 233))
        };

        private static Brush GetRoleFg(int roleId) => roleId switch
        {
            1 => new SolidColorBrush(Color.FromRgb(146, 64, 14)),
            2 => new SolidColorBrush(Color.FromRgb(30, 64, 175)),
            _ => new SolidColorBrush(Color.FromRgb(21, 128, 61))
        };

        private static Brush GetStatusBg(string status) =>
            status == "Заблокирован"
                ? new SolidColorBrush(Color.FromRgb(254, 226, 226))
                : new SolidColorBrush(Color.FromRgb(220, 252, 231));

        private static Brush GetStatusFg(string status) =>
            status == "Заблокирован"
                ? new SolidColorBrush(Color.FromRgb(185, 28, 28))
                : new SolidColorBrush(Color.FromRgb(21, 128, 61));
    }
}
