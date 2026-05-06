using Rewind.Helpers;

namespace Rewind.MVVM.Services
{
    /// <summary>
    /// Абстракция навигации между страницами главного окна.
    /// Страница/VM работает с этим интерфейсом, не зная про конкретный MainWindow.
    /// </summary>
    public interface INavigationService
    {
        void ShowMain();
        void ShowPlaylists();
        void ShowFavorites();
        void ShowSearch();
        void ShowProfile();
        void ShowArtistStudio();

        void OpenPlaylistDetails(Playlist playlist);
        void OpenAlbumDetails(Album album);
        void OpenArtistProfile(int artistId);
    }
}
