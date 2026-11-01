////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// ThreadModeler update check - pure logic (no Inventor / WinForms dependencies, unit tested in ThreadModeler.Tests).
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ThreadModeler.Update
{
    /// <summary>A published release that can be installed.</summary>
    public sealed class ReleaseInfo
    {
        public Version Version { get; set; }
        public string Tag { get; set; }
        public string PageUrl { get; set; }
        public string Notes { get; set; }
        public string MsiName { get; set; }
        public string MsiUrl { get; set; }
        public string Sha256Url { get; set; }
    }

    /// <summary>Per-user update state, stored as JSON in %LOCALAPPDATA%\ThreadModeler\update.json.</summary>
    public sealed class UpdateState
    {
        public DateTime? LastCheckUtc { get; set; }
        public string SkippedVersion { get; set; }
        public bool Disabled { get; set; }
    }

    public static class UpdateLogic
    {
        public const string RepoOwner = "lukupec";
        public const string RepoName = "inventor-thread-modeler";

        /// <summary>Project home page (About and Help link here).</summary>
        public static readonly string ProjectUrl =
            "https://github.com/" + RepoOwner + "/" + RepoName;

        public static readonly string LatestReleaseApiUrl =
            "https://api.github.com/repos/" + RepoOwner + "/" + RepoName + "/releases/latest";

        public static readonly string ReleasesPageUrl =
            "https://github.com/" + RepoOwner + "/" + RepoName + "/releases";

        /// <summary>Automatic checks run at most once per this interval.</summary>
        public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

        private static readonly Regex VersionPattern =
            new Regex(@"^\s*[vV]?(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:[-+].*)?\s*$", RegexOptions.CultureInvariant);

        private static readonly Regex Sha256Pattern =
            new Regex(@"\b([0-9a-fA-F]{64})\b", RegexOptions.CultureInvariant);

        /// <summary>Parses "v30.0.1", "30.0.1", "30.0.1.0" or "30.0.1-beta" into a 3-part version.</summary>
        public static bool TryParseVersion(string text, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            Match m = VersionPattern.Match(text);
            if (!m.Success)
                return false;

            int major = int.Parse(m.Groups[1].Value);
            int minor = int.Parse(m.Groups[2].Value);
            int build = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;
            version = new Version(major, minor, build);
            return true;
        }

        /// <summary>Major.Minor.Build only; the revision part is ignored.</summary>
        public static Version Normalize(Version v)
        {
            if (v == null)
                return new Version(0, 0, 0);
            return new Version(v.Major, Math.Max(v.Minor, 0), Math.Max(v.Build, 0));
        }

        public static bool IsNewer(Version candidate, Version installed)
        {
            return Normalize(candidate) > Normalize(installed);
        }

        /// <summary>
        /// Parses the GitHub "releases/latest" response. Returns null when the release is a draft or
        /// prerelease, has no parseable version tag, or carries no .msi asset.
        /// </summary>
        public static ReleaseInfo ParseLatestRelease(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                JsonElement root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return null;

                if (GetBool(root, "draft") || GetBool(root, "prerelease"))
                    return null;

                string tag = GetString(root, "tag_name");
                Version version;
                if (!TryParseVersion(tag, out version))
                    return null;

                var info = new ReleaseInfo
                {
                    Version = version,
                    Tag = tag,
                    PageUrl = GetString(root, "html_url") ?? ReleasesPageUrl,
                    Notes = GetString(root, "body") ?? string.Empty
                };

                JsonElement assets;
                if (root.TryGetProperty("assets", out assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement asset in assets.EnumerateArray())
                    {
                        string name = GetString(asset, "name");
                        string url = GetString(asset, "browser_download_url");
                        if (name == null || url == null)
                            continue;

                        if (name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) && info.MsiUrl == null)
                        {
                            info.MsiName = name;
                            info.MsiUrl = url;
                        }
                    }

                    if (info.MsiName != null)
                    {
                        foreach (JsonElement asset in assets.EnumerateArray())
                        {
                            if (string.Equals(GetString(asset, "name"), info.MsiName + ".sha256", StringComparison.OrdinalIgnoreCase))
                                info.Sha256Url = GetString(asset, "browser_download_url");
                        }
                    }
                }

                return info.MsiUrl == null ? null : info;
            }
        }

        /// <summary>Automatic check: not disabled and the last check is older than <see cref="CheckInterval"/>.</summary>
        public static bool ShouldCheckAutomatically(UpdateState state, DateTime nowUtc)
        {
            if (state == null)
                return true;
            if (state.Disabled)
                return false;
            if (state.LastCheckUtc == null)
                return true;
            DateTime last = state.LastCheckUtc.Value;
            return last > nowUtc || nowUtc - last >= CheckInterval;
        }

        /// <summary>Whether the user should be offered <paramref name="release"/>.</summary>
        public static bool ShouldOffer(ReleaseInfo release, Version installed, UpdateState state, bool manualCheck)
        {
            if (release == null || !IsNewer(release.Version, installed))
                return false;
            if (manualCheck || state == null || string.IsNullOrEmpty(state.SkippedVersion))
                return true;

            Version skipped;
            if (!TryParseVersion(state.SkippedVersion, out skipped))
                return true;
            // A version newer than the skipped one is offered again.
            return IsNewer(release.Version, skipped);
        }

        /// <summary>Extracts the hash from a "sha256sum"-style file ("&lt;hash&gt;  &lt;file&gt;" or just "&lt;hash&gt;").</summary>
        public static string ParseSha256File(string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;
            Match m = Sha256Pattern.Match(content);
            return m.Success ? m.Groups[1].Value.ToLowerInvariant() : null;
        }

        public static string ComputeSha256(Stream stream)
        {
            using (SHA256 sha = SHA256.Create())
                return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
        }

        public static string SerializeState(UpdateState state)
        {
            return JsonSerializer.Serialize(state ?? new UpdateState(), new JsonSerializerOptions { WriteIndented = true });
        }

        public static UpdateState DeserializeState(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new UpdateState();
            try
            {
                return JsonSerializer.Deserialize<UpdateState>(json) ?? new UpdateState();
            }
            catch (JsonException)
            {
                return new UpdateState();
            }
        }

        /// <summary>Shortens release notes for display.</summary>
        public static string TrimNotes(string notes, int maxLength)
        {
            if (string.IsNullOrEmpty(notes))
                return string.Empty;
            notes = notes.Replace("\r\n", "\n").Trim();
            return notes.Length <= maxLength ? notes : notes.Substring(0, maxLength).TrimEnd() + "...";
        }

        private static string GetString(JsonElement e, string name)
        {
            JsonElement v;
            return e.TryGetProperty(name, out v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        }

        private static bool GetBool(JsonElement e, string name)
        {
            JsonElement v;
            return e.TryGetProperty(name, out v) && v.ValueKind == JsonValueKind.True;
        }
    }
}
