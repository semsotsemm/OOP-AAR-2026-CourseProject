using System;
using System.IO;
using System.Linq;

namespace Rewind.Helpers
{
    public static class FileStorage
    {
        public const string DefaultAvatar = "Images/Avatars/default_avatar.png";

        public static string DataRoot { get; } = ResolveDataRoot();

        private static string ResolveDataRoot()
        {
            var env = Environment.GetEnvironmentVariable("REWIND_DATA_DIR");
            if (!string.IsNullOrWhiteSpace(env)) return env;

            var localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Rewind");
        }

        public static string CopyAvatar(string sourcePath)        => CopyFile(sourcePath, "Images/Avatars",        keepOriginalName: true);
        public static string CopyTrackCover(string sourcePath)    => CopyFile(sourcePath, "Images/TrackCovers",    keepOriginalName: true);
        public static string CopyPlaylistCover(string sourcePath) => CopyFile(sourcePath, "Images/PlaylistCovers", keepOriginalName: true);
        public static string CopyAlbumCover(string sourcePath)    => CopyFile(sourcePath, "Images/AlbumCovers",    keepOriginalName: true);

        public static string CopyTrackAudio(string sourcePath, string trackName)
        {
            string extension = Path.GetExtension(sourcePath);
            string safe = SanitizeFileName(trackName);
            if (string.IsNullOrWhiteSpace(safe)) safe = "track";

            string relative = CopyFile(sourcePath, "MusicLibrary",
                keepOriginalName: false, forcedBaseName: safe, forcedExtension: extension);
            return Path.GetFileName(relative);
        }

        public static string ResolvePath(string? storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return string.Empty;
            if (Path.IsPathRooted(storedPath)) return storedPath;

            string rel = storedPath.Replace('/', Path.DirectorySeparatorChar);

            string inData = Path.Combine(DataRoot, rel);
            if (File.Exists(inData)) return inData;

            string inApp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rel);
            if (File.Exists(inApp)) return inApp;

            return inData;
        }

        public static string ResolveImagePath(string? storedPath, string legacyFolder = "CoversLibrary")
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return string.Empty;
            if (Path.IsPathRooted(storedPath)) return storedPath;

            string direct = ResolvePath(storedPath);
            if (File.Exists(direct)) return direct;

            string legacyData = Path.Combine(DataRoot, legacyFolder, storedPath);
            if (File.Exists(legacyData)) return legacyData;

            string legacyApp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, legacyFolder, storedPath);
            return legacyApp;
        }

        private static string CopyFile(string sourcePath, string relativeFolder,
            bool keepOriginalName, string? forcedBaseName = null, string? forcedExtension = null)
        {
            string folder = Path.Combine(DataRoot, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(folder);

            string ext = forcedExtension ?? Path.GetExtension(sourcePath);
            string baseName = forcedBaseName ?? (keepOriginalName
                ? Path.GetFileNameWithoutExtension(sourcePath)
                : Guid.NewGuid().ToString());
            baseName = SanitizeFileName(baseName);
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "file";

            string fileName = $"{baseName}{ext}";
            string dest = Path.Combine(folder, fileName);
            int n = 1;
            while (File.Exists(dest))
            {
                fileName = $"{baseName}_{n++}{ext}";
                dest = Path.Combine(folder, fileName);
            }
            File.Copy(sourcePath, dest, overwrite: false);
            return Path.Combine(relativeFolder, fileName).Replace('\\', '/');
        }

        public static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        }
    }
}
