using System;
using System.IO;
using ThreadModeler.Settings;
using Xunit;

namespace ThreadModeler.Tests
{
    public class ThreadSettingsTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "ThreadModelerTests-" + Guid.NewGuid().ToString("N"));

        public ThreadSettingsTests()
        {
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch (Exception) { }
        }

        private string Touch(string name)
        {
            string path = Path.Combine(_dir, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "");
            return path;
        }

        [Fact]
        public void Load_MissingFile_GivesDefaults()
        {
            ThreadSettings s = ThreadSettings.Load(Path.Combine(_dir, "missing.json"));

            Assert.Null(s.IsoTemplatePath);
            Assert.Null(s.BswTemplatePath);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("[1,2]")]
        [InlineData("null")]
        public void Deserialize_Invalid_GivesDefaults(string json)
        {
            ThreadSettings s = ThreadSettings.Deserialize(json);

            Assert.NotNull(s);
            Assert.Null(s.IsoTemplatePath);
            Assert.Null(s.BswTemplatePath);
        }

        [Fact]
        public void SaveAndLoad_RoundTrips()
        {
            string file = Path.Combine(_dir, "sub", "settings.json");
            var s = new ThreadSettings();
            s.SetTemplatePath(ThreadStandard.ISO, @"C:\Templates\My ISO.ipt");
            s.SetTemplatePath(ThreadStandard.BSW, "  ");

            Assert.True(s.Save(file));
            ThreadSettings loaded = ThreadSettings.Load(file);

            Assert.Equal(@"C:\Templates\My ISO.ipt", loaded.GetTemplatePath(ThreadStandard.ISO));
            Assert.Null(loaded.GetTemplatePath(ThreadStandard.BSW));
        }

        [Fact]
        public void BundledTemplate_IsInThreadTemplatesFolder()
        {
            Assert.Equal(Path.Combine(_dir, "Thread Templates", "ISO Template.ipt"),
                ThreadSettings.BundledTemplate(ThreadStandard.ISO, _dir));
            Assert.Equal(Path.Combine(_dir, "Thread Templates", "BSW Template.ipt"),
                ThreadSettings.BundledTemplate(ThreadStandard.BSW, _dir));
        }

        [Fact]
        public void ResolveTemplate_UsesConfiguredFileWhenItExists()
        {
            string custom = Touch("custom.ipt");
            var s = new ThreadSettings();
            s.SetTemplatePath(ThreadStandard.BSW, custom);

            Assert.Equal(custom, s.ResolveTemplate(ThreadStandard.BSW, _dir));
            Assert.Equal(ThreadSettings.BundledTemplate(ThreadStandard.ISO, _dir), s.ResolveTemplate(ThreadStandard.ISO, _dir));
        }

        [Fact]
        public void ResolveTemplate_FallsBackToBundledWhenConfiguredFileIsMissing()
        {
            var s = new ThreadSettings();
            s.SetTemplatePath(ThreadStandard.ISO, Path.Combine(_dir, "gone.ipt"));

            Assert.Equal(ThreadSettings.BundledTemplate(ThreadStandard.ISO, _dir), s.ResolveTemplate(ThreadStandard.ISO, _dir));
        }

        [Fact]
        public void DisplayName_NamesTheStandard()
        {
            Assert.StartsWith("ISO", ThreadSettings.DisplayName(ThreadStandard.ISO));
            Assert.StartsWith("BSW", ThreadSettings.DisplayName(ThreadStandard.BSW));
        }
    }
}
