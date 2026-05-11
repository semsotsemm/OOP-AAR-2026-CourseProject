using Rewind.Helpers;
using Rewind.MVVM.Core;
using Rewind.MVVM.Services;
using System.IO;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Pages
{
    public sealed class ArtistStudioViewModel : ViewModelBase
    {
        private readonly IDialogService _dialog;

        public ArtistStudioViewModel(IDialogService dialog)
        {
            _dialog = dialog;
            UploadTrackCommand = new RelayCommand(UploadTrack);
            CreateAlbumCommand = new RelayCommand(CreateAlbum);
            SelectTabCommand = new RelayCommand<string>(name => { if (name != null) ActiveTab = name; });
        }


        private string _activeTab = "upload";
        public string ActiveTab
        {
            get => _activeTab;
            set { if (SetProperty(ref _activeTab, value)) TabChanged?.Invoke(); }
        }
        public event Action? TabChanged;


        public string NewTrackTitle { get; set; } = "";
        public string NewTrackGenre { get; set; } = "";
        public string? NewTrackCoverPath { get; set; }
        public string? NewTrackAudioPath { get; set; }
        public event Action? UploadFinished;

        public ICommand UploadTrackCommand { get; }
        public ICommand CreateAlbumCommand { get; }
        public ICommand SelectTabCommand { get; }


        public string AlbumName { get; set; } = "";
        public string AlbumGenre { get; set; } = "";
        public string? AlbumCoverPath { get; set; }
        public List<int> AlbumSelectedTrackIds { get; set; } = new();
        public int? EditingAlbumId { get; set; }
        public event Action? AlbumSaved;

        private void UploadTrack()
        {
            if (string.IsNullOrWhiteSpace(NewTrackTitle) || string.IsNullOrEmpty(NewTrackAudioPath))
            { _dialog.Error("Введите название и выберите аудиофайл!"); return; }

            try
            {
                string fileName = FileStorage.CopyTrackAudio(NewTrackAudioPath, NewTrackTitle);
                string destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", fileName);
                int duration;
                using (var f = TagLib.File.Create(destPath))
                    duration = (int)f.Properties.Duration.TotalSeconds;

                string? coverPath = !string.IsNullOrWhiteSpace(NewTrackCoverPath)
                    ? FileStorage.CopyTrackCover(NewTrackCoverPath) : null;

                var track = new Track
                {
                    Title = NewTrackTitle,
                    FilePath = fileName,
                    CoverPath = coverPath,
                    Duration = duration,
                    UploadDate = DateTime.UtcNow,
                    ArtistID = Session.UserId,
                    Genre = NewTrackGenre,
                    PublishStatus = "Pending"
                };
                TrackService.AddTrack(track);
                _dialog.Info("Трек отправлен на проверку администратору.", "Заявка отправлена");

                NewTrackTitle = ""; NewTrackAudioPath = null; NewTrackCoverPath = null;
                UploadFinished?.Invoke();
            }
            catch (Exception ex) { _dialog.Error(ex.Message); }
        }

        private void CreateAlbum()
        {
            if (string.IsNullOrWhiteSpace(AlbumName)) { _dialog.Error("Введите название альбома"); return; }
            if (AlbumSelectedTrackIds.Count == 0) { _dialog.Error("Выберите хотя бы один трек"); return; }

            try
            {
                int albumId;
                if (EditingAlbumId.HasValue)
                {
                    AlbumService.Update(EditingAlbumId.Value, AlbumName, AlbumGenre, AlbumCoverPath);
                    albumId = EditingAlbumId.Value;
                }
                else
                {
                    albumId = AlbumService.Create(AlbumName, Session.UserId, AlbumGenre, AlbumCoverPath);
                }
                foreach (var trackId in AlbumSelectedTrackIds)
                    AlbumService.AddTrack(albumId, trackId);

                _dialog.Info(EditingAlbumId.HasValue ? "Альбом обновлён." : "Альбом создан.");

                AlbumName = ""; AlbumGenre = ""; AlbumCoverPath = null;
                AlbumSelectedTrackIds = new List<int>(); EditingAlbumId = null;
                AlbumSaved?.Invoke();
            }
            catch (Exception ex) { _dialog.Error(ex.Message); }
        }
    }
}