using Rewind.Helpers;
using Rewind.MVVM.Core;
using Rewind.MVVM.Services;
using Rewind.MVVM.ViewModels.Entities;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Pages
{
    /// <summary>
    /// VM страницы «Мои плейлисты». Держит фильтрацию (all/own/saved),
    /// поиск и команды (создать плейлист, открыть, переключить фильтр).
    /// View рендерит карточки `PlaylistViewModel` императивно (карточки —
    /// сложная вёрстка с тенями/масками, не имеет смысла переписывать XAML).
    /// </summary>
    public sealed class PlaylistsPageViewModel : ViewModelBase, IDisposable
    {
        private readonly INavigationService _nav;
        private readonly IDialogService _dialog;
        private readonly List<PlaylistViewModel> _all = new();

        public PlaylistsPageViewModel(INavigationService nav, IDialogService dialog)
        {
            _nav = nav;
            _dialog = dialog;
            Shown = new ObservableCollection<PlaylistViewModel>();

            FilterAllCommand = new RelayCommand(() => SetFilter("all"));
            FilterOwnCommand = new RelayCommand(() => SetFilter("own"));
            FilterSavedCommand = new RelayCommand(() => SetFilter("saved"));
            CreatePlaylistCommand = new RelayCommand<(string Title, string? Cover, bool IsPrivate)>(args =>
            {
                if (string.IsNullOrWhiteSpace(args.Title)) return;
                var pl = Session.CreatePlaylist(args.Title, args.Cover, args.IsPrivate);
                _all.Insert(0, new PlaylistViewModel(pl, isOwned: true));
                ActiveFilter = "own";
                Render();
            });

            PlaylistListenService.OnPlaylistListenChanged += OnStatsChanged;
            SavedPlaylistService.OnPlaylistSavedChanged += OnStatsChanged;

            Load();
        }

        public ObservableCollection<PlaylistViewModel> Shown { get; }

        public event Action? RenderRequested;

        private string _activeFilter = "all";
        public string ActiveFilter
        {
            get => _activeFilter;
            set { if (SetProperty(ref _activeFilter, value)) Render(); }
        }

        private string _query = "";
        public string Query
        {
            get => _query;
            set { if (SetProperty(ref _query, value)) Render(); }
        }

        public ICommand FilterAllCommand { get; }
        public ICommand FilterOwnCommand { get; }
        public ICommand FilterSavedCommand { get; }
        public ICommand CreatePlaylistCommand { get; }

        public void OpenPlaylist(PlaylistViewModel vm) => _nav.OpenPlaylistDetails(vm.Model);

        private void SetFilter(string filter) => ActiveFilter = filter;

        private void Load()
        {
            _all.Clear();
            foreach (var pl in Session.CachedPlaylists)
                _all.Add(new PlaylistViewModel(pl, isOwned: true));

            try
            {
                var others = PlaylistService.GetPublicPlaylists(excludeUserId: Session.UserId);
                foreach (var pl in others)
                    _all.Add(new PlaylistViewModel(pl, isOwned: false));
            }
            catch { }
            Render();
        }

        private void Render()
        {
            string q = (_query ?? "").Trim().ToLower();
            Shown.Clear();
            foreach (var p in _all
                .Where(p => _activeFilter == "all"
                            || (_activeFilter == "own" && p.IsOwned)
                            || (_activeFilter == "saved" && !p.IsOwned))
                .Where(p => string.IsNullOrEmpty(q) || p.Title.ToLower().Contains(q)))
                Shown.Add(p);
            RenderRequested?.Invoke();
        }

        private void OnStatsChanged(int _) => Render();

        public void Dispose()
        {
            PlaylistListenService.OnPlaylistListenChanged -= OnStatsChanged;
            SavedPlaylistService.OnPlaylistSavedChanged -= OnStatsChanged;
        }
    }
}
