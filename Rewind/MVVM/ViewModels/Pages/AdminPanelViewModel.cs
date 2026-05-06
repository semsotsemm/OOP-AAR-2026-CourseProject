using Rewind.Helpers;
using Rewind.MVVM.Core;
using System.Windows.Input;

namespace Rewind.MVVM.ViewModels.Pages
{
    /// <summary>
    /// VM админки. Хранит активную вкладку, бейджи и команды (тема, выход,
    /// навигация). View занимается подменой UserControl-а в AdminContentArea
    /// и подсветкой кнопок (визуальная задача).
    /// </summary>
    public sealed class AdminPanelViewModel : ViewModelBase
    {
        public AdminPanelViewModel()
        {
            SelectTabCommand = new RelayCommand<string>(name => { if (name != null) ActiveTab = name; });
            SelectThemeCommand = new RelayCommand<string>(name =>
            {
                if (string.IsNullOrEmpty(name)) return;
                Session.ActiveTheme = name;
                ThemeChanged?.Invoke(name);
            });
            LogoutCommand = new RelayCommand(() => LogoutRequested?.Invoke());
            RefreshBadges();
        }

        // ─── Активная вкладка ──────────────────────

        private string _activeTab = "overview";
        public string ActiveTab
        {
            get => _activeTab;
            set { if (SetProperty(ref _activeTab, value)) TabChanged?.Invoke(value); }
        }
        public event Action<string>? TabChanged;
        public event Action<string>? ThemeChanged;
        public event Action? LogoutRequested;

        public ICommand SelectTabCommand { get; }
        public ICommand SelectThemeCommand { get; }
        public ICommand LogoutCommand { get; }

        // ─── Бейджи ────────────────────────────────

        public int RequestsCount { get; private set; }
        public int SubmissionsCount { get; private set; }
        public int ReportsCount { get; private set; }
        public string RequestsBadgeText => RequestsCount > 99 ? "99+" : RequestsCount.ToString();
        public string SubmissionsBadgeText => SubmissionsCount > 99 ? "99+" : SubmissionsCount.ToString();
        public string ReportsBadgeText => ReportsCount > 99 ? "99+" : ReportsCount.ToString();

        public bool RequestsBadgeVisible => RequestsCount > 0;
        public bool SubmissionsBadgeVisible => SubmissionsCount > 0;
        public bool ReportsBadgeVisible => ReportsCount > 0;

        public void RefreshBadges()
        {
            try { RequestsCount = ArtistRequestService.PendingCount(); } catch { }
            try { SubmissionsCount = TrackService.GetPendingTracks().Count; } catch { }
            try { ReportsCount = TrackReportService.PendingCount(); } catch { }

            OnPropertyChanged(nameof(RequestsBadgeText));
            OnPropertyChanged(nameof(SubmissionsBadgeText));
            OnPropertyChanged(nameof(ReportsBadgeText));
            OnPropertyChanged(nameof(RequestsBadgeVisible));
            OnPropertyChanged(nameof(SubmissionsBadgeVisible));
            OnPropertyChanged(nameof(ReportsBadgeVisible));
        }
    }
}
