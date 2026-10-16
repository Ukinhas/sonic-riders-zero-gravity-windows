using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// Frontend for the existing native runtime. No compilation occurs on the player's PC.
internal static class EntryPoint {
    [STAThread]
    private static void Main(string[] args) {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        bool created;
        using (var gate = new Mutex(true, "Local\\SonicRidersZeroGravityNative", out created)) {
            if (!created) {
                MessageBox.Show("Sonic Riders Zero Gravity is already running.");
                return;
            }
            try { Application.Run(new ImportWindow(args)); }
            catch (Exception e) { MessageBox.Show(e.Message, "Sonic Riders Zero Gravity"); }
            finally { gate.ReleaseMutex(); }
        }
    }
}

internal sealed class ImportWindow : Form {
    const string ExpectedElf = "43ff74931c70111c7332875653aa47a3db62a0c513e71c8cf48d1d0649de16ee";
    readonly string saveRoot;
    readonly TextBox isoPath = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
    readonly Button browse = new Button { Text = "Select ISO / Selecionar ISO", AutoSize = true };
    readonly Button play = new Button { Text = "Play / Jogar", AutoSize = true };
    readonly Button cancel = new Button { Text = "Cancelar importação", AutoSize = true, Enabled = false };
    readonly ProgressBar progress = new ProgressBar { Dock = DockStyle.Fill, Maximum = 100 };
    readonly Label status = new Label { Text = "Select your USA PS2 ISO (SLUS_216.42).", AutoSize = true, MaximumSize = new Size(590, 0) };
    bool busy;
    CancellationTokenSource cancellation;

