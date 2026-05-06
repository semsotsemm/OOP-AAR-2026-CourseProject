using Rewind.Helpers;
using Rewind.MVVM.Core;
using Rewind.MVVM.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Pages
{
    /// <summary>VM деталей плейлиста: сохранение, прослушивание, состав треков.</summary>
    public sealed class PlaylistDetailsViewModel : ViewModelBase, IDisposable
    {
        private readonly Playlist _playlist;
        private readonly INavigationService _nav;
        private readonly IDialogService _dialog;

        public PlaylistDetailsViewModel(Playlist playlist, INavigationService nav, IDialogService dialog)
        {
            _playlist = playlist ?? throw new ArgumentNullException(nameof(playlist));
            _nav = nav;
            _dialog = dialog;

            Tracks = new ObservableCollection<Track>();
            ToggleSaveCommand = new RelayCommand(ToggleSave);
            BackCommand = new RelayCommand(() => _nav.ShowPlaylists());

            Session.PlaylistChanged += OnPlaylistChanged;
            Reload();
        }

        public Playlist Playlist => _playlist;
        public string Title => _playlist.Title;
        public string? CoverPath => _playlist.CoverPath;
        public bool CanSave => _playlist.OwnerID != Session.UserId;
        public bool IsEmpty => Tracks.Count == 0;

        public ObservableCollection<Track> Tracks { get; }
        public string MetaText => $"{Tracks.Count} треков";

        private string _statsText = "";
        public string StatsText { get => _statsText; private set => SetProperty(ref _statsText, value); }

        private bool _isSaved;
        public bool IsSaved { get => _isSaved; private set { if (SetProperty(ref _isSaved, value)) OnPropertyChanged(nameof(SaveButtonText)); } }

        public string SaveButtonText => IsSaved ? "♥ Сохранён" : "♥ Сохранить";

        public ICommand ToggleSaveCommand { get; }
        public ICommand BackCommand { get; }

        public void RegisterListenOnce()
        {
            if (_playlist.PlaylistID <= 0) return;
            if (!Session.TryEnterPlaybackScope("playlist", _playlist.PlaylistID)) return;
            PlaylistListenService.RegisterListen(Session.UserId, _playlist.PlaylistID);
            RefreshStats();
        }

        private void OnPlaylistChanged(int playlistId)
        {
            if (playlistId == _playlist.PlaylistID) Reload();
        }

        public void Reload()
        {
            Tracks.Clear();
            var list = _playlist.PlaylistTracks?
                .Select(pt => pt.Track)
                .Where(t => t != null)!
                .ToList() ?? new List<Track>();
            foreach (var t in list) Tracks.Add(t);

            OnPropertyChanged(nameof(MetaText));
            OnPropertyChanged(nameof(IsEmpty));
            RefreshStats();
            RefreshSavedState();
        }

        private void ToggleSave()
        {
            if (_playlist.PlaylistID <= 0)
            {
                _dialog.Error("Нельзя сохранить неопубликованный плейлист.");
                return;
            }
            bool saved = SavedPlaylistService.Toggle(Session.UserId, _playlist.PlaylistID);
            RefreshSavedState();
            RefreshStats();
            _dialog.Info(saved
                ? "Плейлист сохранён — он появится в вашем профиле."
                : "Плейлист удалён из сохранённых.");
        }

        private void RefreshStats()
        {
            try
            {
                var saved = SavedPlaylistService.GetSavedCount(_playlist.PlaylistID);
                var listens = PlaylistListenService.GetListenerCount(_playlist.PlaylistID);
                StatsText = $"♥ {saved} сохранили  •  ► {listens} прослушали";
            }
            catch { }
        }

        private void RefreshSavedState()
        {
            if (!CanSave) { IsSaved = false; return; }
            try { IsSaved = SavedPlaylistService.IsSaved(Session.UserId, _playlist.PlaylistID); } catch { }
        }

        public void Dispose() => Session.PlaylistChanged -= OnPlaylistChanged;
    }
}
