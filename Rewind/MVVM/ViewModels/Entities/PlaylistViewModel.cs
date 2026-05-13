using Rewind.Helpers;
using Rewind.MVVM.Core;
using Rewind.MVVM.Services;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Entities
{
    /// <summary>
    /// ViewModel-обёртка над сущностью Playlist.
    /// Показывает количество треков, лайков, прослушиваний и команды
    /// «открыть плейлист» / «сохранить»/«удалить сохранение».
    /// </summary>
    public class PlaylistViewModel : ObservableObject
    {
        public Playlist Model { get; }

        public PlaylistViewModel(Playlist playlist, bool isOwned = true, bool? isSaved = null)
        {
            Model = playlist ?? throw new ArgumentNullException(nameof(playlist));
            _isOwned = isOwned;
            // Если IsSaved не передан явно — спрашиваем сервис (одиночный запрос).
            // Чужие плейлисты по умолчанию НЕ считаются сохранёнными — иначе подпись/фильтр врут.
            _isSaved = isSaved ?? (!isOwned && SafeIsSaved());

            OpenCommand = new RelayCommand(Open);

            Session.PlaylistChanged += OnPlaylistChanged;
            PlaylistListenService.OnPlaylistListenChanged += OnPlaylistChanged;
            SavedPlaylistService.OnPlaylistSavedChanged += OnSavedChanged;
        }

        private bool SafeIsSaved()
        {
            try { return SavedPlaylistService.IsSaved(Session.UserId, Model.PlaylistID); }
            catch { return false; }
        }

        private void OnPlaylistChanged(int playlistId)
        {
            if (playlistId != PlaylistId) return;
            OnPropertyChanged(nameof(TrackCount));
            OnPropertyChanged(nameof(LikesCount));
            OnPropertyChanged(nameof(ListensCount));
            OnPropertyChanged(nameof(Subtitle));
        }

        private void OnSavedChanged(int playlistId)
        {
            if (playlistId != PlaylistId) return;
            if (!IsOwned) IsSaved = SafeIsSaved();
            OnPropertyChanged(nameof(LikesCount));
            OnPropertyChanged(nameof(Subtitle));
        }

        public int PlaylistId => Model.PlaylistID;
        public string Title => Model.Title;
        public string? CoverPath => Model.CoverPath;
        public bool IsPrivate => Model.IsPrivate;
        public int OwnerId => Model.OwnerID;

        public int TrackCount => Model.PlaylistTracks?.Count ?? 0;

        public int LikesCount
        {
            get { try { return SavedPlaylistService.GetSavedCount(PlaylistId); } catch { return 0; } }
        }

        public int ListensCount
        {
            get { try { return PlaylistListenService.GetListenerCount(PlaylistId); } catch { return 0; } }
        }

        private bool _isOwned;
        public bool IsOwned
        {
            get => _isOwned;
            set => SetProperty(ref _isOwned, value);
        }

        private bool _isSaved;
        public bool IsSaved
        {
            get => _isSaved;
            set { if (SetProperty(ref _isSaved, value)) OnPropertyChanged(nameof(Subtitle)); }
        }

        public string Subtitle => IsOwned
            ? $"{TrackCount} тр.  •  ♥ {LikesCount}  •  ► {ListensCount}"
            : (IsSaved
                ? $"{TrackCount} тр.  •  сохранён  •  ► {ListensCount}"
                : $"{TrackCount} тр.  •  ♥ {LikesCount}  •  ► {ListensCount}");

        public ICommand OpenCommand { get; }

        private void Open()
        {
            var nav = ServiceLocator.TryResolve<INavigationService>();
            nav?.OpenPlaylistDetails(Model);
        }
    }
}
