using Rewind.Helpers;
using Rewind.MVVM.Core;
using Rewind.MVVM.Services;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Entities
{
    public class TrackViewModel : ObservableObject
    {
        public Track Model { get; }

        public TrackViewModel(Track track, string? artistName = null)
        {
            Model = track ?? throw new ArgumentNullException(nameof(track));
            _artistName = artistName ?? UserService.GetUserById(track.ArtistID)?.Nickname ?? "Неизвестный";
            _isLiked = Session.IsLiked(track.TrackID);

            ToggleLikeCommand = new RelayCommand(ToggleLike);

            Session.LikeChanged += OnSessionLikeChanged;
        }

        private void OnSessionLikeChanged(int trackId, bool isLiked)
        {
            if (trackId == TrackId) IsLiked = isLiked;
        }

        public int TrackId => Model.TrackID;
        public string Title => Model.Title;
        public string Genre => Model.Genre ?? "";
        public int DurationSeconds => Model.Duration;
        public string? CoverPath => Model.CoverPath;
        public string FilePath => Model.FilePath;

        public string FullAudioPath =>
            System.IO.Path.Combine(Rewind.Helpers.FileStorage.DataRoot, "MusicLibrary", Model.FilePath);

        public string DurationText => $"{DurationSeconds / 60}:{DurationSeconds % 60:D2}";

        private string _artistName;
        public string ArtistName
        {
            get => _artistName;
            set => SetProperty(ref _artistName, value);
        }

        private bool _isLiked;
        public bool IsLiked
        {
            get => _isLiked;
            set => SetProperty(ref _isLiked, value);
        }

        public ICommand ToggleLikeCommand { get; }

        private void ToggleLike()
        {
            IsLiked = Session.ToggleLike(TrackId);
        }
    }
}
