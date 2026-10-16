using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Windows.Forms;

internal static class DownloadComplete
{
    const string Expected = "61d5952de74a77b6501c7564358cbcf4da28fb25de394ed6c5959e3c203c751d";
    const long Total = 2663296744;
    const string BaseUrl = "https://github.com/Ukinhas/sonic-riders-zero-gravity-windows/releases/download/v0.1.2-completo/";
    [STAThread] static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Installer());
    }
    sealed class Installer : Form
    {
        readonly Label label = new Label { Left = 20, Top = 20, Width = 480, Height = 75,
            Text = "Sonic Riders Zero Gravity - Windows\nBaixa 2,66 GB e prepara o EXE completo. Sem ISO ou Python.\nReserve 8 GB livres (EXE e dados da primeira abertura)." };
        readonly ProgressBar progress = new ProgressBar { Left = 20, Top = 100, Width = 480 };
        readonly Button install = new Button { Left = 20, Top = 145, Width = 220, Text = "Escolher pasta e instalar" };
        bool busy;
        public Installer()
        {
            Text = "Instalar Sonic Riders Zero Gravity";
            ClientSize = new System.Drawing.Size(520, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Controls.AddRange(new Control[] { label, progress, install });
            install.Click += Install;
            FormClosing += delegate(object sender, FormClosingEventArgs e) {
                if (busy) { e.Cancel = true; MessageBox.Show(this, "Aguarde o download e a verificacao terminarem."); }
            };
        }
        void Install(object sender, EventArgs e)
        {
            string folder;
            using (var picker = new FolderBrowserDialog { Description = "Escolha onde salvar o EXE do jogo", ShowNewFolderButton = true }) {
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                folder = picker.SelectedPath;
            }
            string output = Path.Combine(folder, "SonicRidersZeroGravity-Completo.exe");
            busy = true; install.Enabled = false;
            var worker = new BackgroundWorker { WorkerReportsProgress = true };
            worker.ProgressChanged += delegate(object s, ProgressChangedEventArgs p) {
                progress.Value = p.ProgressPercentage;
                label.Text = (string)p.UserState;
            };
            worker.DoWork += delegate {
                if (File.Exists(output)) {
                    if (Hash(output) == Expected) return;
                    throw new IOException("Ja existe um EXE diferente nessa pasta. Escolha outra pasta para preservar esse arquivo.");
                }
                string temporary = Path.Combine(folder, ".sonic-download-" + Guid.NewGuid().ToString("N") + ".tmp");
                try {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    long received = 0;
                    var buffer = new byte[1024 * 1024];
                    using (var destination = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                        for (int part = 1; part <= 2; part++) {
                            var request = (HttpWebRequest)WebRequest.Create(BaseUrl + "SonicRidersZeroGravity-Completo.part" + part);
                            request.UserAgent = "SonicWindowsInstaller/0.1.2";
                            request.Timeout = 60000;
                            request.ReadWriteTimeout = 120000;
                            using (var response = request.GetResponse())
                            using (var source = response.GetResponseStream()) {
                                long partBytes = 0;
                                int count, lastPercent = -1;
                                while ((count = source.Read(buffer, 0, buffer.Length)) > 0) {
                                    partBytes += count; received += count;
                                    if (received > Total) throw new InvalidDataException("Download maior que o esperado.");
                                    destination.Write(buffer, 0, count);
                                    int percent = (int)(received * 100 / Total);
                                    if (percent != lastPercent) {
                                        lastPercent = percent;
                                        worker.ReportProgress(percent, "Baixando o jogo: " + percent + "%\nMantenha a conexao com a internet.\nParte " + part + " de 2.");
                                    }
                                }
                                long expectedPart = part == 1 ? 1500000000L : Total - 1500000000L;
                                if (partBytes != expectedPart) throw new InvalidDataException("Parte incompleta. Tente novamente.");
                            }
                        }
                    }
                    worker.ReportProgress(100, "Verificando a integridade do EXE completo...");
                    if (new FileInfo(temporary).Length != Total || Hash(temporary) != Expected)
                        throw new InvalidDataException("A verificacao SHA-256 falhou. Tente novamente.");
                    File.Move(temporary, output);
                } finally { if (File.Exists(temporary)) File.Delete(temporary); }
            };
            worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs result) {
                busy = false; install.Enabled = true;
                if (result.Error != null) {
                    label.Text = "Instalacao nao concluida. Voce pode tentar novamente.";
                    MessageBox.Show(this, result.Error.Message, "Falha na instalacao", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                label.Text = "Pronto! Abra SonicRidersZeroGravity-Completo.exe para jogar.\nA primeira abertura prepara os dados locais (cerca de 5 GB).\nNas proximas vezes, o instalador nao sera necessario.";
                if (MessageBox.Show(this, "Instalado em:\n" + output + "\n\nAbrir o jogo agora?", "Concluido", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                    try { Process.Start(new ProcessStartInfo(output) { UseShellExecute = true }); }
                    catch (Exception error) { MessageBox.Show(this, error.Message); }
                }
            };
            worker.RunWorkerAsync();
        }
        static string Hash(string path)
        {
            using (var sha = SHA256.Create())
            using (var input = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
        }
    }
}
