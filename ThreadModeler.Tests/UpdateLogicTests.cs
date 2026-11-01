using System;
using System.IO;
using System.Text;
using ThreadModeler.Update;
using Xunit;

namespace ThreadModeler.Tests
{
    public class UpdateLogicTests
    {
        private const string SampleRelease = @"{
  ""html_url"": ""https://github.com/lukupec/inventor-thread-modeler/releases/tag/v30.0.2"",
  ""tag_name"": ""v30.0.2"",
  ""draft"": false,
  ""prerelease"": false,
  ""body"": ""## What's changed\r\n* Fix things"",
  ""assets"": [
    { ""name"": ""ThreadModeler-30.0.2.msi.sha256"", ""browser_download_url"": ""https://example.test/ThreadModeler-30.0.2.msi.sha256"" },
    { ""name"": ""ThreadModeler-30.0.2.msi"", ""browser_download_url"": ""https://example.test/ThreadModeler-30.0.2.msi"" },
    { ""name"": ""ThreadModeler.bundle.zip"", ""browser_download_url"": ""https://example.test/bundle.zip"" }
  ]
}";

        [Theory]
        [InlineData("v30.0.1", 30, 0, 1)]
        [InlineData("30.0.1", 30, 0, 1)]
        [InlineData("V30.1", 30, 1, 0)]
        [InlineData("30.0.1.0", 30, 0, 1)]
        [InlineData("v30.0.1-beta.2", 30, 0, 1)]
        [InlineData(" v31.2.3+build5 ", 31, 2, 3)]
        public void TryParseVersion_Accepts(string text, int major, int minor, int build)
        {
            Version v;
            Assert.True(UpdateLogic.TryParseVersion(text, out v));
            Assert.Equal(new Version(major, minor, build), v);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("latest")]
        [InlineData("v30")]
        [InlineData("release-30.0.1")]
        public void TryParseVersion_Rejects(string text)
        {
            Version v;
            Assert.False(UpdateLogic.TryParseVersion(text, out v));
        }

        [Fact]
        public void IsNewer_IgnoresRevision()
        {
            Assert.False(UpdateLogic.IsNewer(new Version(30, 0, 1), new Version(30, 0, 1, 0)));
            Assert.False(UpdateLogic.IsNewer(new Version(30, 0, 1, 5), new Version(30, 0, 1, 0)));
            Assert.True(UpdateLogic.IsNewer(new Version(30, 0, 2), new Version(30, 0, 1, 0)));
            Assert.True(UpdateLogic.IsNewer(new Version(31, 0, 0), new Version(30, 9, 9, 9)));
            Assert.False(UpdateLogic.IsNewer(new Version(30, 0, 0), new Version(30, 0, 1)));
        }

        [Fact]
        public void ParseLatestRelease_ReadsVersionNotesAndAssets()
        {
            ReleaseInfo r = UpdateLogic.ParseLatestRelease(SampleRelease);

            Assert.NotNull(r);
            Assert.Equal(new Version(30, 0, 2), r.Version);
            Assert.Equal("v30.0.2", r.Tag);
            Assert.Equal("ThreadModeler-30.0.2.msi", r.MsiName);
            Assert.Equal("https://example.test/ThreadModeler-30.0.2.msi", r.MsiUrl);
            Assert.Equal("https://example.test/ThreadModeler-30.0.2.msi.sha256", r.Sha256Url);
            Assert.Contains("Fix things", r.Notes);
            Assert.EndsWith("/v30.0.2", r.PageUrl);
        }

        [Fact]
        public void ParseLatestRelease_SkipsDraftsPrereleasesAndReleasesWithoutMsi()
        {
            Assert.Null(UpdateLogic.ParseLatestRelease(SampleRelease.Replace(@"""draft"": false", @"""draft"": true")));
            Assert.Null(UpdateLogic.ParseLatestRelease(SampleRelease.Replace(@"""prerelease"": false", @"""prerelease"": true")));
            Assert.Null(UpdateLogic.ParseLatestRelease(SampleRelease.Replace(".msi\"", ".exe\"")));
            Assert.Null(UpdateLogic.ParseLatestRelease(SampleRelease.Replace("v30.0.2\"", "nightly\"")));
            Assert.Null(UpdateLogic.ParseLatestRelease(""));
        }

        [Fact]
        public void ShouldCheckAutomatically_ThrottlesToOncePerInterval()
        {
            DateTime now = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

            Assert.True(UpdateLogic.ShouldCheckAutomatically(null, now));
            Assert.True(UpdateLogic.ShouldCheckAutomatically(new UpdateState(), now));
            Assert.False(UpdateLogic.ShouldCheckAutomatically(new UpdateState { LastCheckUtc = now.AddHours(-1) }, now));
            Assert.True(UpdateLogic.ShouldCheckAutomatically(new UpdateState { LastCheckUtc = now.AddHours(-25) }, now));
            // Clock moved backwards: check again rather than never.
            Assert.True(UpdateLogic.ShouldCheckAutomatically(new UpdateState { LastCheckUtc = now.AddDays(3) }, now));
            Assert.False(UpdateLogic.ShouldCheckAutomatically(new UpdateState { Disabled = true }, now));
        }

        [Fact]
        public void ShouldOffer_RespectsSkippedVersionUnlessManual()
        {
            ReleaseInfo r = UpdateLogic.ParseLatestRelease(SampleRelease);
            Version installed = new Version(30, 0, 1, 0);

            Assert.True(UpdateLogic.ShouldOffer(r, installed, new UpdateState(), manualCheck: false));
            Assert.False(UpdateLogic.ShouldOffer(r, installed, new UpdateState { SkippedVersion = "30.0.2" }, manualCheck: false));
            Assert.True(UpdateLogic.ShouldOffer(r, installed, new UpdateState { SkippedVersion = "30.0.2" }, manualCheck: true));
            // A newer release than the skipped one is offered again.
            Assert.True(UpdateLogic.ShouldOffer(r, installed, new UpdateState { SkippedVersion = "30.0.1" }, manualCheck: false));
            // Not newer than installed.
            Assert.False(UpdateLogic.ShouldOffer(r, new Version(30, 0, 2, 0), new UpdateState(), manualCheck: true));
            Assert.False(UpdateLogic.ShouldOffer(null, installed, new UpdateState(), manualCheck: true));
        }

        [Fact]
        public void Sha256_FileParsingAndComputation()
        {
            byte[] data = Encoding.UTF8.GetBytes("ThreadModeler");
            string hash;
            using (var ms = new MemoryStream(data))
                hash = UpdateLogic.ComputeSha256(ms);

            Assert.Equal(64, hash.Length);
            Assert.Equal(hash, UpdateLogic.ParseSha256File(hash.ToUpperInvariant() + "  ThreadModeler-30.0.2.msi\n"));
            Assert.Equal(hash, UpdateLogic.ParseSha256File(hash));
            Assert.Null(UpdateLogic.ParseSha256File("not a hash"));
            Assert.Null(UpdateLogic.ParseSha256File(null));
        }

        [Fact]
        public void State_RoundTripsAndToleratesGarbage()
        {
            var state = new UpdateState
            {
                LastCheckUtc = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
                SkippedVersion = "30.0.2"
            };

            UpdateState back = UpdateLogic.DeserializeState(UpdateLogic.SerializeState(state));
            Assert.Equal(state.LastCheckUtc, back.LastCheckUtc);
            Assert.Equal("30.0.2", back.SkippedVersion);

            Assert.NotNull(UpdateLogic.DeserializeState("{ not json"));
            Assert.NotNull(UpdateLogic.DeserializeState(null));
        }

        [Fact]
        public void TrimNotes_Truncates()
        {
            Assert.Equal(string.Empty, UpdateLogic.TrimNotes(null, 10));
            Assert.Equal("a\nb", UpdateLogic.TrimNotes(" a\r\nb ", 10));
            Assert.Equal("abcde...", UpdateLogic.TrimNotes("abcdefghij", 5));
        }
    }
}
