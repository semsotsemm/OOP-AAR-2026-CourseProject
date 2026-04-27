using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Rewind
{
    public static class Session
    {
        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        private static string _userName = "Алексей Антипов";
        public static string UserName
        {
            get => _userName;
            set { _userName = value; OnStaticPropertyChanged(); }
        }

        private static string _email = "antipovalexey@mail.ru";
        public static string Email
        {
            get => _email;
            set { _email = value; OnStaticPropertyChanged(); }
        }

        private static string _password = "12345678";
        public static string Password
        {
            get => _password;
            set { _password = value; OnStaticPropertyChanged(); }
        }

        private static string _hidedPassword = "********";
        public static string HidedPassword
        {
            get => _hidedPassword;
            set { _hidedPassword = value; OnStaticPropertyChanged(); }
        }

        private static string _avatarPath = "C:\\Users\\untermensh\\Useless\\OOP-AAR-2026-CourseProject\\Rewind\\Images\\default_avatar.jpg";
        public static string AvatarPath
        {
            get => _avatarPath;
            set { _avatarPath = value; OnStaticPropertyChanged(); }
        }

        public static int UserId { get; set; }
        public static string UserRole { get; set; } = "Слушатель";
        public static string ActiveTheme { get; set; } = "ThemeClassic";

        private static int _tracksListened = 0;
        public static int TracksListened
        {
            get => _tracksListened;
            set { _tracksListened = value; OnStaticPropertyChanged(); }
        }

        private static int _listened = 0;
        public static int Listened
        {
            get => _listened;
            set { _listened = value; OnStaticPropertyChanged(); }
        }

        private static int _playlists = 0;
        public static int Playlists
        {
            get => _playlists;
            set { _playlists = value; OnStaticPropertyChanged(); }
        }

        private static int _liked = 0;
        public static int Liked
        {
            get => _liked;
            set { _liked = value; OnStaticPropertyChanged(); }
        }

        private static int _subscriptions = 0;
        public static int Subscriptions
        {
            get => _subscriptions;
            set { _subscriptions = value; OnStaticPropertyChanged(); }
        }

        public static void AddListenedTrack(double durationSeconds)
        {
            TracksListened++;
            Listened += (int)Math.Round(durationSeconds / 60.0);
        }
    }
}
