using System.Windows;
using Rewind.Contols;
using System.Windows.Input;
using System.Windows.Controls;
using Rewind.Helpers;

namespace Rewind.Tabs.UsersTabs
{
    public partial class MainPage : UserControl
    {
        private readonly List<TrackItem> _trackItems = new();

        public MainPage()
        {
            InitializeComponent();

            UpdateGreeting();
            Loaded += (_, _) => LoadMusicFromFolder();
        }

        private void UpdateGreeting()
        {
            var h = DateTime.Now.Hour;
            string time = h < 6 ? "Доброй ночи" : h < 12 ? "Доброе утро" :
                          h < 18 ? "Добрый день" : "Добрый вечер";
            GreetingText.Text = $"{time}, {Session.UserName}";
        }

        private void LoadMusicFromFolder()
        {
            try
            {
                MusicContainer.Children.Clear();
                _trackItems.Clear();

                List<Track> tracks = TrackService.GetAllTracks();

                foreach (var track in tracks)
                {
                    string durStr = FormatDuration(track.Duration);
                    string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MusicLibrary", track.FilePath);
                    var item = new TrackItem(track.TrackID, track.Title, UserService.GetUserById(track.ArtistID).Nickname, durStr, fullPath,  track.CoverPath , track.Duration);
                    _trackItems.Add(item);
                    MusicContainer.Children.Add(item);
                }
            }
            catch { }
        }

        private static string FormatDuration(double seconds)
        {
            if (seconds <= 0) return "0:00";
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.Hours > 0 ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}" : $"{ts.Minutes}:{ts.Seconds:D2}";
        }

        private void FeaturedPlay_Click(object sender, MouseButtonEventArgs e)
        {
            if (_trackItems.Count > 0) PlayFirst();
            else MessageBox.Show("Нет треков");
        }

        private void PlayFirst()
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.PlayTrackFromContext(_trackItems[0], _trackItems);
        }

        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;
    }
}