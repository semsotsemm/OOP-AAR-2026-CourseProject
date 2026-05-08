-- ============================================================
--  Rewind DB  --  Microsoft SQL Server
--  Все 18 таблиц. Запусти в SSMS, затем открой
--  Database Diagrams -> New Diagram -> добавь все таблицы.
-- ============================================================

USE master;
GO

-- Создаём базу, если ещё нет
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'RewindDB')
    CREATE DATABASE RewindDB;
GO

USE RewindDB;
GO

-- ============================================================
--  1. Roles
-- ============================================================
IF OBJECT_ID('dbo.Roles', 'U') IS NULL
CREATE TABLE dbo.Roles (
    RoleId   INT          NOT NULL IDENTITY(1,1) PRIMARY KEY,
    RoleName VARCHAR(20)  NOT NULL
);
GO

-- ============================================================
--  2. Users
-- ============================================================
IF OBJECT_ID('dbo.Users', 'U') IS NULL
CREATE TABLE dbo.Users (
    UserId           INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Nickname         VARCHAR(50)   NOT NULL,
    Email            VARCHAR(100)  NOT NULL,
    PasswordHash     NVARCHAR(MAX) NOT NULL,
    ProfilePhotoPath NVARCHAR(MAX) NULL,
    Status           VARCHAR(50)   NULL,
    RoleId           INT           NOT NULL,

    CONSTRAINT UQ_Users_Nickname UNIQUE (Nickname),
    CONSTRAINT UQ_Users_Email    UNIQUE (Email),
    CONSTRAINT FK_Users_Roles    FOREIGN KEY (RoleId) REFERENCES dbo.Roles (RoleId)
);
GO