    static string Option(string[] args, string name) {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    public ImportWindow(string[] args) {
        saveRoot = Path.GetFullPath(Option(args, "--profile") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SonicRidersZeroGravity"));
        Text = "Sonic Riders: Zero Gravity — Windows";
        ClientSize = new Size(640, 370);
        MinimumSize = new Size(600, 350);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 7 };
        layout.Controls.Add(new Label { Text = "SONIC RIDERS: ZERO GRAVITY", Font = new Font("Segoe UI", 16, FontStyle.Bold), AutoSize = true });
        layout.Controls.Add(new Label { Text = "Versão Windows não oficial\nImporte sua ISO USA uma vez (4,7 GB). Depois, basta abrir este EXE.", AutoSize = true });
        layout.Controls.Add(isoPath);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.Add(browse); buttons.Controls.Add(play); buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons);
        layout.Controls.Add(progress);
        layout.Controls.Add(status);
        layout.Controls.Add(new Label { Text = "WASD: mover · K/L/I/J: X/bola/triângulo/quadrado\nU/O: L1/R1 · Enter: Start · Setas: menus · F11: tela cheia", AutoSize = true });
        Controls.Add(layout);
        Directory.CreateDirectory(saveRoot);
        string settings = Path.Combine(saveRoot, "iso-path.txt");
        try { if (File.Exists(settings)) isoPath.Text = File.ReadAllText(settings, Encoding.UTF8).Trim(); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        if (Option(args, "--import") != null) isoPath.Text = Option(args, "--import");
        browse.Click += delegate {
            using (var dialog = new OpenFileDialog { Filter = "PS2 ISO (*.iso)|*.iso", Title = "Select Sonic Riders Zero Gravity USA ISO", CheckFileExists = true }) {
                if (dialog.ShowDialog(this) == DialogResult.OK) { isoPath.Text = dialog.FileName; status.Text = "Press Play to validate and start / Clique Jogar para iniciar."; }
            }
        };
        play.Click += async delegate { await StartGame(); };
        cancel.Click += delegate { if (cancellation != null) cancellation.Cancel(); };
        FormClosing += delegate(object sender, FormClosingEventArgs e) { if (busy) { if (cancellation != null) cancellation.Cancel(); e.Cancel = true; } };
        Shown += async delegate {
            if (Array.IndexOf(args, "--setup") < 0 && (Option(args, "--import") != null || InstalledDirectory() != null)) await StartGame();
        };
    }

    static byte[] ReadExact(Stream input, int length) {
        byte[] data = new byte[length];
        int at = 0;
        while (at < length) { int n = input.Read(data, at, length - at); if (n == 0) throw new InvalidDataException("ISO is truncated / ISO incompleta."); at += n; }
        return data;
    }

    static string Hash(byte[] bytes) {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    static byte[] ExtractElf(string path) {
        using (var iso = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            iso.Position = 16 * 2048;
            byte[] pvd = ReadExact(iso, 2048);
            if (pvd[0] != 1 || Encoding.ASCII.GetString(pvd, 1, 5) != "CD001") throw new InvalidDataException("Select an uncompressed ISO 9660 image / Selecione uma ISO não compactada.");
            if ((long)BitConverter.ToUInt32(pvd, 80) * 2048 > iso.Length) throw new InvalidDataException("ISO incompleta: faltam dados do disco.");
            long rootAt = (long)BitConverter.ToUInt32(pvd, 158) * 2048;
            uint rootLength = BitConverter.ToUInt32(pvd, 166);
            if (rootLength == 0 || rootLength > 16 * 1024 * 1024 || rootAt + rootLength > iso.Length) throw new InvalidDataException("Invalid ISO directory.");
            iso.Position = rootAt;
            byte[] root = ReadExact(iso, (int)rootLength);
            for (int at = 0; at < root.Length;) {
                int size = root[at];
                if (size == 0) { at = (at / 2048 + 1) * 2048; continue; }
                if (size < 34 || at + size > root.Length || root[at + 32] > size - 33) throw new InvalidDataException("Invalid ISO record.");
                string name = Encoding.ASCII.GetString(root, at + 33, root[at + 32]);
                if (name == "SLUS_216.42;1" || name == "SLUS_216.42") {
                    uint length = BitConverter.ToUInt32(root, at + 10);
                    long offset = (long)BitConverter.ToUInt32(root, at + 2) * 2048;
                    if (length != 5998128 || offset + length > iso.Length) throw new InvalidDataException("Unsupported game revision / Edição incompatível.");
                    iso.Position = offset;
                    byte[] elf = ReadExact(iso, (int)length);
                    if (Hash(elf) != ExpectedElf) throw new InvalidDataException("Unsupported game executable. Use the USA PS2 SLUS_216.42 release / Use a edição USA de PS2 compatível.");
                    return elf;
                }
                at += size;
            }
            throw new InvalidDataException("SLUS_216.42 not found. Select Sonic Riders Zero Gravity USA for PS2 / ISO incompatível.");
        }
    }

    static string Quote(string path) {
        // These arguments are complete file paths, never directories ending in backslash.
        if (path.IndexOf('"') >= 0) throw new ArgumentException("Invalid path.");
        return "\"" + path + "\"";
    }

    string InstalledDirectory() {
        string pointer = Path.Combine(saveRoot, "installed.txt");
        if (!File.Exists(pointer)) return null;
        try {
            string name = File.ReadAllText(pointer).Trim();
            Guid installId;
            if (!name.StartsWith("install-", StringComparison.Ordinal) || !Guid.TryParseExact(name.Substring(8), "N", out installId)) return null;
            string dir = Path.Combine(saveRoot, name);
            string[] receipt = File.ReadAllLines(Path.Combine(dir, "ready.txt"));
            long size;
            if (receipt.Length != 2 || !Int64.TryParse(receipt[0], out size) || size <= 0 || receipt[1].Length != 64) return null;
            if (new FileInfo(Path.Combine(dir, "game.dat")).Length != size) return null;
            if (Hash(File.ReadAllBytes(Path.Combine(dir, "SLUS_216.42"))) != ExpectedElf) return null;
            ExtractElf(Path.Combine(dir, "game.dat"));
            return dir;
        } catch (IOException) { return null; } catch (UnauthorizedAccessException) { return null; }
    }

    static void AtomicText(string path, string value) {
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temporary, value, new UTF8Encoding(false));
        if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
    }

    string Install(string source, IProgress<int> report, CancellationToken token) {
        byte[] elf = ExtractElf(source);
        long size = new FileInfo(source).Length;
        var drive = new DriveInfo(Path.GetPathRoot(saveRoot));
        if (drive.AvailableFreeSpace < size + 100L * 1024 * 1024) throw new IOException("São necessários aproximadamente 5 GB livres para importar o jogo.");
        string name = "install-" + Guid.NewGuid().ToString("N");
        string dir = Path.Combine(saveRoot, name);
        Directory.CreateDirectory(dir);
        bool committed = false;
        try {
            string data = Path.Combine(dir, "game.dat");
            string digest;
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var output = new FileStream(data, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var sha = SHA256.Create()) {
                byte[] buffer = new byte[1024 * 1024]; long done = 0; int count;
                while ((count = input.Read(buffer, 0, buffer.Length)) > 0) {
                    token.ThrowIfCancellationRequested();
                    output.Write(buffer, 0, count); sha.TransformBlock(buffer, 0, count, buffer, 0);
                    done += count; report.Report((int)(done * 100 / size));
                }
                if (done != size) throw new IOException("A ISO mudou durante a importação. Tente novamente.");
                sha.TransformFinalBlock(new byte[0], 0, 0); output.Flush(true);
                digest = BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
            }
            token.ThrowIfCancellationRequested();
            report.Report(100);
            using (var verify = File.OpenRead(data))
            using (var sha = SHA256.Create()) {
                string actual = BitConverter.ToString(sha.ComputeHash(verify)).Replace("-", "").ToLowerInvariant();
                if (actual != digest) throw new IOException("Falha ao verificar a cópia local. Importe novamente.");
            }
            token.ThrowIfCancellationRequested();
            ExtractElf(data);
            File.WriteAllBytes(Path.Combine(dir, "SLUS_216.42"), elf);
            File.WriteAllText(Path.Combine(dir, "ready.txt"), size + "\n" + digest);
            AtomicText(Path.Combine(saveRoot, "installed.txt"), name);
            committed = true;
            return dir;
        } finally {
            // Only this freshly created, application-owned staging directory is removed.
            if (!committed && Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    async Task StartGame() {
        if (busy) return;
        busy = true; browse.Enabled = play.Enabled = false;
        try {
            string engineDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "engine");
            string engine = Path.Combine(engineDir, "SonicRidersZeroGravity.exe");
            if (!File.Exists(engine)) throw new FileNotFoundException("Extract the entire ZIP before playing / Extraia o ZIP inteiro antes de jogar.");
            string imported = InstalledDirectory();
            if (imported == null) {
                if (String.IsNullOrWhiteSpace(isoPath.Text)) throw new InvalidOperationException("Selecione sua ISO antes de clicar em Jogar.");
                string source = Path.GetFullPath(isoPath.Text);
                if (!File.Exists(source)) throw new FileNotFoundException("ISO não encontrada. Selecione o arquivo novamente.");
                status.Text = "Verificando e importando ISO…";
                cancellation = new CancellationTokenSource(); cancel.Enabled = true;
                var report = new Progress<int>(delegate(int percent) { progress.Value = percent; status.Text = "Importando dados: " + percent + "%"; });
                imported = await Task.Run(delegate { return Install(source, report, cancellation.Token); });
                cancel.Enabled = false;
                AtomicText(Path.Combine(saveRoot, "iso-path.txt"), source);
            }
            string iso = Path.Combine(imported, "game.dat");
            string elfPath = Path.Combine(imported, "SLUS_216.42");
            // Validate the locally installed executable before every launch.
            await Task.Run(delegate {
                ExtractElf(iso);
                string card = Path.Combine(saveRoot, "Mcd001.ps2");
                string backup = Path.Combine(saveRoot, "Mcd001-before-unlock.ps2");
                if (File.Exists(card) && !File.Exists(backup)) File.Copy(card, backup);
            });
            var start = new ProcessStartInfo(engine) {
                Arguments = "--diagnostic " + Quote(elfPath) + " --disc " + Quote(iso) + " --memcard " + Quote(Path.Combine(saveRoot, "Mcd001.ps2")) + " --window --ntsc",
                WorkingDirectory = engineDir, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            // Keep developer profiling overrides out of normal play.
            foreach (string key in new string[] { "PS2_TIME", "PS2_ONE_THREAD", "PS2_NO_VU_THREAD", "PS2_BENCH", "PS2_SDL_RENDERER", "PS2_VU_INTERPRET", "PS2_VU_RECORD" }) start.EnvironmentVariables.Remove(key);
            int result = 0;
            using (var log = new StreamWriter(Path.Combine(saveRoot, "game.log"), false, new UTF8Encoding(false)))
            using (var process = new Process { StartInfo = start }) {
                object logLock = new object();
                DataReceivedEventHandler record = delegate(object sender, DataReceivedEventArgs e) { if (e.Data != null) lock (logLock) { log.WriteLine(e.Data); log.Flush(); } };
                process.OutputDataReceived += record; process.ErrorDataReceived += record;
                process.Start(); process.BeginOutputReadLine(); process.BeginErrorReadLine();
                Hide();
                await Task.Run(delegate { process.WaitForExit(); result = process.ExitCode; });
            }
            Show(); Activate();
            status.Text = result == 0 ? "Game closed / Jogo encerrado." : "Game exited with error. Log: " + Path.Combine(saveRoot, "game.log");
        } catch (OperationCanceledException) {
            Show(); status.Text = "Importação cancelada. Nenhum save foi alterado.";
        } catch (Exception e) {
            Show(); status.Text = "Unable to start / Não foi possível iniciar.";
            MessageBox.Show(this, e.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        } finally { busy = false; browse.Enabled = play.Enabled = true; cancel.Enabled = false; if (cancellation != null) { cancellation.Dispose(); cancellation = null; } }
    }
}
