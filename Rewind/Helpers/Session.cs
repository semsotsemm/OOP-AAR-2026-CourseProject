namespace Rewind
{
    class Session
    {
        public static int UserId { get; set; }
        public static string UserName { get; set; } = "Алексей Антипов";
        public static string Email { get; set; } = "antipovalexey@mail.ru";
        public static string UserRole { get; set; } = "Слушатель";
        public static string Password { get; set; } = "12345678";
        public static string HidedPassword { get; set; } = "********";
        public static string ActiveTheme { get; set; } = "ThemeClassic";
        public static int TracksListened { get; set; } = 0;
        public static int Listened { get; set; } = 0;
        public static int Playlists { get; set; } = 0;
        public static int Liked { get; set; } = 0;
        public static int Subscriptions { get; set; } = 0;
        public static int Theme { get; set; } = 0;

    }
}
