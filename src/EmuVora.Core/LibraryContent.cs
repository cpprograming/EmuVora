using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EmuVora.Core;

public record ContentMatch(string Sha256, long Length, IReadOnlyList<Game> Games);
public record ContentCheck(int CheckedFiles, IReadOnlyList<ContentMatch> Matches, IReadOnlyList<string> Notes);
public record DiscFileSnapshot(string Path, long Length, DateTime LastWriteUtc);
public record DiscPlaylistPlan(LibraryAuditGroup Group, string Path, string Content, IReadOnlyList<DiscFileSnapshot> Files);

public static class LibraryContent
{
    private static readonly HashSet<string> IndexFiles = new(StringComparer.OrdinalIgnoreCase) { ".cue", ".m3u", ".gdi", ".ccd", ".toc", ".mds" };

    public static async Task<ContentCheck> CompareAsync(IEnumerable<Game> games, CancellationToken token = default)
    {
        var matches = new Dictionary<(string Hash, long Length), List<Game>>();
        var notes = new List<string>(); var count = 0;
        foreach (var game in games.DistinctBy(g => Path.GetFullPath(g.Path), StringComparer.OrdinalIgnoreCase))
        {
            token.ThrowIfCancellationRequested();
            if (IndexFiles.Contains(System.IO.Path.GetExtension(game.Path))) { notes.Add(game.Path + ": Verweisdatei ausgelassen; gleiche Listen beweisen keine gleichen Disc-Inhalte."); continue; }
            try
            {
                Backups.NoLinks(game.Path);
                using var stream = new FileStream(game.Path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, FileOptions.Asynchronous | FileOptions.SequentialScan);
                var before = Snapshot(game.Path);
                if (before.Length == 0) { notes.Add(game.Path + ": leere Datei ausgelassen."); continue; }
                var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, token));
                if (before != Snapshot(game.Path)) throw new IOException("Datei wurde während der Prüfung geändert.");
                if (!matches.TryGetValue((hash, before.Length), out var entries)) matches[(hash, before.Length)] = entries = [];
                entries.Add(game); count++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { notes.Add(game.Path + ": " + ex.Message); }
        }
        return new(count, matches.Where(p => p.Value.Count > 1).Select(p => new ContentMatch(p.Key.Hash, p.Key.Length, p.Value)).ToArray(), notes);
    }

    public static DiscPlaylistPlan PreviewPlaylist(LibraryAuditGroup group, CancellationToken token = default)
    {
        if (group.System != "PS1" || group.Games.Count < 2 || group.Games.Count > 99) throw new IOException("Playlist-Erstellung unterstützt zunächst PS1-Disc-Sätze mit 2 bis 99 Discs.");
        var inspected = LibraryAudit.Inspect(group.Games, token).Where(g => g.Kind == "Mehrdisc-Kandidat").ToArray();
        if (inspected.Length != 1 || inspected[0].Games.Count != group.Games.Count || inspected[0].Title != group.Title) throw new IOException("Der Disc-Satz ist nicht eindeutig.");
        var ordered = inspected[0].Games;
        var markers = ordered.Select(g => Regex.Match(g.Title, @"[\(\[]\s*(?:disc|disk|cd)\s*0*([1-9]\d?)(?:\s*(?:of|/)\s*\d+)?\s*[\)\]]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).ToArray();
        var numbers = markers.Select(m => int.Parse(m.Groups[1].Value)).ToArray();
        if (!numbers.SequenceEqual(Enumerable.Range(1, numbers.Length))) throw new IOException("Disc-Nummern müssen eindeutig und lückenlos bei 1 beginnen.");
        var totals = ordered.Select(g => Regex.Match(g.Title, @"(?:disc|disk|cd)\s*\d+\s*(?:of|/)\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value));
        if (totals.Any(total => total != ordered.Count)) throw new IOException("Die angegebene Gesamtzahl stimmt nicht mit dem Disc-Satz überein.");
        var folder = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(ordered[0].Path))!;
        Backups.NoLinks(folder);
        var snapshots = new Dictionary<string, DiscFileSnapshot>(StringComparer.OrdinalIgnoreCase);
        var lines = new List<string>();
        foreach (var game in ordered)
        {
            token.ThrowIfCancellationRequested();
            var path = System.IO.Path.GetFullPath(game.Path); var name = System.IO.Path.GetFileName(path);
            if (game.System != "PS1" || !System.IO.Path.GetDirectoryName(path)!.Equals(folder, StringComparison.OrdinalIgnoreCase) || name != name.Trim() || name.StartsWith('#') || name.Any(char.IsControl)) throw new IOException("Disc-Pfade sind für eine relative Playlist ungeeignet.");
            var extension = System.IO.Path.GetExtension(path);
            if (!extension.Equals(".chd", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".cue", StringComparison.OrdinalIgnoreCase)) throw new IOException("Nur CHD- oder CUE-Discs werden unterstützt; BIN-Tracks nicht einzeln aufnehmen.");
            AddFile(path, snapshots);
            if (extension.Equals(".cue", StringComparison.OrdinalIgnoreCase)) CheckCue(path, folder, snapshots, token);
            lines.Add(name);
        }
        var title = string.Concat(group.Title.Select(c => System.IO.Path.GetInvalidFileNameChars().Contains(c) || char.IsControl(c) ? '_' : c)).Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(title)) throw new IOException("Der Playlist-Name ist leer.");
        var target = System.IO.Path.Combine(folder, title + ".m3u");
        if (File.Exists(target) || Directory.Exists(target)) throw new IOException("Die Playlist existiert bereits und wird nicht überschrieben.");
        return new(group, target, string.Join("\n", lines) + "\n", snapshots.Values.ToArray());
    }

    private static DiscFileSnapshot Snapshot(string path) { var file = new FileInfo(path); if (!file.Exists) throw new FileNotFoundException("Disc-Datei fehlt.", path); return new(file.FullName, file.Length, file.LastWriteTimeUtc); }
    private static void AddFile(string path, Dictionary<string, DiscFileSnapshot> files)
    {
        Backups.NoLinks(path);
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var file = Snapshot(path); if (file.Length == 0) throw new IOException("Disc-Datei ist leer: " + path);
        files[file.Path] = file;
    }
    private static void CheckCue(string path, string folder, Dictionary<string, DiscFileSnapshot> files, CancellationToken token)
    {
        using var input = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (input.Length > 1024 * 1024) throw new IOException("CUE-Datei ist größer als 1 MiB.");
        if (Snapshot(path) != files[System.IO.Path.GetFullPath(path)]) throw new IOException("CUE-Datei wurde während der Vorschau geändert.");
        using var reader = new StreamReader(input, Encoding.UTF8, true);
        var text = reader.ReadToEnd();
        var count = 0;
        foreach (var line in text.Split('\n'))
        {
            token.ThrowIfCancellationRequested();
            var match = Regex.Match(line.Trim(), "^FILE\\s+(?:\"([^\"]+)\"|(\\S+))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success) continue;
            var name = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            if (System.IO.Path.IsPathRooted(name) || name.Any(char.IsControl)) throw new IOException("CUE-Verweise müssen relativ sein.");
            var dependency = System.IO.Path.GetFullPath(System.IO.Path.Combine(folder, name));
            if (!Backups.Inside(dependency, folder)) throw new IOException("CUE-Verweis liegt außerhalb des Disc-Ordners.");
            AddFile(dependency, files); count++;
        }
        if (count == 0) throw new IOException("CUE enthält keine FILE-Verweise.");
    }

    public static Game CreatePlaylist(DiscPlaylistPlan plan, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var current = PreviewPlaylist(plan.Group, token);
        if (current.Path != plan.Path || current.Content != plan.Content || !current.Files.OrderBy(f => f.Path).SequenceEqual(plan.Files.OrderBy(f => f.Path))) throw new IOException("Disc-Dateien oder Vorschau wurden geändert. Bitte erneut prüfen.");
        var handles = new List<FileStream>(); string? temporary = null;
        try
        {
            foreach (var file in current.Files) { token.ThrowIfCancellationRequested(); handles.Add(File.Open(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read)); if (Snapshot(file.Path) != file) throw new IOException("Disc-Datei wurde geändert."); }
            temporary = plan.Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                output.Write(new UTF8Encoding(false).GetBytes(plan.Content)); output.Flush(true);
            }
            token.ThrowIfCancellationRequested(); Backups.NoLinks(System.IO.Path.GetDirectoryName(plan.Path)!);
            File.Move(temporary, plan.Path, false); temporary = null;
            return new(plan.Group.Title, "PS1", plan.Path);
        }
        finally { foreach (var handle in handles) handle.Dispose(); if (temporary != null && File.Exists(temporary)) File.Delete(temporary); }
    }
}
