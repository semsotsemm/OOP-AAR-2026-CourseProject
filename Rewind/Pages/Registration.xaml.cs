using System.Windows;
using System.Windows.Controls;
using Rewind.MVVM.Services;
using Rewind.MVVM.ViewModels.Pages;

namespace Rewind.Pages
{
    /// <summary>
    /// Тонкий View-слой поверх <see cref="RegistrationViewModel"/>.
    /// Вся бизнес-логика (валидация, регистрация, вход) живёт в VM.
    /// Code-behind нужен только чтобы:
    ///   1) Прокидывать значение PasswordBox.Password в VM (т.к. SecureString
    ///      нельзя биндить напрямую по соображениям безопасности);
    ///   2) Обновлять видимость панелей ролей — чисто визуальная логика.
    /// </summary>
    public partial class Registration : Window
    {
        private readonly RegistrationViewModel _vm;

        public Registration()
        {
            InitializeComponent();
            _vm = new RegistrationViewModel(ServiceLocator.Resolve<IDialogService>());
            DataContext = _vm;
            _vm.PropertyChanged += (_, _) => UpdateUI();
            UpdateUI();
        }

        // ─── Команды: собираем текст из TextBox/PasswordBox и зовём VM ───

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_vm.SelectedRoleId)
            {
                case 1:
                    _vm.Email = UserRegEmail.Text;
                    _vm.Nickname = UserRegNickname.Text;
                    _vm.Password = UserRegPassword.Password;
                    _vm.ConfirmPassword = UserRegPasswordConfirm.Password;
                    break;
                case 2:
                    _vm.Email = ArtistRegEmail.Text;
                    _vm.Nickname = ArtistRegNickname.Text;
                    _vm.Password = ArtistRegPassword.Password;
                    _vm.ConfirmPassword = ArtistRegPasswordConfirm.Password;
                    break;
            }
            _vm.RegisterCommand.Execute(null);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_vm.SelectedRoleId)
            {
                case 1:
                    _vm.Login = UserLoginValue.Text;
                    _vm.Password = UserPasswordValue.Password;
                    break;
                case 2:
                    _vm.Login = ArtistLoginValue.Text;
                    _vm.Password = ArtistPasswordValue.Password;
                    break;
                case 3:
                    _vm.Login = AdminLoginValue.Text;
                    _vm.Password = AdminPasswordValue.Password;
                    break;
            }
            _vm.LoginCommand.Execute(null);
        }

        // ─── Переключение режима/роли ───

        private void RoleSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoleSelector == null || PanelUser == null) return;
            _vm.SelectedRoleId = RoleSelector.SelectedIndex + 1;
        }

        private void ModeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Name == "LoginModeBtn") _vm.SwitchToLoginCommand.Execute(null);
            else _vm.SwitchToRegisterCommand.Execute(null);
        }

        /// <summary>Чисто визуальная реакция на IsLoginMode / SelectedRoleId из VM.</summary>
        private void UpdateUI()
        {
            if (LoginModeBtn == null || RegModeBtn == null) return;

            LoginModeBtn.Style = (Style)FindResource(_vm.IsLoginMode ? "ModeBtnActive" : "ModeBtn");
            RegModeBtn.Style = (Style)FindResource(!_vm.IsLoginMode ? "ModeBtnActive" : "ModeBtn");

            if (_vm.SelectedRoleId == 3)
            {
                RegModeBtn.Style = (Style)FindResource("ModeBtnActive");
                RegModeBtn.Visibility = Visibility.Collapsed;
                Grid.SetColumnSpan(LoginModeBtn, 2);
            }
            else
            {
                RegModeBtn.Visibility = Visibility.Visible;
                Grid.SetColumnSpan(LoginModeBtn, 1);
            }

            PanelUser.Visibility = Visibility.Collapsed;
            PanelArtist.Visibility = Visibility.Collapsed;
            PanelAdmin.Visibility = Visibility.Collapsed;
            UserLoginForm.Visibility = Visibility.Collapsed;
            UserRegForm.Visibility = Visibility.Collapsed;
            ArtistLoginForm.Visibility = Visibility.Collapsed;
            ArtistRegForm.Visibility = Visibility.Collapsed;

            switch (_vm.SelectedRoleId)
            {
                case 1:
                    PanelUser.Visibility = Visibility.Visible;
                    (_vm.IsLoginMode ? UserLoginForm : (FrameworkElement)UserRegForm).Visibility = Visibility.Visible;
                    break;
                case 2:
                    PanelArtist.Visibility = Visibility.Visible;
                    (_vm.IsLoginMode ? ArtistLoginForm : (FrameworkElement)ArtistRegForm).Visibility = Visibility.Visible;
                    break;
                case 3:
                    PanelAdmin.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
