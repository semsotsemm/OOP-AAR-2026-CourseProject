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

        public PlaylistViewModel(Playlist playlist, bool isOwned = true)
        {
            Model = playlist ?? throw new ArgumentNullException(nameof(playlist));
            _isOwned = isOwned;

            OpenCommand = new RelayCommand(Open);

            Session.PlaylistChanged += OnPlaylistChanged;
            PlaylistListenService.OnPlaylistListenChanged += _ => OnPlaylistChanged(_);
            SavedPlaylistService.OnPlaylistSavedChanged += _ => OnPlaylistChanged(_);
        }

        private void OnPlaylistChanged(int playlistId)
        {
            if (playlistId != PlaylistId) return;
            OnPropertyChanged(nameof(TrackCount));
            OnPropertyChanged(nameof(LikesCount));
            OnPropertyChanged(nameof(ListensCount));
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

        public string Subtitle => IsOwned
            ? $"{TrackCount} тр.  •  ♥ {LikesCount}  •  ► {ListensCount}"
            : $"{TrackCount} тр.  •  сохранён  •  ► {ListensCount}";

        public ICommand OpenCommand { get; }

        private void Open()
        {
            var nav = ServiceLocator.TryResolve<INavigationService>();
            nav?.OpenPlaylistDetails(Model);
        }
    }
}
