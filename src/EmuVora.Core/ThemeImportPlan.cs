using System.Security.Cryptography;
using System.Text.Json;

namespace EmuVora.Core;

public sealed record ThemeSourceFile(string Path, long Length, string Sha256);
public sealed record ThemeImportPlan(string Source, string System, bool PreferDark, ThemeSelection? Selection,
    EmulationStationTheme Preview, IReadOnlyList<ThemeSourceFile> Files, IReadOnlyList<string> Notes)
{
    public long Bytes => Files.Sum(f => f.Length);
}

public sealed partial class EmulationStationTheme
{
    private static IReadOnlyList<ThemeSourceFile> Snapshot(string root, CancellationToken token)
    {
        Backups.NoLinks(root); var files = new List<ThemeSourceFile>(); long bytes = 0;
        void Read(string folder, int depth)
        {
            token.ThrowIfCancellationRequested(); Backups.NoLinks(folder);
            if (depth > 32) throw new IOException("Theme-Ordner ist zu tief verschachtelt.");
            foreach (var file in Directory.EnumerateFiles(folder))
            {
                token.ThrowIfCancellationRequested(); Backups.NoLinks(file);
                using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                bytes = checked(bytes + input.Length);
                if (files.Count >= 10000 || bytes > 256L * 1024 * 1024) throw new IOException("Theme ist zu groß (max. 256 MiB / 10.000 Dateien).");
                var hash = SHA256.HashData(input);
                files.Add(new(Path.GetRelativePath(root, file), input.Length, Convert.ToHexString(hash)));
            }
            foreach (var child in Directory.EnumerateDirectories(folder)) Read(child, depth + 1);
        }
        Read(root, 0); return files.OrderBy(f => f.Path, StringComparer.Ordinal).ToArray();
    }
    public static ThemeImportPlan PreviewImport(string source, string system, bool preferDark = true, ThemeSelection? selection = null, CancellationToken token = default)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source));
        var files = Snapshot(root, token);
        if (!files.Any(f => Path.GetFileName(f.Path).Equals("theme.xml", StringComparison.OrdinalIgnoreCase))) throw new IOException("Der Ordner enthält keine theme.xml.");
        var notes = new HashSet<string>(); var supported = 0;
        foreach (var name in EmulatorCatalog.Systems)
        {
            token.ThrowIfCancellationRequested(); var theme = Load(root, name, preferDark, selection);
            supported += theme.Elements.Count(e => e.Properties.Count > 0); foreach (var note in theme.Warnings) notes.Add(note);
        }
        if (supported == 0) throw new IOException("Keine unterstützten Theme-Elemente gefunden.");
        var preview = Load(root, system, preferDark, selection);
        if (!files.SequenceEqual(Snapshot(root, token))) throw new IOException("Theme-Dateien wurden während der Prüfung verändert. Vorschau erneut prüfen.");
        return new(root, system, preferDark, selection, preview, files, notes.Order(StringComparer.Ordinal).ToArray());
    }
    public static string ApplyImport(ThemeImportPlan plan, string dataDirectory, CancellationToken token = default)
    {
        var parent = Path.Combine(Path.GetFullPath(dataDirectory), "themes"); var root = plan.Source;
        if (parent.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || root.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || root.Equals(parent, StringComparison.OrdinalIgnoreCase)) throw new IOException("Bitte einen Theme-Ordner außerhalb des verwalteten Theme-Verzeichnisses wählen.");
        var fresh = PreviewImport(root, plan.System, plan.PreferDark, plan.Selection, token);
        if (!plan.Files.SequenceEqual(fresh.Files)) throw new IOException("Theme-Dateien wurden nach der Vorschau verändert. Vorschau erneut prüfen.");
        Backups.NoLinks(parent);
        var destination = Path.Combine(parent, Path.GetFileName(root) + "-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(destination);
            foreach (var file in fresh.Files)
            {
                token.ThrowIfCancellationRequested(); var source = Path.Combine(root, file.Path); Backups.NoLinks(source);
                var target = Path.Combine(destination, file.Path); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var output = new FileStream(target, FileMode.CreateNew);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); var buffer = new byte[81920]; int read; long size = 0;
                while ((read = input.Read(buffer)) > 0) { token.ThrowIfCancellationRequested(); size += read; hash.AppendData(buffer, 0, read); output.Write(buffer, 0, read); }
                if (size != file.Length || Convert.ToHexString(hash.GetHashAndReset()) != file.Sha256) throw new IOException("Theme-Datei hat sich während des Kopierens verändert.");
            }
            if (plan.Selection != null) File.WriteAllText(Path.Combine(destination, SelectionFile), JsonSerializer.Serialize(plan.Selection, SettingsStore.JsonOptions));
            File.WriteAllText(Path.Combine(destination, "emuvora-import-report.txt"), "EmuVora · Theme-Anpassung\nLayout und Bedienung bleiben nativ. Die Vorschau zeigt unterstützte Eigenschaften, keine vollständige ES-DE-Oberfläche.\n" + string.Join("\n", fresh.Notes));
            token.ThrowIfCancellationRequested(); return destination;
        }
        catch { if (Directory.Exists(destination)) Directory.Delete(destination, true); throw; }
    }
}
