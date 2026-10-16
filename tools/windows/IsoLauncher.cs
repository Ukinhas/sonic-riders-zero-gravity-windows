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
    private static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        bool created;
        using (var gate = new Mutex(true, "Local\\SonicRidersZeroGravityNative", out created)) {
            if (!created) {
                MessageBox.Show("Sonic Riders Zero Gravity is already running.");
                return;
            }
            try { Application.Run(new ImportWindow()); }
            catch (Exception e) { MessageBox.Show(e.Message, "Sonic Riders Zero Gravity"); }
            finally { gate.ReleaseMutex(); }
        }
    }
}

internal sealed class ImportWindow : Form {
    const string ExpectedElf = "43ff74931c70111c7332875653aa47a3db62a0c513e71c8cf48d1d0649de16ee";
    readonly string saveRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SonicRidersZeroGravity");
    readonly TextBox isoPath = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
    readonly Button browse = new Button { Text = "Select ISO / Selecionar ISO", AutoSize = true };
    readonly Button play = new Button { Text = "Play / Jogar", AutoSize = true };
    readonly Label status = new Label { Text = "Select your USA PS2 ISO (SLUS_216.42).", AutoSize = true, MaximumSize = new Size(590, 0) };
    bool busy;

    public ImportWindow() {
        Text = "Sonic Riders: Zero Gravity — Windows";
        ClientSize = new Size(640, 310);
        MinimumSize = new Size(600, 350);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 6 };
        layout.Controls.Add(new Label { Text = "SONIC RIDERS: ZERO GRAVITY", Font = new Font("Segoe UI", 16, FontStyle.Bold), AutoSize = true });
        layout.Controls.Add(new Label { Text = "Unofficial Windows build / Versão Windows não oficial\nSelecione sua ISO USA. Mantenha o arquivo no local escolhido.", AutoSize = true });
        layout.Controls.Add(isoPath);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.Add(browse); buttons.Controls.Add(play);
        layout.Controls.Add(buttons);
        layout.Controls.Add(status);
        layout.Controls.Add(new Label { Text = "WASD: mover · K/L/I/J: X/bola/triângulo/quadrado\nU/O: L1/R1 · Enter: Start · Setas: menus · F11: tela cheia", AutoSize = true });
        Controls.Add(layout);
        Directory.CreateDirectory(saveRoot);
        string settings = Path.Combine(saveRoot, "iso-path.txt");
        if (File.Exists(settings)) isoPath.Text = File.ReadAllText(settings, Encoding.UTF8).Trim();
        browse.Click += delegate {
            using (var dialog = new OpenFileDialog { Filter = "PS2 ISO (*.iso)|*.iso", Title = "Select Sonic Riders Zero Gravity USA ISO", CheckFileExists = true }) {
                if (dialog.ShowDialog(this) == DialogResult.OK) { isoPath.Text = dialog.FileName; status.Text = "Press Play to validate and start / Clique Jogar para iniciar."; }
            }
        };
        play.Click += async delegate { await StartGame(); };
        FormClosing += delegate(object sender, FormClosingEventArgs e) { if (busy) e.Cancel = true; };
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

    async Task StartGame() {
        if (busy) return;
        busy = true; browse.Enabled = play.Enabled = false;
        try {
            string iso = Path.GetFullPath(isoPath.Text);
            if (!File.Exists(iso)) throw new FileNotFoundException("ISO not found. Select it again / Selecione uma ISO existente.");
            string engineDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "engine");
            string engine = Path.Combine(engineDir, "SonicRidersZeroGravity.exe");
            if (!File.Exists(engine)) throw new FileNotFoundException("Extract the entire ZIP before playing / Extraia o ZIP inteiro antes de jogar.");
            status.Text = "Validating ISO / Verificando ISO…";
            string imported = Path.Combine(saveRoot, "imported");
            Directory.CreateDirectory(imported);
            string elfPath = Path.Combine(imported, "SLUS_216.42");
            await Task.Run(delegate {
                byte[] elf = ExtractElf(iso);
                File.WriteAllBytes(elfPath, elf);
                File.WriteAllText(Path.Combine(saveRoot, "iso-path.txt"), iso, new UTF8Encoding(false));
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
        } catch (Exception e) {
            Show(); status.Text = "Unable to start / Não foi possível iniciar.";
            MessageBox.Show(this, e.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        } finally { busy = false; browse.Enabled = play.Enabled = true; }
    }
}
