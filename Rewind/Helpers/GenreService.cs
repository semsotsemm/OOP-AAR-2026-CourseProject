namespace Rewind.Helpers
{
    public static class GenreService
    {
        public static IReadOnlyList<string> DefaultGenres { get; } = new[]
        {
            "Pop", "Rock", "Hip-Hop", "Electronic", "R&B", "Jazz", "Classical", "Metal",
            "Folk", "Indie", "Alternative", "Dance", "Reggae", "Latin", "Other"
        };

        public static List<string> GetAllGenresFromTracks()
        {
            using var db = new AppDbContext();
            return db.Tracks
                .Select(t => t.Genre)
                .Where(g => g != null && g != "")
                .ToList()!
                .Select(g => g!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g)
                .ToList();
        }
    }
}
