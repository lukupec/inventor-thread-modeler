////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Minimal file logger for the add-in (no third-party dependency).
//
// Inventor and other add-ins (e.g. Vault) load their own versions of common libraries such as log4net
// into the same process; an add-in that depends on a different version can then fail to load silently.
// This logger only uses the .NET base library.
//
// Log file: %LOCALAPPDATA%\coolOrange\ThreadModeler\threadModeler.log (rolled over at 1 MB)
// Also written to the debugger output (Sysinternals DebugView).
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ThreadModeler.Addin
{
    internal static class AddinLog
    {
        private const long MaxSize = 1024 * 1024;
        private static readonly object Sync = new object();

        public static readonly string Directory = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "coolOrange", "ThreadModeler");

        public static readonly string Path = System.IO.Path.Combine(Directory, "threadModeler.log");

        public static void Debug(string message) { Write("DEBUG", message, null); }
        public static void Info(string message) { Write("INFO ", message, null); }
        public static void Warn(string message, Exception ex = null) { Write("WARN ", message, ex); }
        public static void Error(string message, Exception ex = null) { Write("ERROR", message, ex); }

        private static void Write(string level, string message, Exception ex)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                  .Append(" [").Append(Environment.CurrentManagedThreadId).Append("] ")
                  .Append(level).Append(' ').Append(message);
                if (ex != null)
                    sb.AppendLine().Append(ex);
                string line = sb.ToString();

                Trace.WriteLine("ThreadModeler: " + line);

                lock (Sync)
                {
                    System.IO.Directory.CreateDirectory(Directory);
                    var fi = new FileInfo(Path);
                    if (fi.Exists && fi.Length > MaxSize)
                    {
                        string old = System.IO.Path.ChangeExtension(Path, ".1.log");
                        File.Delete(old);
                        File.Move(Path, old);
                    }
                    File.AppendAllText(Path, line + Environment.NewLine);
                }
            }
            catch (Exception)
            {
                // Logging must never break the add-in.
            }
        }
    }
}
