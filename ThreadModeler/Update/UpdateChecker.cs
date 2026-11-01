////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// ThreadModeler update check - runs inside Inventor.
//
// Network calls run on a background task; their results are picked up on Inventor's UI thread by a
// WinForms timer (Inventor pumps its messages), so no Inventor COM call or UI is touched off-thread.
// The update is installed after Inventor exits (msiexec /passive), so Inventor is never closed with
// unsaved work.
//
// Disable for all users:  HKLM\SOFTWARE\coolOrange\ThreadModeler  DisableUpdateCheck = 1 (DWORD)
//   (the MSI writes this with DISABLEUPDATECHECK=1). Per user: HKCU, same value.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using ThreadModeler.Addin;
using ThreadModeler.Utilities;

namespace ThreadModeler.Update
{
    internal sealed class UpdateChecker : IDisposable
    {
        private const string PolicyKey = @"SOFTWARE\coolOrange\ThreadModeler";
        private const string PolicyValue = "DisableUpdateCheck";
        private const string Caption = "ThreadModeler update";

        /// <summary>Delay after Inventor start before the automatic check, to keep startup fast.</summary>
        private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(20);

        private static readonly HttpClient Http = CreateHttpClient();

        private readonly Inventor.Application _app;
        private readonly Version _installed;
        private readonly string _stateFile;

        private Timer _timer;
        private Task<ReleaseInfo> _checkTask;
        private Task<string> _downloadTask;
        private ReleaseInfo _downloading;
        private bool _manual;
        private bool _installScheduled;

        public static UpdateChecker Instance { get; private set; }

        public static Version InstalledVersion
        {
            get { return typeof(UpdateChecker).Assembly.GetName().Version; }
        }

        private UpdateChecker(Inventor.Application app)
        {
            _app = app;
            _installed = InstalledVersion;
            _stateFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "coolOrange", "ThreadModeler", "update.json");
        }

        /// <summary>Creates the checker (call from Activate, on Inventor's UI thread) and schedules the automatic check.</summary>
        public static void Start(Inventor.Application app)
        {
            Instance = new UpdateChecker(app);

            if (IsDisabledByPolicy())
            {
                AddinLog.Info("Automatic update check disabled by policy (" + PolicyKey + "\\" + PolicyValue + ").");
                return;
            }

            if (!UpdateLogic.ShouldCheckAutomatically(Instance.LoadState(), DateTime.UtcNow))
                return;

            Instance.ScheduleCheck(manual: false, delay: StartupDelay);
        }

        public static void Stop()
        {
            if (Instance != null)
            {
                Instance.Dispose();
                Instance = null;
            }
        }

        /// <summary>"Check for Updates" command: checks now, ignores throttling and skipped versions.</summary>
        public void CheckNow()
        {
            if (_installScheduled)
            {
                Info("The update has already been downloaded.\n\nIt will be installed after you close Inventor.");
                return;
            }
            if (_checkTask != null || _downloadTask != null)
                return; // already running

            ScheduleCheck(manual: true, delay: TimeSpan.Zero);
        }

