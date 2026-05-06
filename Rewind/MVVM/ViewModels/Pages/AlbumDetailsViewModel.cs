using Rewind.Helpers;
using Rewind.MVVM.Core;
using Rewind.MVVM.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Pages
{
    /// <summary>VM страницы деталей альбома.</summary>
    public sealed class AlbumDetailsViewModel : ViewModelBase
    {
        private readonly Album _album;
        private readonly INavigationService _nav;
        private readonly IDialogService _dialog;

        public AlbumDetailsViewModel(Album album, INavigationService nav, IDialogService dialog)
        {
            _album = album ?? throw new ArgumentNullException(nameof(album));
            _nav = nav;
            _dialog = dialog;

            Tracks = new ObservableCollection<Track>(
                album.AlbumTracks?.Select(at => at.Track).Where(t => t != null)!.ToList() ?? new List<Track>());

            ToggleSaveCommand = new RelayCommand(ToggleSave);
            BackCommand = new RelayCommand(() => _nav.ShowMain());
            RefreshState();
        }

        public Album Album => _album;
        public string Title => _album.Title;
        public string ArtistName => _album.Artist?.Nickname
            ?? UserService.GetUserById(_album.ArtistId)?.Nickname
            ?? "Исполнитель";
        public string? CoverPath => _album.CoverPath;

        public ObservableCollection<Track> Tracks { get; }
        public bool IsEmpty => Tracks.Count == 0;

        private string _statsText = "";
        public string StatsText { get => _statsText; private set => SetProperty(ref _statsText, value); }

        private bool _isSaved;
        public bool IsSaved { get => _isSaved; private set => SetProperty(ref _isSaved, value); }

        public string SaveButtonText => IsSaved ? "✓ Сохранён" : "♥ Сохранить";

        public ICommand ToggleSaveCommand { get; }
        public ICommand BackCommand { get; }

        public void RegisterListenOnce()
        {
            if (_album.AlbumId <= 0) return;
            if (!Session.TryEnterPlaybackScope("album", _album.AlbumId)) return;
            AlbumService.RegisterListen(Session.UserId, _album.AlbumId);
            RefreshState();
        }

        private void ToggleSave()
        {
            bool saved = AlbumService.ToggleSave(Session.UserId, _album.AlbumId);
            RefreshState();
            _dialog.Info(saved ? "Альбом сохранён в профиль." : "Альбом удалён из сохранённых.");
        }

        private void RefreshState()
        {
            try { IsSaved = AlbumService.IsSaved(Session.UserId, _album.AlbumId); } catch { }
            int tracks = _album.AlbumTracks?.Count ?? 0;
            int saves = 0, listens = 0;
            try { saves = AlbumService.GetSavedCount(_album.AlbumId); } catch { }
            try { listens = AlbumService.GetListenCount(_album.AlbumId); } catch { }
            StatsText = $"{tracks} треков  •  ♥ {saves} сохранили  •  ► {listens} прослушали";
            OnPropertyChanged(nameof(SaveButtonText));
        }
    }
}
