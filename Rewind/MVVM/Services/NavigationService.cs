using Rewind.Helpers;
using System.Windows;

namespace Rewind.MVVM.Services
{
    public sealed class NavigationService : INavigationService
    {
        private MainWindow? MW => Application.Current?.MainWindow as MainWindow;

        public void ShowMain() => MW?.ShowMainPage();
        public void ShowPlaylists() => MW?.ShowPlaylistsPage();
        public void ShowFavorites() => MW?.ShowFavoritesPage();
        public void ShowSearch() => MW?.ShowSearchPage();
        public void ShowProfile() => MW?.ShowProfilePage();
        public void ShowArtistStudio() => MW?.ShowArtistStudioPage();

        public void OpenPlaylistDetails(Playlist playlist) => MW?.OpenPlaylistDetails(playlist);
        public void OpenAlbumDetails(Album album) => MW?.OpenAlbumDetails(album);
        public void OpenArtistProfile(int artistId) => MW?.OpenArtistProfile(artistId);
    }
}
