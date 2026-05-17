using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rewind.Helpers
{
    public class Role
    {
        [Key] public int RoleId { get; set; }
        [Required, MaxLength(20)] public string RoleName { get; set; }
        public List<User> Users { get; set; } = new();
    }

    public class User
    {
        [Key] public int UserId { get; set; }
        [Required, MaxLength(50)] public string Nickname { get; set; }
        [Required, MaxLength(100)] public string Email { get; set; }
        [Required] public string PasswordHash { get; set; }
        public string? ProfilePhotoPath { get; set; }
        public string? Status { get; set; }
        public int RoleId { get; set; }
        public Role Role { get; set; }
        public List<Track> Tracks { get; set; } = new();
        public List<Playlist> Playlists { get; set; } = new();
        public List<Favorite> Favorites { get; set; } = new();
        public List<ListeningHistory> History { get; set; } = new();
    }

    public class Track
    {
        [Key] public int TrackID { get; set; }
        [Required, MaxLength(255)] public string Title { get; set; }
        [Required] public string FilePath { get; set; }
        public string? CoverPath { get; set; }
        public int Duration { get; set; }
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        public int ArtistID { get; set; }
        public string? Genre { get; set; }
        public string PublishStatus { get; set; } = "Published";
        public string? RejectionReason { get; set; }
        public User Artist { get; set; }
        public Statistic Statistics { get; set; }
        public List<PlaylistTrack> PlaylistTracks { get; set; } = new();
        public List<Favorite> Favorites { get; set; } = new();
        public List<ListeningHistory> History { get; set; } = new();
    }

    public class ListeningHistory
    {
        [Key] public int HistoryId { get; set; }
        public int UserID { get; set; }
        public User User { get; set; }
        public int TrackID { get; set; }
        public Track Track { get; set; }
        public DateTime ListenedAt { get; set; } = DateTime.UtcNow;
    }

    public class Statistic
    {
        [Key, ForeignKey("Track")]
        public int TrackID { get; set; }
        public int PlayCount { get; set; } = 0;
        public int LikesCount { get; set; } = 0;
        public Track Track { get; set; }
    }

    public class Playlist
    {
        [Key] public int PlaylistID { get; set; }
        [Required, MaxLength(100)] public string Title { get; set; }
        public int OwnerID { get; set; }
        public User Owner { get; set; }
        public bool IsPrivate { get; set; } = false;
        public string? CoverPath { get; set; }
        public List<PlaylistTrack> PlaylistTracks { get; set; } = new();
    }

    public class PlaylistTrack
    {
        public int PlaylistID { get; set; }
        public Playlist Playlist { get; set; }
        public int TrackID { get; set; }
        public Track Track { get; set; }
    }

    public class Subscription
    {
        public int FollowerID { get; set; }
        public User Follower { get; set; }
        public int ArtistID { get; set; }
        public User Artist { get; set; }
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    }

    public class Favorite
    {
        public int UserID { get; set; }
        public User User { get; set; }
        public int TrackID { get; set; }
        public Track Track { get; set; }
    }

    public class Album
    {
        [Key] public int AlbumId { get; set; }
        [Required, MaxLength(100)] public string Title { get; set; } = "";
        public int ArtistId { get; set; }
        public User Artist { get; set; } = null!;
        public string? CoverPath { get; set; }
        public string? Genre { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<AlbumTrack> AlbumTracks { get; set; } = new();
    }

    public class AlbumTrack
    {
        public int AlbumId { get; set; }
        public Album Album { get; set; } = null!;
        public int TrackId { get; set; }
        public Track Track { get; set; } = null!;
    }

    public class SavedAlbum
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int AlbumId { get; set; }
        public Album Album { get; set; } = null!;
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }

    public class AlbumListenEvent
    {
        [Key] public int AlbumListenEventId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int AlbumId { get; set; }
        public Album Album { get; set; } = null!;
        public DateTime ListenedAt { get; set; } = DateTime.UtcNow;
    }

    public class ArtistRequest
    {
        [Key] public int RequestId { get; set; }
        [Required, MaxLength(50)] public string Nickname { get; set; } = "";
        [Required, MaxLength(100)] public string Email { get; set; } = "";
        [Required] public string PasswordHash { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class TrackReport
    {
        [Key] public int ReportId { get; set; }
        public int TrackId { get; set; }
        public Track Track { get; set; } = null!;
        public int ReporterId { get; set; }
        public User Reporter { get; set; } = null!;
        [Required] public string Reason { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class PlaylistListen
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int PlaylistId { get; set; }
        public Playlist Playlist { get; set; } = null!;
        public DateTime ListenedAt { get; set; } = DateTime.UtcNow;
    }

    public class PlaylistPlayEvent
    {
        [Key] public int PlaylistPlayEventId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int PlaylistId { get; set; }
        public Playlist Playlist { get; set; } = null!;
        public DateTime ListenedAt { get; set; } = DateTime.UtcNow;
    }

    public class SavedPlaylist
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int PlaylistId { get; set; }
        public Playlist Playlist { get; set; } = null!;
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
