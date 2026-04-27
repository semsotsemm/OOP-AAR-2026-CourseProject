using System.Windows;
using Rewind.DbManager;
using System.Windows.Controls;

namespace Rewind.Pages
{
    public partial class Registration : Window
    {
        // 1 = Пользователь, 2 = Исполнитель, 3 = Админ
        private int selectedRoleId = 1;

        // true = Вход, false = Регистрация
        private bool isLoginMode = true;

        public Registration()
        {
            InitializeComponent();
            UpdateUI();
        }

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

            if (!email.Contains('@'))
            {
                MessageBox.Show("Введите корректный Email", "Ошибка");
                return;
            }

            if (nickname.Contains('@'))
            {
                MessageBox.Show("Никнейм не может содержать символ '@'", "Ошибка");
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают!", "Ошибка");
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
                User newUser = new User
                {
                    Email = email,
                    Nickname = nickname,
                    PasswordHash = PasswordHelper.HashPassword(password),
                    RoleId = selectedRoleId
                };

                Session.UserId = UserService.AddUser(newUser);
                Session.UserName = nickname;
                Session.Email = email;
                Session.Password = password;
                Session.UserRole = selectedRoleId == 1 ? "Слушатель" : "Исполнитель";
                Session.HidedPassword = new string('●', Session.Password.Length);

                MainWindow profile_window = new MainWindow();
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

            if (PasswordHelper.VerifyPassword(password, checking_user.PasswordHash))
            {
                if (checking_user.RoleId != selectedRoleId)
                {
                    MessageBox.Show("Выбранная роль не соответствует вашему аккаунту", "Доступ запрещен");
                    return;
                }

                // Получаем текущего пользователя
                UserStatisticsDto current_user = new UserStatisticsDto();
                UserStatisticsDto.GetUserStats(checking_user.UserId);

                Session.UserId = checking_user.UserId;
                Session.UserName = checking_user.Nickname;
                Session.Email = checking_user.Email;
                Session.Password = password;
                Session.UserRole = selectedRoleId == 1 ? "Слушатель" : "Исполнитель";
                Session.TracksListened = current_user.TotalTracksListened;
                Session.Subscriptions = current_user.SubscriptionsCount;
                Session.Liked = current_user.FavoritesCount;
                Session.Listened = current_user.TotalTimeFormatted;
                Session.Playlists = current_user.PlaylistsCount;
                Session.HidedPassword = new string('●', Session.Password.Length);


                MainWindow profile_window = new MainWindow();
                profile_window.Show();
                this.Close();
            }
            else 
            {
                MessageBox.Show("Пожалуйста, проверьте пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
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