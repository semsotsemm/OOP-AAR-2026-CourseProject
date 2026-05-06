using Rewind.Helpers;
using Rewind.MVVM.Core;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Pages
{
    /// <summary>
    /// VM для страницы «Любимые треки». Держит полный список лайкнутых,
    /// применяет фильтры/сортировку и подписывается на Session.LikeChanged,
    /// чтобы моментально реагировать на лайк/анлайк из любого места приложения.
    /// </summary>
    public sealed class FavoritesPageViewModel : ViewModelBase, IDisposable
    {
        private static readonly string[] SortModes = { "recent", "az", "artist", "duration" };
        private static readonly string[] SortLabels = { "Недавние", "А → Я", "Исполнитель", "Длительность" };

        private readonly List<Track> _allTracks = new();

        public FavoritesPageViewModel()
        {
            Results = new ObservableCollection<Track>();
            AvailableGenres = new ObservableCollection<string>();
            SelectedGenres = new ObservableCollection<string>();

            ToggleSortCommand = new RelayCommand(() =>
            {
                SortIndex = (SortIndex + 1) % SortModes.Length;
            });
            ToggleGenreCommand = new RelayCommand<string>(ToggleGenre);
            ClearGenresCommand = new RelayCommand(() => { SelectedGenres.Clear(); RefreshGenres(); Render(); });

            Session.LikeChanged += OnLikeChanged;

            Load();
        }

        public ObservableCollection<Track> Results { get; }
        public ObservableCollection<string> AvailableGenres { get; }
        public ObservableCollection<string> SelectedGenres { get; }

        public event Action? ResultsChanged;
        public event Action? StatsChanged;

        private string _query = "";
        public string Query
        {
            get => _query;
            set { if (SetProperty(ref _query, value)) Render(); }
        }

        private int _sortIndex;
        public int SortIndex
        {
            get => _sortIndex;
            private set
            {
                if (SetProperty(ref _sortIndex, value))
                {
                    OnPropertyChanged(nameof(SortLabel));
                    Render();
                }
            }
        }

        public string SortLabel => SortLabels[_sortIndex];

        public int TracksCount => _allTracks.Count;
        public int ArtistsCount => _allTracks.Select(t => t.ArtistID).Distinct().Count();
        public string DurationText
        {
            get { int s = _allTracks.Sum(t => t.Duration); return $"{s / 60}м {s % 60}с"; }
        }

        public string GenreDropdownLabel => SelectedGenres.Count == 0
            ? "Все жанры"
            : SelectedGenres.Count <= 3
                ? string.Join(", ", SelectedGenres.OrderBy(g => g))
                : $"Выбрано жанров: {SelectedGenres.Count}";

        public ICommand ToggleSortCommand { get; }
        public ICommand ToggleGenreCommand { get; }
        public ICommand ClearGenresCommand { get; }

        public bool IsGenreActive(string genre)
            => genre == "Все" ? SelectedGenres.Count == 0 : SelectedGenres.Contains(genre);

        private void ToggleGenre(string? genre)
        {
            if (string.IsNullOrWhiteSpace(genre)) return;
            if (genre == "Все") SelectedGenres.Clear();
            else if (SelectedGenres.Contains(genre)) SelectedGenres.Remove(genre);
            else SelectedGenres.Add(genre);
            OnPropertyChanged(nameof(GenreDropdownLabel));
            Render();
        }

        private void Load()
        {
            _allTracks.Clear();
            try
            {
                foreach (var id in Session.LikedTrackIds)
                {
                    var t = TrackService.GetTrackById(id);
                    if (t != null) _allTracks.Add(t);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки избранного: {ex.Message}");
            }
            RefreshGenres();
            Render();
            RaiseStats();
        }

        private void OnLikeChanged(int trackId, bool isNowLiked)
        {
            if (!isNowLiked)
            {
                var toRemove = _allTracks.FirstOrDefault(t => t.TrackID == trackId);
                if (toRemove != null) _allTracks.Remove(toRemove);
            }
            else if (!_allTracks.Any(t => t.TrackID == trackId))
            {
                var track = TrackService.GetTrackById(trackId);
                if (track != null) _allTracks.Add(track);
            }
            RefreshGenres();
            Render();
            RaiseStats();
        }

        private void RefreshGenres()
        {
            AvailableGenres.Clear();
            var genres = GenreService.DefaultGenres
                .Concat(_allTracks.Select(t => NormalizeGenre(t.Genre)))
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g);
            foreach (var g in genres) AvailableGenres.Add(g);
        }

        private void Render()
        {
            string q = (_query ?? "").Trim().ToLower();
            string mode = SortModes[_sortIndex];

            var list = _allTracks
                .Where(t => SelectedGenres.Count == 0 || SelectedGenres.Contains(NormalizeGenre(t.Genre)))
                .Where(t => string.IsNullOrEmpty(q)
                    || t.Title.ToLower().Contains(q)
                    || (t.Artist?.Nickname?.ToLower().Contains(q) ?? false)
                    || NormalizeGenre(t.Genre).ToLower().Contains(q));

            list = mode switch
            {
                "az" => list.OrderBy(t => t.Title),
                "artist" => list.OrderBy(t => t.Artist?.Nickname),
                "duration" => list.OrderBy(t => t.Duration),
                _ => list
            };

            Results.Clear();
            foreach (var t in list) Results.Add(t);
            ResultsChanged?.Invoke();
        }

        private void RaiseStats()
        {
            OnPropertyChanged(nameof(TracksCount));
            OnPropertyChanged(nameof(ArtistsCount));
            OnPropertyChanged(nameof(DurationText));
            StatsChanged?.Invoke();
        }

        private static string NormalizeGenre(string? genre)
            => string.IsNullOrWhiteSpace(genre) ? "Other" : genre.Trim();

        public void Dispose() => Session.LikeChanged -= OnLikeChanged;
    }
}