        public void Dispose()
        {
            StopTimer();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        // Check

        private void ScheduleCheck(bool manual, TimeSpan delay)
        {
            _manual = manual;
            StopTimer();

            if (delay <= TimeSpan.Zero)
            {
                _checkTask = Task.Run(FetchLatestReleaseAsync);
                StartTimer(250, OnCheckTick);
            }
            else
            {
                StartTimer((int)delay.TotalMilliseconds, (s, e) =>
                {
                    StopTimer();
                    _checkTask = Task.Run(FetchLatestReleaseAsync);
                    StartTimer(250, OnCheckTick);
                });
            }
        }

        private static async Task<ReleaseInfo> FetchLatestReleaseAsync()
        {
            using (HttpResponseMessage response = await Http.GetAsync(UpdateLogic.LatestReleaseApiUrl).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null; // no release published yet

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return UpdateLogic.ParseLatestRelease(json);
            }
        }

        private void OnCheckTick(object sender, EventArgs e)
        {
            if (_checkTask == null || !_checkTask.IsCompleted)
                return;

            StopTimer();
            Task<ReleaseInfo> task = _checkTask;
            _checkTask = null;

            if (task.IsFaulted || task.IsCanceled)
            {
                Exception ex = task.Exception != null ? task.Exception.GetBaseException() : null;
                AddinLog.Warn("Update check failed.", ex);
                if (_manual)
                    Error("Could not check for updates.\n\n" + (ex != null ? ex.Message : "The request timed out."));
                return;
            }

            UpdateState state = LoadState();
            state.LastCheckUtc = DateTime.UtcNow;
            SaveState(state);

            ReleaseInfo release = task.Result;
            if (!UpdateLogic.ShouldOffer(release, _installed, state, _manual))
            {
                if (_manual)
                    Info("You have the latest version of ThreadModeler (" + Format(_installed) + ").");
                return;
            }

            Offer(release, state);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        // Offer / download / install

        private void Offer(ReleaseInfo release, UpdateState state)
        {
            UpdateChoice choice;
            using (var form = new UpdateForm(release, _installed))
                choice = form.ShowAndGetChoice(Owner);

            switch (choice)
            {
                case UpdateChoice.Install:
                    StartDownload(release);
                    break;
                case UpdateChoice.Skip:
                    state.SkippedVersion = Format(release.Version);
                    SaveState(state);
                    break;
                default:
                    break; // remind later: the next automatic check offers it again
            }
        }

        private void StartDownload(ReleaseInfo release)
        {
            _downloading = release;
            SetStatus("Downloading ThreadModeler " + Format(release.Version) + "...");
            _downloadTask = Task.Run(() => DownloadAndVerifyAsync(release));
            StartTimer(250, OnDownloadTick);
        }

        private static async Task<string> DownloadAndVerifyAsync(ReleaseInfo release)
        {
            string dir = Path.Combine(Path.GetTempPath(), "ThreadModelerUpdate");
            Directory.CreateDirectory(dir);
            string msiPath = Path.Combine(dir, Path.GetFileName(release.MsiName));

            using (HttpResponseMessage response = await Http.GetAsync(release.MsiUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                using (Stream source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (FileStream target = File.Create(msiPath))
                    await source.CopyToAsync(target).ConfigureAwait(false);
            }

            if (release.Sha256Url == null)
                throw new InvalidOperationException("The release has no checksum file (" + release.MsiName + ".sha256).");

            string expected = UpdateLogic.ParseSha256File(
                await Http.GetStringAsync(release.Sha256Url).ConfigureAwait(false));

            string actual;
            using (FileStream fs = File.OpenRead(msiPath))
                actual = UpdateLogic.ComputeSha256(fs);

            if (expected == null || !string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(msiPath);
                throw new InvalidOperationException("The downloaded installer failed the SHA-256 check and was deleted.");
            }

            return msiPath;
        }

        private void OnDownloadTick(object sender, EventArgs e)
        {
            if (_downloadTask == null || !_downloadTask.IsCompleted)
                return;

            StopTimer();
            Task<string> task = _downloadTask;
            ReleaseInfo release = _downloading;
            _downloadTask = null;
            _downloading = null;
            SetStatus(string.Empty);

            if (task.IsFaulted || task.IsCanceled)
            {
                Exception ex = task.Exception != null ? task.Exception.GetBaseException() : null;
                AddinLog.Error("Update download failed.", ex);
                Error("The update could not be downloaded.\n\n" + (ex != null ? ex.Message : "The request timed out.") +
                      "\n\nYou can download it manually from:\n" + release.PageUrl);
                return;
            }

            try
            {
                ScheduleInstallAfterExit(task.Result);
                _installScheduled = true;
                Info("ThreadModeler " + Format(release.Version) + " has been downloaded.\n\n" +
                     "It will be installed automatically after you close Inventor " +
                     "(Windows will ask for administrator permission).");
            }
            catch (Exception ex)
            {
                AddinLog.Error("Could not schedule the update.", ex);
                Error("The update could not be scheduled.\n\n" + ex.Message +
                      "\n\nYou can install it manually: " + task.Result);
            }
        }

        /// <summary>Starts a hidden PowerShell process that waits for this Inventor process to exit, then runs the MSI.</summary>
        private static void ScheduleInstallAfterExit(string msiPath)
        {
            int pid = Process.GetCurrentProcess().Id;
            string msi = msiPath.Replace("'", "''");
            string script =
                "Wait-Process -Id " + pid + " -ErrorAction SilentlyContinue; " +
                "Start-Process -FilePath msiexec.exe -ArgumentList @('/i', '\"" + msi + "\"', '/passive', '/norestart')";

            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"),
                Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand " + encoded,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////
        // Helpers

        public static bool IsDisabledByPolicy()
        {
            return ReadPolicy(Registry.LocalMachine) || ReadPolicy(Registry.CurrentUser);
        }

        private static bool ReadPolicy(RegistryKey hive)
        {
            try
            {
                using (RegistryKey key = hive.OpenSubKey(PolicyKey))
                {
                    object value = key != null ? key.GetValue(PolicyValue) : null;
                    return value != null && Convert.ToInt32(value) != 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private UpdateState LoadState()
        {
            try
            {
                return System.IO.File.Exists(_stateFile)
                    ? UpdateLogic.DeserializeState(System.IO.File.ReadAllText(_stateFile))
                    : new UpdateState();
            }
            catch (Exception ex)
            {
                AddinLog.Warn("Could not read " + _stateFile, ex);
                return new UpdateState();
            }
        }

        private void SaveState(UpdateState state)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_stateFile));
                System.IO.File.WriteAllText(_stateFile, UpdateLogic.SerializeState(state));
            }
            catch (Exception ex)
            {
                AddinLog.Warn("Could not write " + _stateFile, ex);
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ThreadModeler-Inventor-AddIn/" + Format(InstalledVersion));
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        private void StartTimer(int intervalMs, EventHandler handler)
        {
            _timer = new Timer { Interval = Math.Max(intervalMs, 1) };
            _timer.Tick += handler;
            _timer.Start();
        }

        private void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }

        private IWin32Window Owner
        {
            get
            {
                try { return new WindowWrapper((IntPtr)_app.MainFrameHWND); }
                catch (Exception) { return null; }
            }
        }

        private void SetStatus(string text)
        {
            try { _app.StatusBarText = text; }
            catch (Exception) { }
        }

        private void Info(string text)
        {
            MessageBox.Show(Owner, text, Caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Error(string text)
        {
            MessageBox.Show(Owner, text, Caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        internal static string Format(Version v)
        {
            return UpdateLogic.Normalize(v).ToString(3);
        }
    }
}
