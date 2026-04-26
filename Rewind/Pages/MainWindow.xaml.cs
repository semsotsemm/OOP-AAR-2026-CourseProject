using System.Windows;
using Rewind.DbManager;

namespace Rewind;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        CreateDatabase();
    }

    private void CreateDatabase()
    {
        using (var db = new AppDbContext())
        {
            db.Database.EnsureCreated();
        }
    }

    public static bool IsUserExists(string nickname, string email)
    {
        using (var db = new AppDbContext())
        {
            return db.Users.Any(u => u.Nickname == nickname || u.Email == email);
        }
    }
    private void RegisterNewUser(object sender, RoutedEventArgs e)
    {
        try
        {
            string nickname = UserNickname.Text;
            string email = UserEmail.Text;
            string password = UserPassword.Text;

            if (!int.TryParse(UserRole.Text, out int roleId))
            {
                MessageBox.Show("Введите корректный ID роли (число)!");
                return;
            }

            if (IsUserExists(nickname, email)) 
            {
                MessageBox.Show("Никнейм или почта уже занята.");
                return;
            }

            User new_user = new User
            {
                Nickname = nickname,
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(password), 
                RoleId = roleId
            };

            UserService.AddUser(new_user);

            MessageBox.Show($"Пользователь {nickname} успешно добавлен!");

            UserNickname.Clear();
            UserEmail.Clear();
            UserPassword.Clear();
            UserRole.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}");
        }
    }

    private void UpdateUser(object sender, RoutedEventArgs e) 
    {
        string nickname = UserNickname.Text;
        string email = UserEmail.Text;
        string password = UserPassword.Text;
        User user = UserService.GetUserByEmail(email);
        User new_user = new User
        {
            Nickname = nickname,
            Email = email,
            PasswordHash = PasswordHelper.HashPassword(password),
            RoleId = user.RoleId
        };
        UserService.UpdateUser(user, new_user);
     
        MessageBox.Show("Пользователь успешно обновлен");
    }
}