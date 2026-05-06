using Rewind.Helpers;
using System.Windows;

namespace Rewind.MVVM.Services
{
    /// <summary>
    /// Реализация навигации поверх уже существующего MainWindow.
    /// Хранит лениво добываемую ссылку на активное главное окно
    /// (берётся через Application.Current.MainWindow, что стабильно
    /// работает при открытии/закрытии Registration → MainWindow).
    /// </summary>
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
