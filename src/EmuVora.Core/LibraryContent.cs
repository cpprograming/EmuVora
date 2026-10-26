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
    public static string PlaylistFormats(string system)=>system=="Dreamcast"?"CHD / CUE / GDI":"CHD / CUE";
    public static string? PlaylistEmulator(string system) => system switch
    {
        "PS1" => "DuckStation oder RetroArch / Beetle PSX",
        "Saturn" => "RetroArch / Beetle Saturn",
        "Dreamcast" => "RetroArch / Flycast",
        "Mega-CD" => "RetroArch / Genesis Plus GX",
        _ => null
    };
    private static readonly HashSet<string> IndexFiles = new(StringComparer.OrdinalIgnoreCase) { ".cue", ".m3u", ".gdi", ".ccd", ".toc", ".mds" };
    internal static bool IsReferenceFile(string path) => IndexFiles.Contains(Path.GetExtension(path));

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
        if (PlaylistEmulator(group.System) == null || group.Games.Count < 2 || group.Games.Count > 99) throw new IOException("Playlist-Erstellung unterstützt PS1, Saturn, Dreamcast und Mega-CD mit 2 bis 99 CHD/CUE-Discs; Dreamcast zusätzlich GDI.");
        var inspected = LibraryAudit.Inspect(group.Games, token).Where(g => g.Kind == "Mehrdisc-Kandidat").ToArray();
        if (inspected.Length != 1 || inspected[0].Games.Count != group.Games.Count || inspected[0].Title != group.Title) throw new IOException("Der Disc-Satz ist nicht eindeutig.");
        var ordered = inspected[0].Games;
        var markers = ordered.Select(g => Regex.Match(g.Title, @"[\(\[]\s*(?:disc|disk|cd)\s*0*([1-9]\d?)(?:\s*(?:of|/)\s*\d+)?\s*[\)\]]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).ToArray();
        var numbers = markers.Select(m => int.Parse(m.Groups[1].Value)).ToArray();
        if (!numbers.SequenceEqual(Enumerable.Range(1, numbers.Length))) throw new IOException("Disc-Nummern müssen eindeutig und lückenlos bei 1 beginnen.");
        var totals = ordered.Select(g => Regex.Match(g.Title, @"(?:disc|disk|cd)\s*\d+\s*(?:of|/)\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value));
        if (totals.Any(total => total != ordered.Count)) throw new IOException("Die angegebene Gesamtzahl stimmt nicht mit dem Disc-Satz überein.");
        var folder = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(ordered[0].Path))!;
        if(ordered.Any(g=>!System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(g.Path))!.Equals(folder,StringComparison.OrdinalIgnoreCase)))
        {
            if(group.System!="Dreamcast"||ordered.Any(g=>!g.Path.EndsWith(".gdi",StringComparison.OrdinalIgnoreCase)))throw new IOException("Nur GDI-Sätze in eindeutig nummerierten benachbarten Disc-Ordnern dürfen verschiedene Ordner verwenden.");
            folder=System.IO.Path.GetDirectoryName(folder)!;if(ordered.Any(g=>!System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(g.Path))!)!.Equals(folder,StringComparison.OrdinalIgnoreCase)))throw new IOException("GDI-Disc-Ordner müssen denselben übergeordneten Ordner haben.");
        }
        Backups.NoLinks(folder);
        var snapshots = new Dictionary<string, DiscFileSnapshot>(StringComparer.OrdinalIgnoreCase);
        var lines = new List<string>();
        foreach (var game in ordered)
        {
            token.ThrowIfCancellationRequested();
            var path = System.IO.Path.GetFullPath(game.Path); var name = System.IO.Path.GetFileName(path);
            var relative=System.IO.Path.GetRelativePath(folder,path).Replace('\\','/');
            if (game.System != group.System || !Backups.Inside(path,folder) || relative!=relative.Trim() || relative.StartsWith('#') || relative.Any(char.IsControl) || name != name.Trim()) throw new IOException("Disc-Pfade sind für eine relative Playlist ungeeignet.");
            var extension = System.IO.Path.GetExtension(path);
            var gdi=extension.Equals(".gdi",StringComparison.OrdinalIgnoreCase)&&group.System=="Dreamcast";
            if (!extension.Equals(".chd", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".cue", StringComparison.OrdinalIgnoreCase)&&!gdi) throw new IOException("CHD/CUE oder bei Dreamcast GDI wählen; rohe Tracks und CDI werden nicht gebündelt.");
            AddFile(path, snapshots);
            if (extension.Equals(".cue", StringComparison.OrdinalIgnoreCase)) CheckCue(path, folder, snapshots, token);
            if(gdi)foreach(var track in GdiManifest.Read(path,token))AddFile(track.Path,snapshots);
            lines.Add(relative);
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
            return new(plan.Group.Title, plan.Group.System, plan.Path);
        }
        finally { foreach (var handle in handles) handle.Dispose(); if (temporary != null && File.Exists(temporary)) File.Delete(temporary); }
    }
}
