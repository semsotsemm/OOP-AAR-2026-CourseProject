using Rewind.Helpers;
using Rewind.MVVM.Core;
using Rewind.MVVM.Services;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Entities
{
    /// <summary>ViewModel-обёртка для Album (карточки на главной / профиле).</summary>
    public class AlbumViewModel : ObservableObject
    {
        public Album Model { get; }

        public AlbumViewModel(Album album)
        {
            Model = album ?? throw new ArgumentNullException(nameof(album));
            OpenCommand = new RelayCommand(Open);
        }

        public int AlbumId => Model.AlbumId;
        public string Title => Model.Title;
        public string? CoverPath => Model.CoverPath;
        public int TrackCount => Model.AlbumTracks?.Count ?? 0;

        public string ArtistName =>
            Model.Artist?.Nickname
            ?? UserService.GetUserById(Model.ArtistId)?.Nickname
            ?? "Исполнитель";

        public string Subtitle => $"{ArtistName} • {TrackCount} треков";

        public ICommand OpenCommand { get; }

        private void Open()
        {
            var nav = ServiceLocator.TryResolve<INavigationService>();
            // Плотная сущность для страницы — с треками и навигацией; для списка достаточно model
            var full = AlbumService.GetById(AlbumId) ?? Model;
            nav?.OpenAlbumDetails(full);
        }
    }
}
