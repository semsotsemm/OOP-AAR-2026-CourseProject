using Rewind.Helpers;
using Rewind.Contols;
using Rewind.MVVM.Services;
using Rewind.MVVM.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rewind.Tabs.UsersTabs
{
    /// <summary>
    /// View поверх <see cref="ArtistProfileViewModel"/>.
    /// VM хранит данные исполнителя, его треки и состояние подписки.
    /// View — императивно рендерит TrackItem и карточки альбомов.
    /// </summary>
    public partial class ArtistProfilePage : UserControl
    {
        private readonly ArtistProfileViewModel _vm;
        private readonly int _artistId;
        private User? _artist;
        private List<Track> _publishedTracks = new();
        private List<TrackItem> _trackItems = new();
        private bool _showAllTracks = false;

        public ArtistProfilePage(int artistId)
        {
            InitializeComponent();
            _artistId = artistId;
            _vm = new ArtistProfileViewModel(
                artistId,
                ServiceLocator.Resolve<INavigationService>(),
                ServiceLocator.Resolve<IDialogService>());
            DataContext = _vm;
            _vm.Loaded += () => Dispatcher.Invoke(LoadPage);
            Loaded += (_, _) => LoadPage();
        }

        // ─────────────────────────────────────────────
        //  Page load
        // ─────────────────────────────────────────────

        private void LoadPage()
        {
            // Resolve artist
            try { _artist = UserService.GetUserById(_artistId); } catch { }
            if (_artist == null) return;

            ArtistNameText.Text = _artist.Nickname;

            // Avatar
            try
            {
                string avatarStoredPath = !string.IsNullOrEmpty(_artist.ProfilePhotoPath)
                    ? _artist.ProfilePhotoPath
                    : FileStorage.DefaultAvatar;
                string fp = FileStorage.ResolveImagePath(avatarStoredPath, "AvatarsLibrary");
                if (File.Exists(fp))
                    HeroAvatarBrush.ImageSource = new BitmapImage(new Uri(fp));
            }
            catch { }

            // Published tracks, ordered by popularity
            try
            {
                _publishedTracks = TrackService.GetByArtistAll(_artistId)
                    .Where(t => t.PublishStatus == "Published")
                    .OrderByDescending(t => t.Statistics?.PlayCount ?? 0)
                    .ToList();
            }
            catch { }

            // Follower count
            int followerCount = 0;
            try { followerCount = SubscriptionService.GetFollowers(_artistId).Count; } catch { }

            ArtistStatsText.Text = $"{_publishedTracks.Count} треков  •  {followerCount} подписчиков";

            // Subscribe button visibility
            if (_artistId == Session.UserId)
                SubscribeBtn.Visibility = Visibility.Collapsed;
            else
                RefreshSubscribeBtn();

            // Build TrackItem wrappers
            _trackItems = _publishedTracks
                .Select(t =>
                {
                    string fp = Path.Combine(Rewind.Helpers.FileStorage.DataRoot, "MusicLibrary", t.FilePath);
                    return new TrackItem(
                        t.TrackID, t.Title, _artist.Nickname,
                        FormatDuration(t.Duration), fp, t.CoverPath, t.Duration);
                })
                .ToList();

            // Top-3 tracks
            TopTracksContainer.Children.Clear();
            foreach (var ti in _trackItems.Take(3))
            {
                ti.MouseLeftButtonDown += OnTrackClick;
                TopTracksContainer.Children.Add(ti);
            }

            // Remaining tracks (shown on demand)
            AllTracksContainer.Children.Clear();
            foreach (var ti in _trackItems.Skip(3))
            {
                ti.MouseLeftButtonDown += OnTrackClick;
                AllTracksContainer.Children.Add(ti);
            }

            // Hide "show all" toggle if there are 3 or fewer tracks
            if (_trackItems.Count <= 3)
            {
                ShowAllBtn.Text = "";
                ShowAllBtn.Visibility = Visibility.Collapsed;
            }

            // Albums section
            try { LoadAlbums(); } catch { }
        }

        // ─────────────────────────────────────────────
        //  Albums
        // ─────────────────────────────────────────────

        private void LoadAlbums()
        {
            var albums = AlbumService.GetByArtist(_artistId);
            if (albums.Count == 0) return;

            AlbumsSection.Visibility = Visibility.Visible;
            AlbumsContainer.Children.Clear();

            foreach (var album in albums)
            {
                // Card shell
                var card = new Border
                {
                    Width = 140,
                    Height = 170,
                    CornerRadius = new CornerRadius(14),
                    Margin = new Thickness(0, 0, 12, 0),
                    Cursor = Cursors.Hand,
                    Background = new SolidColorBrush(Color.FromRgb(245, 244, 240)),
                    ClipToBounds = true
                };

                var inner = new StackPanel();

                // Cover image (top half of card)
                var coverBorder = new Border
                {
                    Height = 110,
                    CornerRadius = new CornerRadius(14, 14, 0, 0),
                    ClipToBounds = true,
                    Background = (Brush?)Application.Current.TryFindResource("GreenGradientStyle")
                                 ?? new LinearGradientBrush(
                                     Color.FromRgb(42, 232, 118),
                                     Color.FromRgb(0, 77, 64),
                                     new Point(0, 0), new Point(1, 1))
                };

                bool hasCover = false;
                if (!string.IsNullOrEmpty(album.CoverPath))
                {
                    try
                    {
                        string fp = FileStorage.ResolveImagePath(album.CoverPath, "AlbumCovers");
                        if (File.Exists(fp))
                        {
                            coverBorder.Background = new ImageBrush(new BitmapImage(new Uri(fp)))
                            {
                                Stretch = Stretch.UniformToFill
                            };
                            hasCover = true;
                        }
                    }
                    catch { }
                }
                if (!hasCover)
                {
                    coverBorder.Child = new Image
                    {
                        Source = IconAssets.LoadBitmap("music_note.png"),
                        Width = 44, Height = 44,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }

                inner.Children.Add(coverBorder);

                // Album title
                inner.Children.Add(new TextBlock
                {
                    Text = album.Title,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(10, 8, 10, 2),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 24))
                });

                // Track count
                inner.Children.Add(new TextBlock
                {
                    Text = $"{album.AlbumTracks?.Count ?? 0} треков",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 128)),
                    Margin = new Thickness(10, 0, 10, 0)
                });

                card.Child = inner;
                int albumId = album.AlbumId;
                card.MouseLeftButtonDown += (_, _) =>
                {
                    var full = AlbumService.GetById(albumId) ?? album;
                    if (Window.GetWindow(this) is MainWindow mw)
                        mw.OpenAlbumDetails(full);
                };
                AlbumsContainer.Children.Add(card);
            }
        }

        // ─────────────────────────────────────────────
        //  Track playback
        // ─────────────────────────────────────────────

        private void OnTrackClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TrackItem clicked) return;
            if (Window.GetWindow(this) is MainWindow mw)
                mw.PlayTrackFromContext(clicked, _trackItems);
        }

        // ─────────────────────────────────────────────
        //  Subscribe / Unsubscribe
        // ─────────────────────────────────────────────

        private void RefreshSubscribeBtn()
        {
            bool isFollowing = false;
            try { isFollowing = SubscriptionService.IsFollowing(Session.UserId, _artistId); } catch { }

            SubscribeBtnText.Text = isFollowing ? "✓ Вы подписаны" : "+ Подписаться";
            SubscribeBtn.Background = isFollowing
                ? new SolidColorBrush(Color.FromRgb(42, 140, 84))
                : new SolidColorBrush(Colors.White);
            SubscribeBtnText.Foreground = isFollowing
                ? new SolidColorBrush(Colors.White)
                : new SolidColorBrush(Color.FromRgb(26, 26, 24));
        }

        private void Subscribe_Click(object sender, MouseButtonEventArgs e)
        {
            // Делегируем VM. Toast уведомление остаётся здесь — это визуальное действие окна.
            bool wasFollowing = _vm.IsFollowing;
            _vm.ToggleSubscribeCommand.Execute(null);
            if (!wasFollowing && Session.NotifNewTracksEnabled && Session.NotifPushEnabled
                && _publishedTracks.FirstOrDefault() is { } recent
                && _vm.Artist != null
                && Window.GetWindow(this) is MainWindow mw)
            {
                mw.ShowToastNotification(
                    $"Подписка на {_vm.Artist.Nickname}!",
                    $"Последний трек: «{recent.Title}»");
            }

            ArtistStatsText.Text = _vm.StatsText;
            RefreshSubscribeBtn();
        }

        // ─────────────────────────────────────────────
        //  Show all / collapse tracks toggle
        // ─────────────────────────────────────────────

        private void ToggleAllTracks_Click(object sender, MouseButtonEventArgs e)
        {
            _showAllTracks = !_showAllTracks;
            AllTracksContainer.Visibility = _showAllTracks ? Visibility.Visible : Visibility.Collapsed;
            ShowAllBtn.Text = _showAllTracks ? "↑ Скрыть" : "Все треки →";
        }

        // ─────────────────────────────────────────────
        //  Navigation
        // ─────────────────────────────────────────────

        private void Back_Click(object sender, MouseButtonEventArgs e) => _vm.BackCommand.Execute(null);

        // ─────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────

        public IReadOnlyList<TrackItem> GetTrackItems() => _trackItems;

        private static string FormatDuration(int sec) => sec > 0 ? $"{sec / 60}:{sec % 60:D2}" : "";
    }
}
