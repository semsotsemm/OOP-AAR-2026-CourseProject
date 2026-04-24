using Rewind.DbManager;
using System.Windows;

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

            User new_user = new User
            {
                Nickname = nickname,
                Email = email,
                PasswordHash = password, 
                RoleId = roleId
            };
            
            UserService.AddUser(new_user);

            MessageBox.Show($"Пользователь {nickname} успешно добавлен!");

            UserNickname.Clear();
            UserEmail.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}");
        }
    }
}