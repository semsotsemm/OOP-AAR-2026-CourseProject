using Rewind.Helpers;
using Rewind.MVVM.Core;
using Rewind.MVVM.Services;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Entities
{
    /// <summary>ViewModel-обёртка для User (карточки исполнителей).</summary>
    public class UserViewModel : ObservableObject
    {
        public User Model { get; }

        public UserViewModel(User user)
        {
            Model = user ?? throw new ArgumentNullException(nameof(user));
            OpenProfileCommand = new RelayCommand(OpenProfile);
        }

        public int UserId => Model.UserId;
        public string Nickname => Model.Nickname;
        public string? AvatarPath => Model.ProfilePhotoPath;
        public string? Status => Model.Status;
        public string RoleName => Model.Role?.RoleName ?? "";

        public ICommand OpenProfileCommand { get; }

        private void OpenProfile()
        {
            var nav = ServiceLocator.TryResolve<INavigationService>();
            nav?.OpenArtistProfile(UserId);
        }
    }
}
