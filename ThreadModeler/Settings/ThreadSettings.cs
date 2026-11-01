////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// User settings: which thread profile template (.ipt) is used for each thread standard.
//
// Stored as JSON in %LOCALAPPDATA%\ThreadModeler\settings.json. No Inventor or WinForms types here, so the
// file is also compiled into the unit tests.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Text.Json;

namespace ThreadModeler.Settings
{
    /// <summary>Thread standards the user can pick in the ThreadModeler dialog.</summary>
    public enum ThreadStandard
    {
        ISO,
        BSW
    }

    public sealed class ThreadSettings
    {
        public static readonly string DefaultFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ThreadModeler", "settings.json");

        /// <summary>Configured ISO template; null or empty = bundled template.</summary>
        public string IsoTemplatePath { get; set; }

        /// <summary>Configured BSW template; null or empty = bundled template.</summary>
        public string BswTemplatePath { get; set; }

        /// <summary>Folder of the add-in assembly (contains "Thread Templates").</summary>
        public static string AddinDirectory
        {
            get { return Path.GetDirectoryName(typeof(ThreadSettings).Assembly.Location); }
        }

        public static string DisplayName(ThreadStandard standard)
        {
            return standard == ThreadStandard.ISO ? "ISO (metric, 60°)" : "BSW (Whitworth, 55°)";
        }

        /// <summary>Template shipped with the add-in, e.g. "&lt;addinDir&gt;\Thread Templates\ISO Template.ipt".</summary>
        public static string BundledTemplate(ThreadStandard standard, string addinDir)
        {
            return Path.Combine(addinDir, "Thread Templates", standard + " Template.ipt");
        }

        public string GetTemplatePath(ThreadStandard standard)
        {
            return standard == ThreadStandard.ISO ? IsoTemplatePath : BswTemplatePath;
        }

        public void SetTemplatePath(ThreadStandard standard, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                path = null;

            if (standard == ThreadStandard.ISO)
                IsoTemplatePath = path;
            else
                BswTemplatePath = path;
        }

        /// <summary>
        /// Template to use for a standard: the configured file if it exists, otherwise the bundled one
        /// (which may not exist either; the caller checks).
        /// </summary>
        public string ResolveTemplate(ThreadStandard standard, string addinDir)
        {
            string path = GetTemplatePath(standard);

            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                return path;

            return BundledTemplate(standard, addinDir);
        }

        public static string Serialize(ThreadSettings settings)
        {
            return JsonSerializer.Serialize(settings ?? new ThreadSettings(), new JsonSerializerOptions { WriteIndented = true });
        }

        public static ThreadSettings Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new ThreadSettings();
            try
            {
                return JsonSerializer.Deserialize<ThreadSettings>(json) ?? new ThreadSettings();
            }
            catch (JsonException)
            {
                return new ThreadSettings();
            }
        }

        /// <summary>Loads the settings; a missing or unreadable file gives the defaults.</summary>
        public static ThreadSettings Load(string file = null)
        {
            try
            {
                file = file ?? DefaultFile;
                return File.Exists(file) ? Deserialize(File.ReadAllText(file)) : new ThreadSettings();
            }
            catch (Exception)
            {
                return new ThreadSettings();
            }
        }

        /// <summary>Saves the settings. Returns false if the file could not be written.</summary>
        public bool Save(string file = null)
        {
            try
            {
                file = file ?? DefaultFile;
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllText(file, Serialize(this));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
