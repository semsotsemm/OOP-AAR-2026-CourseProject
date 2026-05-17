using Rewind.Helpers;

namespace Rewind.MVVM.Services
{
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
