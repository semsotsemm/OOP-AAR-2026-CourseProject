using Rewind.Contols;

namespace Rewind.MVVM.Services
{
    /// <summary>
    /// Абстракция над плеером. Скрывает от VM/контролов прямые вызовы
    /// MainWindow.PlayTrackFromContext / CurrentTrack / TogglePlayPause и т.п.
    /// Даёт возможность подменить реализацию (например, тестовую).
    /// </summary>
    public interface IPlayerService
    {
        TrackItem? CurrentTrack { get; }
        bool IsPlaying { get; }

        void PlayFromContext(TrackItem item, IReadOnlyList<TrackItem> context);
        void TogglePlayPause();
        void OpenNowPlaying(string sourcePage);
    }
}
