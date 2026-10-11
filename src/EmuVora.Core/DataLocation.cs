using System.Text.Json;

namespace EmuVora.Core;

public sealed class DataLocation(string bootstrapFile)
{
    public static string LegacyDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EmuVora");
    public static string BootstrapFile => Path.Combine(LegacyDirectory, "location.json");
    public string? Read()
    {
        if (!File.Exists(bootstrapFile)) return null;
        var result = JsonSerializer.Deserialize<Location>(File.ReadAllText(bootstrapFile).TrimStart('\uFEFF'));
        if (result == null || string.IsNullOrWhiteSpace(result.DataDirectory)) throw new IOException("Der gespeicherte Datenpfad ist ungültig.");
        return Path.GetFullPath(result.DataDirectory);
    }
    public void Select(string directory)
    {
        var full = Prepare(directory);
        AtomicFile.WriteText(bootstrapFile, JsonSerializer.Serialize(new Location(full), SettingsStore.JsonOptions));
    }
    public static string Prepare(string directory)
    {
        if (!Path.IsPathFullyQualified(directory)) throw new IOException("Bitte einen vollständigen Pfad mit Laufwerk wählen.");
        var full = Path.GetFullPath(directory);
        if (full.TrimEnd(Path.DirectorySeparatorChar).Equals(Path.GetPathRoot(full)?.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)) throw new IOException("Bitte einen Unterordner statt des gesamten Laufwerks wählen.");
        Directory.CreateDirectory(full);
        var probe = Path.Combine(full, ".emuvora-write-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(probe, "test"); File.Delete(probe);
        return full;
    }
    public static void CheckAvailable(string directory)
    {
        // Never silently create a fresh library when a removable drive is absent.
        if (!Directory.Exists(directory)) throw new IOException("Der Datenordner ist nicht erreichbar: " + directory + "\nBitte das Laufwerk verbinden oder den Pfad erneut auswählen.");
    }
    public record Location(string DataDirectory);
}
