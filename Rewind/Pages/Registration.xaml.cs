using System;
using System.IO;
using System.Windows;
using Rewind.Helpers;
using System.Windows.Controls;
using System.Text.RegularExpressions; // Добавлено для работы с регулярными выражениями

namespace Rewind.Pages
{
    public partial class Registration : Window
    {
        // UI роли: 1 = Пользователь, 2 = Исполнитель, 3 = Админ
        private int selectedRoleId = 1;

        // true = Вход, false = Регистрация
        private bool isLoginMode = true;

        public Registration()
        {
            InitializeComponent();
            UpdateUI();
        }

        private static string NormalizeRoleName(int roleId) => roleId switch
        {
            1 => "Слушатель",
            2 => "Исполнитель",
            3 => "Администратор",
            _ => "Слушатель"
        };

        private static int UiRoleToDbRoleId(int uiRoleId) => uiRoleId switch
        {
            1 => 3, // Listener
            2 => 2, // Artist
            3 => 1, // Admin
            _ => 3
        };

        // Регистрация
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string email = "";
            string nickname = "";
            string password = "";
            string confirmPassword = "";

            switch (selectedRoleId)
            {
                case 1:
                    email = UserRegEmail.Text;
                    nickname = UserRegNickname.Text;
                    password = UserRegPassword.Password;
                    confirmPassword = UserRegPasswordConfirm.Password;
                    break;
                case 2:
                    email = ArtistRegEmail.Text;
                    nickname = ArtistRegNickname.Text;
                    password = ArtistRegPassword.Password;
                    confirmPassword = ArtistRegPasswordConfirm.Password;
                    break;
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка");
                return;
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@(gmail\.com|yandex\.ru|yandex\.com|ya\.ru|mail\.ru)$"))
            {
                MessageBox.Show("Введите корректный Email. Допускаются только почтовые адреса сервисов Google, Яндекс и Mail.ru (например, name@gmail.com, name@yandex.ru, name@mail.ru)", "Ошибка");
                return;
            }

            if (!Regex.IsMatch(nickname, @"^[a-zA-Z0-9_]{3,20}$"))
            {
                MessageBox.Show("Никнейм должен содержать от 3 до 20 символов (только латинские буквы, цифры и знак '_').", "Ошибка");
                return;
            }

            if (!Regex.IsMatch(password, @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$"))
            {
                MessageBox.Show("Пароль должен содержать минимум 8 символов, хотя бы одну букву и одну цифру, и не должен содержать пробелов.", "Ошибка");
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают!", "Ошибка");
                return;
            }

            // Если это исполнитель — отправляем заявку админу
            if (selectedRoleId == 2)
            {
                RegisterArtistRequest(nickname, email, password);
                return;
            }

            if (UserService.GetUserByEmail(email) != null)
            {
                MessageBox.Show("Пользователь с такой почтой уже существует", "Ошибка");
                return;
            }

            if (UserService.GetUserByNickname(nickname) != null)
            {
                MessageBox.Show("Этот никнейм уже занят", "Ошибка");
                return;
            }

            try
            {
                string exePath = AppDomain.CurrentDomain.BaseDirectory;

                string avatarFileName = "default_avatar.jpg";
                string fullPath = Path.Combine(exePath, "Images", avatarFileName);
                User newUser = new User
                {
                    Email = email,
                    Nickname = nickname,
                    ProfilePhotoPath = fullPath,
                    PasswordHash = PasswordHelper.HashPassword(password),
                    RoleId = UiRoleToDbRoleId(selectedRoleId)
                };

                Session.UserId = UserService.AddUser(newUser);
                Session.UserName = nickname;
                Session.Email = email;
                Session.Password = password;
                Session.UserRole = NormalizeRoleName(selectedRoleId);
                Session.HidedPassword = new string('●', Session.Password.Length);
                Session.AvatarPath = fullPath;
                Session.LoadFromDatabase();

                MainWindow profile_window = new MainWindow();
                Application.Current.MainWindow = profile_window;
                profile_window.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }

        // Вход в аккаунт
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = "";
            string password = "";

            // Определяем, из каких полей брать данные в зависимости от текущей роли
            switch (selectedRoleId)
            {
                case 1:
                    login = UserLoginValue.Text;
                    password = UserPasswordValue.Password;
                    break;
                case 2:
                    login = ArtistLoginValue.Text;
                    password = ArtistPasswordValue.Password;
                    break;
                case 3:
                    login = AdminLoginValue.Text;
                    password = AdminPasswordValue.Password;
                    break;
            }
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            User checking_user = login.Contains('@') ? UserService.GetUserByEmail(login) : UserService.GetUserByNickname(login);
            if (checking_user == null)
            {
                MessageBox.Show("Пользователь с таким логином не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool verified = PasswordHelper.VerifyPassword(password, checking_user.PasswordHash);
            if (!verified && checking_user.PasswordHash == password)
            {
                // Восстановление аккаунтов, где хеш был поврежден и записан как plain-text
                checking_user.PasswordHash = PasswordHelper.HashPassword(password);
                UserService.UpdateUser(checking_user, checking_user);
                verified = true;
            }

            if (verified)
            {
                if (checking_user.RoleId != UiRoleToDbRoleId(selectedRoleId))
                {
                    MessageBox.Show("Выбранная роль не соответствует вашему аккаунту", "Доступ запрещен");
                    return;
                }

                Session.UserId = checking_user.UserId;
                Session.UserName = checking_user.Nickname;
                Session.Email = checking_user.Email;
                Session.Password = password;
                Session.UserRole = NormalizeRoleName(selectedRoleId);
                Session.HidedPassword = new string('●', Session.Password.Length);
                Session.AvatarPath = checking_user.ProfilePhotoPath;
                Session.LoadFromDatabase();

                if (selectedRoleId == 3)
                {
                    var adminWindow = new AdminPanel();
                    Application.Current.MainWindow = adminWindow;
                    adminWindow.Show();
                }
                else
                {
                    MainWindow profile_window = new MainWindow();
                    Application.Current.MainWindow = profile_window;
                    profile_window.Show();
                }
                this.Close();
            }
            else
            {
                MessageBox.Show("Пожалуйста, проверьте пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        private void RegisterArtistRequest(string nickname, string email, string password)
        {
            try
            {
                if (UserService.GetUserByEmail(email) != null || UserService.GetUserByNickname(nickname) != null)
                {
                    MessageBox.Show("Пользователь с таким email или никнеймом уже существует.",
                                     "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (ArtistRequestService.HasActiveRequest(email, nickname))
                {
                    MessageBox.Show("Заявка с таким email или никнеймом уже отправлена или одобрена.",
                                     "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ArtistRequestService.CreateRequest(
                    nickname, email, PasswordHelper.HashPassword(password));

                MessageBox.Show(
                    $"Заявка на регистрацию исполнителя \u00AB{nickname}\u00BB отправлена администратору.\n"
                    + "Как только заявка будет одобрена, вы сможете войти в свой аккаунт.",
                    "Заявка отправлена", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        // Изменение роли
        private void RoleSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoleSelector == null || PanelUser == null)
            {
                return;
            }

            selectedRoleId = RoleSelector.SelectedIndex + 1;

            UpdateUI();
        }

        // Изменение режима
        private void ModeBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            isLoginMode = (btn.Name == "LoginModeBtn");

            UpdateUI();
        }

        // Обновление пользовательского интерфейса
        private void UpdateUI()
        {
            if (LoginModeBtn == null || RegModeBtn == null) return;

            // Стили кнопок режима
            LoginModeBtn.Style = (Style)FindResource(isLoginMode ? "ModeBtnActive" : "ModeBtn");
            RegModeBtn.Style = (Style)FindResource(!isLoginMode ? "ModeBtnActive" : "ModeBtn");

            // Логика для Админа
            if (selectedRoleId == 3)
            {
                isLoginMode = true;
                RegModeBtn.Style = (Style)FindResource("ModeBtnActive");
                RegModeBtn.Visibility = Visibility.Collapsed;
                Grid.SetColumnSpan(LoginModeBtn, 2);
            }
            else
            {
                RegModeBtn.Visibility = Visibility.Visible;
                Grid.SetColumnSpan(LoginModeBtn, 1);
            }

            // Скрываем все
            PanelUser.Visibility = Visibility.Collapsed;
            PanelArtist.Visibility = Visibility.Collapsed;
            PanelAdmin.Visibility = Visibility.Collapsed;

            UserLoginForm.Visibility = Visibility.Collapsed;
            UserRegForm.Visibility = Visibility.Collapsed;
            ArtistLoginForm.Visibility = Visibility.Collapsed;
            ArtistRegForm.Visibility = Visibility.Collapsed;

            // Показываем соответствующее
            switch (selectedRoleId)
            {
                // Пользователь
                case 1:
                    PanelUser.Visibility = Visibility.Visible;
                    if (isLoginMode) UserLoginForm.Visibility = Visibility.Visible;
                    else UserRegForm.Visibility = Visibility.Visible;
                    break;

                // Исполнитель
                case 2:
                    PanelArtist.Visibility = Visibility.Visible;
                    if (isLoginMode) ArtistLoginForm.Visibility = Visibility.Visible;
                    else ArtistRegForm.Visibility = Visibility.Visible;
                    break;

                // Админ
                case 3:
                    PanelAdmin.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
