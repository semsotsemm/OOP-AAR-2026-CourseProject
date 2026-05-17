using Rewind.Helpers;
using Rewind.MVVM.Core;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Pages
{
    public sealed class SearchPageViewModel : ViewModelBase
    {
        private static readonly string[] SortModes = { "title", "artist", "duration" };
        private static readonly string[] SortLabels = { "Название", "Исполнитель", "Длительность" };

        public SearchPageViewModel()
        {
            SelectedGenres = new ObservableCollection<string>();
            Results = new ObservableCollection<Track>();
            AvailableGenres = new ObservableCollection<string>();

            ToggleSortCommand = new RelayCommand(CycleSort);
            ToggleGenreCommand = new RelayCommand<string>(ToggleGenre);
            ClearGenresCommand = new RelayCommand(() => { SelectedGenres.Clear(); RefreshGenres(); Render(); });

            RefreshGenres();
            Render();
        }

        public ObservableCollection<Track> Results { get; }
        public ObservableCollection<string> AvailableGenres { get; }
        public ObservableCollection<string> SelectedGenres { get; }

        public event Action? ResultsChanged;

        private string _query = "";
        public string Query
        {
            get => _query;
            set { if (SetProperty(ref _query, value)) Render(); }
        }

        private int _sortIndex = 0;
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

        public string GenreDropdownLabel => SelectedGenres.Count == 0
            ? "Все жанры"
            : SelectedGenres.Count <= 3
                ? string.Join(", ", SelectedGenres.OrderBy(g => g))
                : $"Выбрано жанров: {SelectedGenres.Count}";

        public ICommand ToggleSortCommand { get; }
        public ICommand ToggleGenreCommand { get; }
        public ICommand ClearGenresCommand { get; }

        private void CycleSort() => SortIndex = (SortIndex + 1) % SortModes.Length;

        private void ToggleGenre(string? genre)
        {
            if (string.IsNullOrWhiteSpace(genre)) return;
            if (genre == "Все") SelectedGenres.Clear();
            else if (SelectedGenres.Contains(genre)) SelectedGenres.Remove(genre);
            else SelectedGenres.Add(genre);
            OnPropertyChanged(nameof(GenreDropdownLabel));
            Render();
        }

        public bool IsGenreActive(string genre)
            => genre == "Все" ? SelectedGenres.Count == 0 : SelectedGenres.Contains(genre);

        private void RefreshGenres()
        {
            AvailableGenres.Clear();
            var genres = GenreService.DefaultGenres
                .Concat(TrackService.GetPublishedTracks().Select(t => NormalizeGenre(t.Genre)))
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g);
            foreach (var g in genres) AvailableGenres.Add(g);
        }

        private void Render()
        {
            string q = (_query ?? "").Trim().ToLower();
            string mode = SortModes[_sortIndex];

            var list = TrackService.GetPublishedTracks()
                .Where(t => SelectedGenres.Count == 0 || SelectedGenres.Contains(NormalizeGenre(t.Genre)))
                .Where(t => string.IsNullOrEmpty(q)
                            || t.Title.ToLower().Contains(q)
                            || (t.Artist?.Nickname?.ToLower().Contains(q) ?? false)
                            || NormalizeGenre(t.Genre).ToLower().Contains(q));

            list = mode switch
            {
                "artist" => list.OrderBy(t => t.Artist?.Nickname),
                "duration" => list.OrderBy(t => t.Duration),
                _ => list.OrderBy(t => t.Title)
            };

            Results.Clear();
            foreach (var t in list.Take(40)) Results.Add(t);
            ResultsChanged?.Invoke();
        }

        private static string NormalizeGenre(string? genre)
            => string.IsNullOrWhiteSpace(genre) ? "Other" : genre.Trim();
    }
}
