using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RewindLauncher
{
    public partial class MainWindow : Window
    {
        // Параметры подключения к БД (должны совпадать с docker-compose.yml и AppDbContext.cs)
        private const string DbHost = "localhost";
        private const int DbPort = 5433;

        // Имя исполняемого файла основного приложения. Лежит рядом с лаунчером.
        private const string AppExeName = "Rewind.exe";

        // docker-compose.yml кладётся в подпапку 'db' установщика, рядом с лаунчером.
        private const string ComposeRelativePath = "db\\docker-compose.yml";

        // Заранее экспортированный образ postgres (для офлайн-старта). При первом
        // запуске лаунчер делает `docker load`, если такого образа на машине нет.
        private const string PostgresImageRef = "postgres:17-alpine";
        private const string PostgresImageTar = "db\\postgres-17-alpine.tar";

        // Лог запуска — пишется в %LOCALAPPDATA%\Rewind\launcher.log
        private string _logPath = "";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += async (_, __) => await RunAsync();
        }

        private void Log(string line)
        {
            try
            {
                if (string.IsNullOrEmpty(_logPath))
                {
                    var dir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Rewind");
                    Directory.CreateDirectory(dir);
                    _logPath = Path.Combine(dir, "launcher.log");
                    File.WriteAllText(_logPath,
                        $"--- Rewind launcher {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---\r\n");
                }
                File.AppendAllText(_logPath,
                    $"[{DateTime.Now:HH:mm:ss}] {line}\r\n");
            }
            catch { /* ignore */ }
        }

        private void SetStatus(string text)
        {
            Dispatcher.Invoke(() => StatusText.Text = text);
        }

        private async Task RunAsync()
        {
            try
            {
                // Для single-file публикации AppContext.BaseDirectory может указывать
                // на временную папку распаковки, поэтому берём директорию реального .exe.
                var baseDir = Path.GetDirectoryName(Environment.ProcessPath)
                              ?? AppContext.BaseDirectory;
                var composePath = Path.Combine(baseDir, ComposeRelativePath);
                var appPath = Path.Combine(baseDir, AppExeName);

                if (!File.Exists(composePath))
                    throw new FileNotFoundException(
                        $"Не найден docker-compose.yml по пути:\n{composePath}", composePath);
                if (!File.Exists(appPath))
                    throw new FileNotFoundException(
                        $"Не найден Rewind.exe по пути:\n{appPath}", appPath);

                Log($"BaseDir = {baseDir}");
                Log($"Compose = {composePath}");

                SetStatus("Проверка Docker...");
                var dockerErr = await EnsureDockerRunningAsync();
                if (dockerErr != null)
                {
                    Fatal("Не удалось подключиться к Docker Desktop.\n\n" + dockerErr +
                          "\n\nЧастые причины:\n" +
                          "• Docker Desktop ещё не запустился (подождите минуту и запустите снова)\n" +
                          "• После установки Docker нужно выйти из системы и зайти заново\n" +
                          "• Не настроен WSL 2");
                    return;
                }

                SetStatus("Подготовка образа PostgreSQL...");
                var imageErr = await EnsurePostgresImageAsync(
                    Path.Combine(baseDir, PostgresImageTar));
                if (imageErr != null)
                {
                    Fatal("Не удалось подготовить образ PostgreSQL.\n\n" + imageErr);
                    return;
                }

                SetStatus("Запуск базы данных...");
                var composeErr = await ComposeUpAsync(composePath);
                if (composeErr != null)
                {
                    Fatal("Не удалось поднять контейнер с базой данных.\n\n" +
                          "Вывод docker compose:\n" + composeErr);
                    return;
                }

                SetStatus("Ожидание готовности базы данных...");
                if (!await WaitForPortAsync(DbHost, DbPort, TimeSpan.FromSeconds(120)))
                {
                    Fatal($"База данных не отвечает на {DbHost}:{DbPort} (тайм-аут 120с).");
                    return;
                }

                SetStatus("Запуск Rewind...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = appPath,
                    WorkingDirectory = baseDir,
                    UseShellExecute = false
                });

                // Даём приложению немного подняться, затем закрываем лаунчер.
                await Task.Delay(1500);
                Dispatcher.Invoke(Close);
            }
            catch (Exception ex)
            {
                Fatal("Ошибка при запуске Rewind:\n\n" + ex.Message);
            }
        }

        private void Fatal(string message)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(this, message, "Rewind", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            });
        }

        // --- Docker -----------------------------------------------------------
        // Возвращают null при успехе или текст ошибки при провале.

        private async Task<string?> EnsureDockerRunningAsync()
        {
            // Если уже работает — выходим сразу.
            var info = await DockerInfoAsync();
            if (info.Code == 0)
            {
                Log("docker info: OK (already running)");
                return null;
            }
            Log($"docker info initial: code={info.Code}\nstdout: {info.StdOut}\nstderr: {info.StdErr}");

            // Пробуем запустить Docker Desktop (уже установленный).
            // ВНИМАНИЕ: мы НИКОГДА сами не запускаем DockerDesktopInstaller.exe.
            // Если Docker не установлен — даём пользователю чёткую инструкцию и выходим.
            string? dockerDesktop = FindDockerDesktopExe();
            if (dockerDesktop != null && File.Exists(dockerDesktop))
            {
                Log($"Starting Docker Desktop: {dockerDesktop}");
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = dockerDesktop,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex) { Log("Start failed: " + ex.Message); }
            }
            else
            {
                Log("Docker Desktop.exe не найден в стандартных путях.");
                var baseDir = Path.GetDirectoryName(Environment.ProcessPath)
                              ?? AppContext.BaseDirectory;
                var bundled = Path.Combine(baseDir, "db", "DockerDesktopInstaller.exe");
                return
                    "Docker Desktop не установлен на этом компьютере.\n\n" +
                    "Rewind использует Docker для запуска базы данных PostgreSQL.\n\n" +
                    "Что делать:\n" +
                    "1) Откройте папку: " + Path.Combine(baseDir, "db") + "\n" +
                    "2) Запустите DockerDesktopInstaller.exe ВРУЧНУЮ.\n" +
                    "3) После установки Docker Desktop перезагрузите компьютер.\n" +
                    "4) Запустите Rewind с ярлыка на рабочем столе.\n\n" +
                    "Встроенный установщик: " + bundled +
                    (File.Exists(bundled) ? "" : "\n(файл не найден — переустановите Rewind)");
            }

            // Ждём до 3 минут пока демон оживёт
            var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(3);
            (int Code, string StdOut, string StdErr) last = info;
            while (DateTime.UtcNow < deadline)
            {
                last = await DockerInfoAsync();
                if (last.Code == 0)
                {
                    Log("docker info: OK");
                    return null;
                }
                await Task.Delay(3000);
            }
            return BuildErrorText("docker info", last);
        }

        private static string? FindDockerDesktopExe()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Docker", "Docker", "Docker Desktop.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Docker", "Docker", "Docker Desktop.exe")
            };
            foreach (var c in candidates)
                if (File.Exists(c)) return c;
            return null;
        }

        private async Task<(int Code, string StdOut, string StdErr)> DockerInfoAsync()
        {
            return await RunProcessAsync("docker", "info", TimeSpan.FromSeconds(15));
        }

        private async Task<string?> EnsurePostgresImageAsync(string tarPath)
        {
            // 1) Проверяем, есть ли образ локально.
            var inspect = await RunProcessAsync(
                "docker", $"image inspect {PostgresImageRef}", TimeSpan.FromSeconds(20));
            if (inspect.Code == 0)
            {
                Log($"image {PostgresImageRef}: present locally");
                return null;
            }
            Log($"image {PostgresImageRef}: not present, will load from tar");

            // 2) Если есть встроенный tar — грузим из него.
            if (File.Exists(tarPath))
            {
                SetStatus("Загрузка образа PostgreSQL из локального файла (~80 МБ)...");
                var load = await RunProcessAsync(
                    "docker", $"load -i \"{tarPath}\"", TimeSpan.FromMinutes(5));
                Log($"docker load: code={load.Code}\nstdout: {load.StdOut}\nstderr: {load.StdErr}");
                if (load.Code == 0) return null;
                return BuildErrorText($"docker load -i \"{tarPath}\"", load);
            }

            // 3) Tar нет — пробуем docker pull (потребуется интернет).
            Log("tar not found, falling back to docker pull");
            SetStatus("Скачивание образа PostgreSQL из интернета...");
            var pull = await RunProcessAsync(
                "docker", $"pull {PostgresImageRef}", TimeSpan.FromMinutes(10));
            Log($"docker pull: code={pull.Code}\nstdout: {pull.StdOut}\nstderr: {pull.StdErr}");
            if (pull.Code == 0) return null;
            return BuildErrorText($"docker pull {PostgresImageRef}", pull);
        }

        private async Task<string?> ComposeUpAsync(string composePath)
        {
            // Используем фиксированное имя проекта, чтобы compose всегда работал
            // с одним и тем же набором ресурсов независимо от имени папки.
            var composeArgs = $"compose -p rewind -f \"{composePath}\" up -d";

            var res = await RunProcessAsync("docker", composeArgs, TimeSpan.FromMinutes(8));
            Log($"compose up: code={res.Code}\nstdout: {res.StdOut}\nstderr: {res.StdErr}");
            if (res.Code == 0) return null;

            // Типичная проблема: остался "висячий" контейнер с тем же именем
            // (например, от ручного docker compose up из репозитория).
            // Сносим его и повторяем — данные сохранятся в named volume rewind-pgdata.
            var combined = (res.StdErr + " " + res.StdOut).ToLowerInvariant();
            if (combined.Contains("is already in use") ||
                combined.Contains("conflict"))
            {
                Log("Container name conflict detected — forcibly removing rewind-postgres and retrying.");
                SetStatus("Удаляю старый контейнер rewind-postgres и повторяю...");

                var rm = await RunProcessAsync("docker", "rm -f rewind-postgres",
                    TimeSpan.FromSeconds(30));
                Log($"docker rm -f rewind-postgres: code={rm.Code}\nstderr: {rm.StdErr}");

                res = await RunProcessAsync("docker", composeArgs, TimeSpan.FromMinutes(8));
                Log($"compose up retry: code={res.Code}\nstdout: {res.StdOut}\nstderr: {res.StdErr}");
                if (res.Code == 0) return null;
            }

            return BuildErrorText($"docker {composeArgs}", res);
        }

        private static string BuildErrorText(string cmd,
            (int Code, string StdOut, string StdErr) r)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Команда: {cmd}");
            sb.AppendLine($"Код выхода: {r.Code}");
            if (!string.IsNullOrWhiteSpace(r.StdErr))
            {
                sb.AppendLine("STDERR:");
                sb.AppendLine(r.StdErr.Trim());
            }
            if (!string.IsNullOrWhiteSpace(r.StdOut))
            {
                sb.AppendLine("STDOUT:");
                sb.AppendLine(r.StdOut.Trim());
            }
            return sb.ToString();
        }

        // --- Утилиты ----------------------------------------------------------

        private static async Task<(int Code, string StdOut, string StdErr)> RunProcessAsync(
            string fileName, string args, TimeSpan timeout)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            try
            {
                using var p = Process.Start(psi);
                if (p == null) return (-1, "", "");
                var outTask = p.StandardOutput.ReadToEndAsync();
                var errTask = p.StandardError.ReadToEndAsync();
                using var cts = new CancellationTokenSource(timeout);
                try
                {
                    await p.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    try { p.Kill(true); } catch { }
                    return (-1, "", "timeout");
                }
                return (p.ExitCode, await outTask, await errTask);
            }
            catch (Exception ex)
            {
                return (-1, "", ex.Message);
            }
        }

        private static async Task<bool> WaitForPortAsync(string host, int port, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    using var client = new TcpClient();
                    var connectTask = client.ConnectAsync(host, port);
                    var completed = await Task.WhenAny(connectTask, Task.Delay(2000));
                    if (completed == connectTask && client.Connected) return true;
                }
                catch { /* ignore */ }
                await Task.Delay(1000);
            }
            return false;
        }
    }
}
