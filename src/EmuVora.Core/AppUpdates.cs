using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

namespace EmuVora.Core;

public record AppRelease(Version Version, Uri Download, string Sha256, long Size, string Notes);

public sealed class AppUpdates(HttpClient http)
{
    public const string Repository = "cpprograming/EmuVora";
    public const long MaximumSize = 256 * 1024 * 1024;
    public async Task<AppRelease?> CheckAsync(Version current, CancellationToken token = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/" + Repository + "/releases/latest");
        request.Headers.UserAgent.ParseAdd("EmuVora/0.7");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await ReadBoundedAsync(response, 1024 * 1024, token));
        var root = json.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean()) return null;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!Version.TryParse(tag.TrimStart('v'), out var version) || version <= current) return null;
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            if (asset.GetProperty("name").GetString() != "EmuVora-Setup.exe") continue;
            var digest = asset.TryGetProperty("digest", out var hash) ? hash.GetString() : null;
            var size = asset.GetProperty("size").GetInt64();
            var expected = "https://github.com/" + Repository + "/releases/download/" + Uri.EscapeDataString(tag) + "/EmuVora-Setup.exe";
            if (asset.GetProperty("browser_download_url").GetString() != expected || size < 2 || size > MaximumSize || digest == null || !digest.StartsWith("sha256:") || digest.Length != 71 || !digest[7..].All(Uri.IsHexDigit))
                throw new InvalidDataException("Das EmuVora-Update hat keine gültige Download-Adresse oder SHA-256-Prüfsumme.");
            return new(version, new(expected), digest[7..], size, root.TryGetProperty("body", out var notes) ? notes.GetString() ?? "" : "");
        }
        throw new InvalidDataException("Die neue Version enthält kein Windows-Setup.");
    }

    public async Task<string> DownloadAsync(AppRelease release, string directory, IProgress<double>? progress = null, CancellationToken token = default)
    {
        // CheckAsync accepts numeric tags with or without v; downloads must stay in this repository.
        if (release.Download.Scheme != "https" || release.Download.Host != "github.com" || !release.Download.AbsolutePath.StartsWith("/" + Repository + "/releases/download/", StringComparison.Ordinal) || !release.Download.AbsolutePath.EndsWith("/EmuVora-Setup.exe", StringComparison.Ordinal) || release.Size < 2 || release.Size > MaximumSize)
            throw new InvalidDataException("Ungültiges EmuVora-Update.");
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, "EmuVora-" + release.Version + "-" + Guid.NewGuid().ToString("N") + ".exe");
        var temporary = file + ".part";
        try
        {
            using var response = await http.GetAsync(release.Download, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is long length && length != release.Size) throw new InvalidDataException("Update-Größe stimmt nicht überein.");
            await using var input = await response.Content.ReadAsStreamAsync(token);
            await using (var output = File.Create(temporary))
            {
                var buffer = new byte[81920]; long total = 0; int count;
                while ((count = await input.ReadAsync(buffer, token)) != 0)
                {
                    total += count;
                    if (total > release.Size) throw new InvalidDataException("Update ist größer als angekündigt.");
                    await output.WriteAsync(buffer.AsMemory(0, count), token); progress?.Report(100d * total / release.Size);
                }
                if (total != release.Size) throw new InvalidDataException("Update-Download ist unvollständig.");
            }
            await VerifyAsync(temporary, release, token);
            File.Move(temporary, file); return file;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static async Task VerifyAsync(string file, AppRelease release, CancellationToken token = default)
    {
        await using var input = File.OpenRead(file);
        if (input.Length != release.Size || input.ReadByte() != 'M' || input.ReadByte() != 'Z') throw new InvalidDataException("Update ist keine gültige Windows-Programmdatei.");
        input.Position = 0;
        var digest = Convert.ToHexString(await SHA256.HashDataAsync(input, token));
        if (!digest.Equals(release.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Update-Prüfsumme stimmt nicht überein. Installation abgebrochen.");
    }

    public static string? InstallationRoot(string executable)
    {
        var app = Path.GetDirectoryName(Path.GetFullPath(executable))!;
        return Path.GetFileName(app).Equals("App", StringComparison.OrdinalIgnoreCase) && File.Exists(Path.Combine(app, "emuvora-location.json")) ? Path.GetDirectoryName(app) : null;
    }
    public static ProcessStartInfo SetupStartInfo(string setup, string root, string data, int processId)
    {
        if (!Path.IsPathFullyQualified(setup) || !Path.IsPathFullyQualified(root) || !Path.IsPathFullyQualified(data) || processId <= 0) throw new ArgumentException("Ungültiger Update-Pfad.");
        var start = new ProcessStartInfo(setup) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(setup)! };
        foreach (var argument in new[] { "--install-dir", root, "--data-dir", data, "--wait-for-pid", processId.ToString(), "--no-shortcuts" }) start.ArgumentList.Add(argument);
        return start;
    }
    private static async Task<byte[]> ReadBoundedAsync(HttpResponseMessage response, int limit, CancellationToken token)
    {
        await using var input = await response.Content.ReadAsStreamAsync(token); using var output = new MemoryStream();
        var buffer = new byte[8192]; int count;
        while ((count = await input.ReadAsync(buffer, token)) != 0) { if (output.Length + count > limit) throw new InvalidDataException("Update-Antwort zu groß."); output.Write(buffer, 0, count); }
        return output.ToArray();
    }
}
