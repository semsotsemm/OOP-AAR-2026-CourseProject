using Rewind.Helpers;
using Rewind.MVVM.Core;
using Rewind.MVVM.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Pages
{
    /// <summary>
    /// VM главной страницы. Держит:
    ///   • приветствие;
    ///   • избранный трек дня (FeaturedTrack) и его статистику воспроизведений;
    ///   • популярные плейлисты;
    ///   • список треков в основном фиде.
    /// События OnFeaturedChanged / OnPlaylistsChanged используются View-слоем,
    /// чтобы перерисовывать TrackItem/AlbumCard контролы (они пока императивные).
    /// </summary>
    public sealed class MainPageViewModel : ViewModelBase, IDisposable
    {
        public MainPageViewModel()
        {
            Tracks = new ObservableCollection<Track>();
            PopularPlaylists = new ObservableCollection<(Playlist Playlist, int Saves, int Listens)>();

            ToggleShowAllCommand = new RelayCommand(() =>
            {
                ShowAllTracks = !ShowAllTracks;
                LoadTracks();
            });

            TrackService.OnPlayCountUpdated += OnPlayCountUpdated;
            PlaylistListenService.OnPlaylistListenChanged += OnPlaylistStatsChanged;
            SavedPlaylistService.OnPlaylistSavedChanged += OnPlaylistStatsChanged;
        }

        public ObservableCollection<Track> Tracks { get; }
        public ObservableCollection<(Playlist Playlist, int Saves, int Listens)> PopularPlaylists { get; }

        public event Action? FeaturedChanged;
        public event Action? TracksChanged;
        public event Action? PlaylistsChanged;

        private Track? _featuredTrack;
        public Track? FeaturedTrack
        {
            get => _featuredTrack;
            private set
            {
                if (SetProperty(ref _featuredTrack, value))
                {
                    OnPropertyChanged(nameof(FeaturedTitle));
                    OnPropertyChanged(nameof(FeaturedArtistInfo));
                    FeaturedChanged?.Invoke();
                }
            }
        }

        public string FeaturedTitle => _featuredTrack?.Title ?? "";
        public string FeaturedArtistInfo
        {
            get
            {
                if (_featuredTrack == null) return "";
                var name = UserService.GetUserById(_featuredTrack.ArtistID)?.Nickname ?? "Неизвестен";
                return $"{name} · {_featuredTrack.Statistics?.PlayCount ?? 0} прослушиваний";
            }
        }

        public string Greeting
        {
            get
            {
                int h = DateTime.Now.Hour;
                string time = h < 6 ? "Доброй ночи" : h < 12 ? "Доброе утро"
                            : h < 18 ? "Добрый день" : "Добрый вечер";
                return $"{time}, {Session.UserName}";
            }
        }

        private bool _showAllTracks;
        public bool ShowAllTracks
        {
            get => _showAllTracks;
            private set
            {
                if (SetProperty(ref _showAllTracks, value)) OnPropertyChanged(nameof(ShowAllLabel));
            }
        }
        public string ShowAllLabel => _showAllTracks ? "Свернуть ↑" : "Все →";

        public ICommand ToggleShowAllCommand { get; }

        public void LoadAll()
        {
            LoadTracks();
            LoadPopularPlaylists();
            LoadFeaturedTrack();
        }

        public void LoadTracks()
        {
            try
            {
                Tracks.Clear();
                var list = TrackService.GetPublishedTracks();
                if (!ShowAllTracks) list = list.Take(7).ToList();
                foreach (var t in list) Tracks.Add(t);
                TracksChanged?.Invoke();
            }
            catch { }
        }

        public void LoadFeaturedTrack()
        {
            try { FeaturedTrack = TrackService.GetMostPopularTrack(); } catch { }
        }

        public void LoadPopularPlaylists()
        {
            PopularPlaylists.Clear();
            List<Playlist> all;
            try
            {
                var pub = PlaylistService.GetPublicPlaylists(0);
                var own = Session.CachedPlaylists.Where(p => !p.IsPrivate && p.PlaylistID > 0);
                all = pub.Concat(own.Where(o => !pub.Any(p => p.PlaylistID == o.PlaylistID)))
                         .Where(p => p.PlaylistID > 0).ToList();
            }
            catch { return; }

            var counts = all.ToDictionary(p => p.PlaylistID, p =>
            {
                int s = 0, l = 0;
                try { s = SavedPlaylistService.GetSavedCount(p.PlaylistID); } catch { }
                try { l = PlaylistListenService.GetListenerCount(p.PlaylistID); } catch { }
                return (s, l);
            });

            foreach (var p in all.OrderByDescending(p => counts[p.PlaylistID].s + counts[p.PlaylistID].l).Take(8))
                PopularPlaylists.Add((p, counts[p.PlaylistID].s, counts[p.PlaylistID].l));

            PlaylistsChanged?.Invoke();
        }

        public void OpenPlaylist(Playlist p)
            => ServiceLocator.TryResolve<INavigationService>()?.OpenPlaylistDetails(p);

        private void OnPlayCountUpdated(int trackId, int newCount)
        {
            if (_featuredTrack?.TrackID != trackId) return;
            if (_featuredTrack.Statistics != null) _featuredTrack.Statistics.PlayCount = newCount;
            OnPropertyChanged(nameof(FeaturedArtistInfo));
            FeaturedChanged?.Invoke();
        }

        private void OnPlaylistStatsChanged(int _) => LoadPopularPlaylists();

        public void Dispose()
        {
            TrackService.OnPlayCountUpdated -= OnPlayCountUpdated;
            PlaylistListenService.OnPlaylistListenChanged -= OnPlaylistStatsChanged;
            SavedPlaylistService.OnPlaylistSavedChanged -= OnPlaylistStatsChanged;
        }
    }
}
