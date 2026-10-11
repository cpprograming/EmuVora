using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

internal static class Setup
{
    private static string Quote(string value)
    {
        // Windows command-line quoting, including trailing backslashes.
        var result = new System.Text.StringBuilder("\""); int slashes = 0;
        foreach (char c in value)
        {
            if (c == '\\') { slashes++; continue; }
            result.Append('\\', c == '"' ? slashes * 2 + 1 : slashes); result.Append(c); slashes = 0;
        }
        result.Append('\\', slashes * 2); result.Append('"'); return result.ToString();
    }

    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        string destination = null;
        bool shortcuts = true, launch = true, unattended = args.Length > 0;
        string temporary = Path.Combine(Path.GetTempPath(), "EmuVora-setup-" + Guid.NewGuid().ToString("N"));
        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--install-dir" && i + 1 < args.Length) destination = args[++i];
                else if (args[i] == "--no-shortcuts") shortcuts = false;
                else if (args[i] == "--no-launch") launch = false;
                else throw new ArgumentException("Unbekannter Setup-Parameter: " + args[i]);
            }
            if (unattended && destination == null) throw new ArgumentException("--install-dir fehlt.");
            if (!unattended)
            {
                using (var form = new Form())
                {
                    form.Text = "EmuVora installieren"; form.ClientSize = new Size(580, 285);
                    form.StartPosition = FormStartPosition.CenterScreen; form.FormBorderStyle = FormBorderStyle.FixedDialog;
                    form.MaximizeBox = false; form.MinimizeBox = false;
                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EmuVora.ico")) form.Icon = new Icon(stream);
                    var title = new Label { Text = "EmuVora", Location = new Point(22, 20), Size = new Size(530, 38), Font = new Font("Segoe UI", 21, FontStyle.Bold) };
                    var info = new Label { Text = "Festplatte und Installationsordner wählen. Programm und Daten liegen\nin den Unterordnern App und Data. Vorhandene Spielstände bleiben erhalten.", Location = new Point(24, 67), Size = new Size(530, 44) };
                    var path = new TextBox { Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EmuVora"), Location = new Point(24, 124), Size = new Size(425, 25) };
                    var browse = new Button { Text = "Auswählen …", Location = new Point(456, 121), Size = new Size(100, 28) };
                    browse.Click += delegate { using (var picker = new FolderBrowserDialog { Description = "EmuVora-Installationsordner wählen", SelectedPath = path.Text }) if (picker.ShowDialog(form) == DialogResult.OK) path.Text = picker.SelectedPath; };
                    var links = new CheckBox { Text = "Eigene Verknüpfungen auf Desktop und im Startmenü", Checked = true, Location = new Point(24, 168), Size = new Size(530, 24) };
                    var start = new CheckBox { Text = "EmuVora nach der Installation starten", Checked = true, Location = new Point(24, 196), Size = new Size(530, 24) };
                    var install = new Button { Text = "Installieren", DialogResult = DialogResult.OK, Location = new Point(344, 240), Size = new Size(100, 30) };
                    var cancel = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, Location = new Point(456, 240), Size = new Size(100, 30) };
                    form.Controls.AddRange(new Control[] { title, info, path, browse, links, start, install, cancel });
                    form.AcceptButton = install; form.CancelButton = cancel;
                    if (form.ShowDialog() != DialogResult.OK) return 0;
                    destination = path.Text; shortcuts = links.Checked; launch = start.Checked;
                }
            }
            if (!Path.IsPathRooted(destination)) throw new IOException("Bitte einen vollständigen Installationspfad wählen.");
            Directory.CreateDirectory(temporary);
            string archive = Path.Combine(temporary, "package.zip"), package = Path.Combine(temporary, "package");
            using (var input = Assembly.GetExecutingAssembly().GetManifestResourceStream("EmuVora.package.zip"))
            using (var output = File.Create(archive)) input.CopyTo(output);
            ZipFile.ExtractToDirectory(archive, package);
            var process = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell\\v1.0\\powershell.exe"));
            process.UseShellExecute = false; process.CreateNoWindow = true;
            process.RedirectStandardOutput = true; process.RedirectStandardError = true;
            process.Arguments = "-NoProfile -STA -ExecutionPolicy Bypass -File " + Quote(Path.Combine(package, "Install-EmuVora.ps1")) + " -DestinationPath " + Quote(destination) + (shortcuts ? " -DesktopShortcut" : " -SkipShortcuts");
            using (var child = Process.Start(process))
            {
                var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
                child.WaitForExit(); System.Threading.Tasks.Task.WaitAll(output, error);
                if (child.ExitCode != 0) throw new IOException(error.Result + "\n" + output.Result);
            }
            if (!unattended) MessageBox.Show("EmuVora wurde installiert.\n\n" + destination, "EmuVora", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (launch) Process.Start(new ProcessStartInfo(Path.Combine(destination, "App", "EmuVora.App.exe")) { UseShellExecute = true, WorkingDirectory = Path.Combine(destination, "App") });
            return 0;
        }
        catch (Exception ex)
        {
            if (unattended) { try { File.WriteAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "EmuVora-setup-error.log"), ex.ToString()); } catch { } }
            else MessageBox.Show(ex.Message, "EmuVora – Installation fehlgeschlagen", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
        finally
        {
            // Delete only the unique directory created by this setup run.
            try { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); } catch { }
        }
    }
}
