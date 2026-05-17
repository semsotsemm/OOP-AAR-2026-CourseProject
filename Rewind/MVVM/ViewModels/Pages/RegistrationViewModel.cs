using Rewind.Helpers;
using Rewind.MVVM.Core;
using Rewind.MVVM.Services;
using Rewind.Pages;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Pages
{
    public sealed class RegistrationViewModel : ViewModelBase
    {
        private readonly IDialogService _dialog;

        public RegistrationViewModel(IDialogService dialog)
        {
            _dialog = dialog;
            RegisterCommand = new RelayCommand(Register);
            LoginCommand = new RelayCommand(DoLogin);
            SwitchToLoginCommand = new RelayCommand(() => IsLoginMode = true);
            SwitchToRegisterCommand = new RelayCommand(() => IsLoginMode = false);
        }


        private bool _isLoginMode = true;
        public bool IsLoginMode
        {
            get => _isLoginMode;
            set => SetProperty(ref _isLoginMode, value);
        }

        private int _selectedRoleId = 1;
        public int SelectedRoleId
        {
            get => _selectedRoleId;
            set
            {
                if (SetProperty(ref _selectedRoleId, value) && value == 3)
                    IsLoginMode = true;
            }
        }

        public string Login { get; set; } = "";
        public string Password { get; set; } = "";
        public string Email { get; set; } = "";
        public string Nickname { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";

        public ICommand RegisterCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand SwitchToLoginCommand { get; }
        public ICommand SwitchToRegisterCommand { get; }

        private static string NormalizeRoleName(int roleId) => roleId switch
        {
            1 => "Слушатель",
            2 => "Исполнитель",
            3 => "Администратор",
            _ => "Слушатель"
        };

        private static int UiRoleToDbRoleId(int uiRoleId) => uiRoleId switch
        {
            1 => 3, 
            2 => 2, 
            3 => 1, 
            _ => 3
        };

        private void Register()
        {
            string email = Email.Trim();
            string nickname = Nickname.Trim();
            string password = Password;
            string confirm = ConfirmPassword;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(password))
            { _dialog.Error("Заполните все поля!"); return; }

            if (!Regex.IsMatch(email, @"^[^@\s]+@(gmail\.com|yandex\.ru|yandex\.com|ya\.ru|mail\.ru)$"))
            { _dialog.Error("Введите корректный Email. Допускаются только почтовые адреса сервисов Google, Яндекс и Mail.ru (например, name@gmail.com, name@yandex.ru, name@mail.ru)"); return; }

            if (!Regex.IsMatch(nickname, @"^[a-zA-Z0-9_]{3,20}$"))
            { _dialog.Error("Никнейм должен содержать от 3 до 20 символов (только латинские буквы, цифры и знак '_')."); return; }

            if (!Regex.IsMatch(password, @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$"))
            { _dialog.Error("Пароль должен содержать минимум 8 символов, хотя бы одну букву и одну цифру, и не должен содержать пробелов."); return; }

            if (password != confirm)
            { _dialog.Error("Пароли не совпадают!"); return; }

            if (SelectedRoleId == 2)
            { RegisterArtistRequest(nickname, email, password); return; }

            if (UserService.GetUserByEmail(email) != null)
            { _dialog.Error("Пользователь с такой почтой уже существует"); return; }

            if (UserService.GetUserByNickname(nickname) != null)
            { _dialog.Error("Этот никнейм уже занят"); return; }

            try
            {
                var newUser = new User
                {
                    Email = email,
                    Nickname = nickname,
                    ProfilePhotoPath = FileStorage.DefaultAvatar,
                    PasswordHash = PasswordHelper.HashPassword(password),
                    RoleId = UiRoleToDbRoleId(SelectedRoleId)
                };

                Session.UserId = UserService.AddUser(newUser);
                Session.UserName = nickname;
                Session.Email = email;
                Session.Password = password;
                Session.UserRole = NormalizeRoleName(SelectedRoleId);
                Session.HidedPassword = new string('●', Session.Password.Length);
                Session.AvatarPath = FileStorage.DefaultAvatar;
                Session.LoadFromDatabase();

                OpenMainWindowAndClose();
            }
            catch (Exception ex)
            {
                _dialog.Error($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void DoLogin()
        {
            try
            {
                string login = Login.Trim();
                string password = Password;

                if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                { _dialog.Error("Пожалуйста, заполните все поля"); return; }

                var user = login.Contains('@')
                    ? UserService.GetUserByEmail(login)
                    : UserService.GetUserByNickname(login);

                if (user == null)
                { _dialog.Error("Пользователь с таким логином не найден"); return; }

                bool verified;
                try
                {
                    verified = PasswordHelper.VerifyPassword(password, user.PasswordHash);
                }
                catch
                {
                    verified = false;
                }

                if (!verified && user.PasswordHash == password)
                {
                    user.PasswordHash = PasswordHelper.HashPassword(password);
                    UserService.UpdateUser(user, user);
                    verified = true;
                }

                if (!verified)
                { _dialog.Error("Пожалуйста, проверьте пароль"); return; }

                if (user.RoleId != UiRoleToDbRoleId(SelectedRoleId))
                { _dialog.Error("Выбранная роль не соответствует вашему аккаунту", "Доступ запрещен"); return; }

                Session.UserId = user.UserId;
                Session.UserName = user.Nickname;
                Session.Email = user.Email;
                Session.Password = password;
                Session.UserRole = NormalizeRoleName(SelectedRoleId);
                Session.HidedPassword = new string('●', Session.Password.Length);
                Session.AvatarPath = string.IsNullOrWhiteSpace(user.ProfilePhotoPath)
                    ? FileStorage.DefaultAvatar
                    : user.ProfilePhotoPath;
                Session.LoadFromDatabase();

                if (SelectedRoleId == 3)
                    OpenAdminPanelAndClose();
                else
                    OpenMainWindowAndClose();
            }
            catch (Exception ex)
            {
                _dialog.Error($"Ошибка входа: {ex.Message}");
            }
        }

        private void RegisterArtistRequest(string nickname, string email, string password)
        {
            try
            {
                if (UserService.GetUserByEmail(email) != null || UserService.GetUserByNickname(nickname) != null)
                { _dialog.Error("Пользователь с таким email или никнеймом уже существует."); return; }

                if (ArtistRequestService.HasActiveRequest(email, nickname))
                { _dialog.Error("Заявка с таким email или никнеймом уже отправлена или одобрена."); return; }

                ArtistRequestService.CreateRequest(nickname, email, PasswordHelper.HashPassword(password));

                _dialog.Info(
                    $"Заявка на регистрацию исполнителя \u00AB{nickname}\u00BB отправлена администратору.\n"
                    + "Как только заявка будет одобрена, вы сможете войти в свой аккаунт.",
                    "Заявка отправлена");
            }
            catch (Exception ex)
            {
                _dialog.Error($"Ошибка: {ex.Message}");
            }
        }

        private static void OpenMainWindowAndClose()
        {
            var mw = new MainWindow();
            var old = Application.Current.MainWindow;
            Application.Current.MainWindow = mw;
            mw.Show();
            old?.Close();
        }

        private static void OpenAdminPanelAndClose()
        {
            var aw = new AdminPanel();
            var old = Application.Current.MainWindow;
            Application.Current.MainWindow = aw;
            aw.Show();
            old?.Close();
        }
    }
}
