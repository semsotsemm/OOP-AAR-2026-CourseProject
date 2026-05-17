using Rewind.Contols;

namespace Rewind.MVVM.Services
{
    public interface IPlayerService
    {
        TrackItem? CurrentTrack { get; }
        bool IsPlaying { get; }

        void PlayFromContext(TrackItem item, IReadOnlyList<TrackItem> context);
        void TogglePlayPause();
        void OpenNowPlaying(string sourcePage);
    }
}