-- ============================================================
--  3. ArtistRequests  (standalone — без FK)
-- ============================================================
IF OBJECT_ID('dbo.ArtistRequests', 'U') IS NULL
CREATE TABLE dbo.ArtistRequests (
    RequestId    INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Nickname     VARCHAR(50)   NOT NULL,
    Email        VARCHAR(100)  NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Status       VARCHAR(20)   NOT NULL DEFAULT 'Pending',
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================================
--  4. Tracks
-- ============================================================
IF OBJECT_ID('dbo.Tracks', 'U') IS NULL
CREATE TABLE dbo.Tracks (
    TrackID         INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Title           VARCHAR(255)   NOT NULL,
    FilePath        NVARCHAR(MAX)  NOT NULL,
    CoverPath       NVARCHAR(MAX)  NULL,
    Duration        INT            NOT NULL DEFAULT 0,
    UploadDate      DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    ArtistID        INT            NOT NULL,
    Genre           VARCHAR(100)   NULL,
    PublishStatus   VARCHAR(20)    NOT NULL DEFAULT 'Published',
    RejectionReason NVARCHAR(MAX)  NULL,

    CONSTRAINT FK_Tracks_Users FOREIGN KEY (ArtistID) REFERENCES dbo.Users (UserId)
        ON DELETE CASCADE
);
GO

-- ============================================================
--  5. Statistics  (1:1 с Tracks)
-- ============================================================
IF OBJECT_ID('dbo.Statistics', 'U') IS NULL
CREATE TABLE dbo.Statistics (
    TrackID    INT NOT NULL PRIMARY KEY,
    PlayCount  INT NOT NULL DEFAULT 0,
    LikesCount INT NOT NULL DEFAULT 0,

    CONSTRAINT FK_Statistics_Tracks FOREIGN KEY (TrackID) REFERENCES dbo.Tracks (TrackID)
        ON DELETE CASCADE
);
GO

-- ============================================================
--  6. Playlists
-- ============================================================
IF OBJECT_ID('dbo.Playlists', 'U') IS NULL
CREATE TABLE dbo.Playlists (
    PlaylistID INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Title      VARCHAR(100)   NOT NULL,
    OwnerID    INT            NOT NULL,
    IsPrivate  BIT            NOT NULL DEFAULT 0,
    CoverPath  NVARCHAR(MAX)  NULL,

    CONSTRAINT FK_Playlists_Users FOREIGN KEY (OwnerID) REFERENCES dbo.Users (UserId)
        ON DELETE CASCADE
);
GO

-- ============================================================
--  7. PlaylistTracks  (M:N)
-- ============================================================
IF OBJECT_ID('dbo.PlaylistTracks', 'U') IS NULL
CREATE TABLE dbo.PlaylistTracks (
    PlaylistID INT NOT NULL,
    TrackID    INT NOT NULL,

    CONSTRAINT PK_PlaylistTracks PRIMARY KEY (PlaylistID, TrackID),
    CONSTRAINT FK_PLT_Playlists  FOREIGN KEY (PlaylistID) REFERENCES dbo.Playlists (PlaylistID) ON DELETE CASCADE,
    CONSTRAINT FK_PLT_Tracks     FOREIGN KEY (TrackID)    REFERENCES dbo.Tracks    (TrackID)    ON DELETE NO ACTION
);
GO

-- ============================================================
--  8. Favorites  (M:N)
-- ============================================================
IF OBJECT_ID('dbo.Favorites', 'U') IS NULL
CREATE TABLE dbo.Favorites (
    UserID  INT NOT NULL,
    TrackID INT NOT NULL,

    CONSTRAINT PK_Favorites     PRIMARY KEY (UserID, TrackID),
    CONSTRAINT FK_FAV_Users     FOREIGN KEY (UserID)  REFERENCES dbo.Users  (UserId)  ON DELETE CASCADE,
    CONSTRAINT FK_FAV_Tracks    FOREIGN KEY (TrackID) REFERENCES dbo.Tracks (TrackID) ON DELETE NO ACTION
);
GO

-- ============================================================
--  9. ListeningHistory
-- ============================================================
IF OBJECT_ID('dbo.ListeningHistory', 'U') IS NULL
CREATE TABLE dbo.ListeningHistory (
    HistoryId  INT       NOT NULL IDENTITY(1,1) PRIMARY KEY,
    UserID     INT       NOT NULL,
    TrackID    INT       NOT NULL,
    ListenedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_LH_Users  FOREIGN KEY (UserID)  REFERENCES dbo.Users  (UserId)  ON DELETE CASCADE,
    CONSTRAINT FK_LH_Tracks FOREIGN KEY (TrackID) REFERENCES dbo.Tracks (TrackID) ON DELETE NO ACTION
);
GO
CREATE INDEX IX_LH_UserID  ON dbo.ListeningHistory (UserID);
CREATE INDEX IX_LH_TrackID ON dbo.ListeningHistory (TrackID);
GO

-- ============================================================
--  10. Subscriptions  (User подписывается на User)
-- ============================================================
IF OBJECT_ID('dbo.Subscriptions', 'U') IS NULL
CREATE TABLE dbo.Subscriptions (
    FollowerID   INT       NOT NULL,
    ArtistID     INT       NOT NULL,
    SubscribedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Subscriptions      PRIMARY KEY (FollowerID, ArtistID),
    CONSTRAINT FK_SUB_Follower        FOREIGN KEY (FollowerID) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_SUB_Artist          FOREIGN KEY (ArtistID)   REFERENCES dbo.Users (UserId)
);
GO

-- ============================================================
--  11. Albums
-- ============================================================
IF OBJECT_ID('dbo.Albums', 'U') IS NULL
CREATE TABLE dbo.Albums (
    AlbumId   INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Title     VARCHAR(100)   NOT NULL,
    ArtistId  INT            NOT NULL,
    CoverPath NVARCHAR(MAX)  NULL,
    Genre     VARCHAR(100)   NULL,
    CreatedAt DATETIME2      NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_Albums_Users FOREIGN KEY (ArtistId) REFERENCES dbo.Users (UserId)
        ON DELETE CASCADE
);
GO

-- ============================================================
--  12. AlbumTracks  (M:N)
-- ============================================================
IF OBJECT_ID('dbo.AlbumTracks', 'U') IS NULL
CREATE TABLE dbo.AlbumTracks (
    AlbumId INT NOT NULL,
    TrackId INT NOT NULL,

    CONSTRAINT PK_AlbumTracks    PRIMARY KEY (AlbumId, TrackId),
    CONSTRAINT FK_AT_Albums      FOREIGN KEY (AlbumId) REFERENCES dbo.Albums (AlbumId) ON DELETE CASCADE,
    CONSTRAINT FK_AT_Tracks      FOREIGN KEY (TrackId) REFERENCES dbo.Tracks (TrackID) ON DELETE NO ACTION
);
GO

-- ============================================================
--  13. SavedAlbums  (M:N)
-- ============================================================
IF OBJECT_ID('dbo.SavedAlbums', 'U') IS NULL
CREATE TABLE dbo.SavedAlbums (
    UserId  INT       NOT NULL,
    AlbumId INT       NOT NULL,
    SavedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_SavedAlbums   PRIMARY KEY (UserId, AlbumId),
    CONSTRAINT FK_SA_Users      FOREIGN KEY (UserId)  REFERENCES dbo.Users  (UserId)  ON DELETE NO ACTION,
    CONSTRAINT FK_SA_Albums     FOREIGN KEY (AlbumId) REFERENCES dbo.Albums (AlbumId) ON DELETE CASCADE
);
GO

-- ============================================================
--  14. AlbumListenEvents
-- ============================================================
IF OBJECT_ID('dbo.AlbumListenEvents', 'U') IS NULL
CREATE TABLE dbo.AlbumListenEvents (
    AlbumListenEventId INT       NOT NULL IDENTITY(1,1) PRIMARY KEY,
    UserId             INT       NOT NULL,
    AlbumId            INT       NOT NULL,
    ListenedAt         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_ALE_Users  FOREIGN KEY (UserId)  REFERENCES dbo.Users  (UserId)  ON DELETE NO ACTION,
    CONSTRAINT FK_ALE_Albums FOREIGN KEY (AlbumId) REFERENCES dbo.Albums (AlbumId) ON DELETE CASCADE
);
GO

-- ============================================================
--  15. TrackReports
-- ============================================================
IF OBJECT_ID('dbo.TrackReports', 'U') IS NULL
CREATE TABLE dbo.TrackReports (
    ReportId   INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    TrackId    INT            NOT NULL,
    ReporterId INT            NOT NULL,
    Reason     NVARCHAR(MAX)  NOT NULL,
    Status     VARCHAR(20)    NOT NULL DEFAULT 'Pending',
    CreatedAt  DATETIME2      NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_TR_Tracks FOREIGN KEY (TrackId)    REFERENCES dbo.Tracks (TrackID) ON DELETE CASCADE,
    CONSTRAINT FK_TR_Users  FOREIGN KEY (ReporterId) REFERENCES dbo.Users  (UserId)  ON DELETE NO ACTION
);
GO

-- ============================================================
--  16. SavedPlaylists  (M:N)
-- ============================================================
IF OBJECT_ID('dbo.SavedPlaylists', 'U') IS NULL
CREATE TABLE dbo.SavedPlaylists (
    UserId     INT       NOT NULL,
    PlaylistId INT       NOT NULL,
    SavedAt    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_SavedPlaylists  PRIMARY KEY (UserId, PlaylistId),
    CONSTRAINT FK_SP_Users        FOREIGN KEY (UserId)     REFERENCES dbo.Users     (UserId)     ON DELETE NO ACTION,
    CONSTRAINT FK_SP_Playlists    FOREIGN KEY (PlaylistId) REFERENCES dbo.Playlists (PlaylistID) ON DELETE CASCADE
);
GO

-- ============================================================
--  17. PlaylistListens  (M:N)
-- ============================================================
IF OBJECT_ID('dbo.PlaylistListens', 'U') IS NULL
CREATE TABLE dbo.PlaylistListens (
    UserId     INT       NOT NULL,
    PlaylistId INT       NOT NULL,
    ListenedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_PlaylistListens PRIMARY KEY (UserId, PlaylistId),
    CONSTRAINT FK_PL_Users        FOREIGN KEY (UserId)     REFERENCES dbo.Users     (UserId)     ON DELETE NO ACTION,
    CONSTRAINT FK_PL_Playlists    FOREIGN KEY (PlaylistId) REFERENCES dbo.Playlists (PlaylistID) ON DELETE CASCADE
);
GO

-- ============================================================
--  18. PlaylistPlayEvents
-- ============================================================
IF OBJECT_ID('dbo.PlaylistPlayEvents', 'U') IS NULL
CREATE TABLE dbo.PlaylistPlayEvents (
    PlaylistPlayEventId INT       NOT NULL IDENTITY(1,1) PRIMARY KEY,
    UserId              INT       NOT NULL,
    PlaylistId          INT       NOT NULL,
    ListenedAt          DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_PPE_Users     FOREIGN KEY (UserId)     REFERENCES dbo.Users     (UserId)     ON DELETE NO ACTION,
    CONSTRAINT FK_PPE_Playlists FOREIGN KEY (PlaylistId) REFERENCES dbo.Playlists (PlaylistID) ON DELETE CASCADE
);
GO

-- ============================================================
--  Seed: дефолтные роли
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Roles)
BEGIN
    INSERT INTO dbo.Roles (RoleName) VALUES ('Admin'), ('Artist'), ('Listener');
END
GO

PRINT 'RewindDB: все 18 таблиц созданы успешно.';
GO
