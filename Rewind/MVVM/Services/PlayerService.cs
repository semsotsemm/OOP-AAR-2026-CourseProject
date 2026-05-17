using Rewind.Contols;
using System.Windows;

namespace Rewind.MVVM.Services
{
    public sealed class PlayerService : IPlayerService
    {
        private MainWindow? MW => Application.Current?.MainWindow as MainWindow;

        public TrackItem? CurrentTrack => MW?.CurrentTrack;
        public bool IsPlaying => MW?.IsPlaying ?? false;

        public void PlayFromContext(TrackItem item, IReadOnlyList<TrackItem> context)
            => MW?.PlayTrackFromContext(item, context);

        public void TogglePlayPause() => MW?.TogglePlayPause();

        public void OpenNowPlaying(string sourcePage) => MW?.OpenNowPlaying(sourcePage);
    }
}
