using Rewind.Helpers;
using Rewind.MVVM.Core;
using Rewind.MVVM.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Pages
{
    /// <summary>
    /// VM страницы исполнителя.
    /// Управляет данными профиля, подпиской и переключением «показать все треки».
    /// </summary>
    public sealed class ArtistProfileViewModel : ViewModelBase
    {
        private readonly INavigationService _nav;
        private readonly IDialogService _dialog;
        private readonly int _artistId;

        public ArtistProfileViewModel(int artistId, INavigationService nav, IDialogService dialog)
        {
            _nav = nav;
            _dialog = dialog;
            _artistId = artistId;

            PublishedTracks = new ObservableCollection<Track>();
            Albums = new ObservableCollection<Album>();

            ToggleSubscribeCommand = new RelayCommand(ToggleSubscribe);
            ToggleShowAllCommand = new RelayCommand(() =>
            {
                ShowAllTracks = !ShowAllTracks;
                OnPropertyChanged(nameof(ShowAllLabel));
            });
            BackCommand = new RelayCommand(() => _nav.ShowMain());

            Load();
        }

        public User? Artist { get; private set; }
        public int ArtistId => _artistId;

        public string ArtistName => Artist?.Nickname ?? "";
        public string? AvatarPath => Artist?.ProfilePhotoPath;

        public ObservableCollection<Track> PublishedTracks { get; }
        public ObservableCollection<Album> Albums { get; }

        public int FollowerCount { get; private set; }
        public string StatsText => $"{PublishedTracks.Count} треков  •  {FollowerCount} подписчиков";

        public bool IsSelf => _artistId == Session.UserId;

        private bool _isFollowing;
        public bool IsFollowing
        {
            get => _isFollowing;
            private set
            {
                if (SetProperty(ref _isFollowing, value)) OnPropertyChanged(nameof(SubscribeButtonText));
            }
        }
        public string SubscribeButtonText => IsFollowing ? "✓ Вы подписаны" : "+ Подписаться";

        private bool _showAllTracks;
        public bool ShowAllTracks { get => _showAllTracks; private set => SetProperty(ref _showAllTracks, value); }
        public string ShowAllLabel => _showAllTracks ? "↑ Скрыть" : "Все треки →";

        public ICommand ToggleSubscribeCommand { get; }
        public ICommand ToggleShowAllCommand { get; }
        public ICommand BackCommand { get; }

        public event Action? Loaded;

        private void Load()
        {
            try { Artist = UserService.GetUserById(_artistId); } catch { }
            if (Artist == null) return;

            try
            {
                PublishedTracks.Clear();
                foreach (var t in TrackService.GetByArtistAll(_artistId)
                    .Where(t => t.PublishStatus == "Published")
                    .OrderByDescending(t => t.Statistics?.PlayCount ?? 0))
                    PublishedTracks.Add(t);
            }
            catch { }

            try { FollowerCount = SubscriptionService.GetFollowers(_artistId).Count; } catch { }
            try { IsFollowing = SubscriptionService.IsFollowing(Session.UserId, _artistId); } catch { }

            try
            {
                Albums.Clear();
                foreach (var a in AlbumService.GetByArtist(_artistId)) Albums.Add(a);
            }
            catch { }

            OnPropertyChanged(nameof(ArtistName));
            OnPropertyChanged(nameof(StatsText));
            OnPropertyChanged(nameof(IsSelf));
            Loaded?.Invoke();
        }

        private void ToggleSubscribe()
        {
            if (IsSelf || Artist == null) return;

            try
            {
                if (IsFollowing)
                {
                    SubscriptionService.Unsubscribe(Session.UserId, _artistId);
                    IsFollowing = false;
                }
                else
                {
                    SubscriptionService.Subscribe(Session.UserId, _artistId);
                    IsFollowing = true;
                    if (Session.NotifNewTracksEnabled && !Session.NotifPushEnabled)
                        Session.NotificationCount++;
                }
                try { FollowerCount = SubscriptionService.GetFollowers(_artistId).Count; } catch { }
                OnPropertyChanged(nameof(StatsText));
            }
            catch (Exception ex) { _dialog.Error(ex.Message); }
        }

        public void OpenAlbum(Album album)
        {
            var full = AlbumService.GetById(album.AlbumId) ?? album;
            _nav.OpenAlbumDetails(full);
        }
    }
}
