////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// "Update available" dialog (built in code; no designer file).
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace ThreadModeler.Update
{
    internal enum UpdateChoice
    {
        Later,
        Install,
        Skip
    }

    internal sealed class UpdateForm : Form
    {
        private UpdateChoice _choice = UpdateChoice.Later;

        public UpdateForm(ReleaseInfo release, Version installed)
        {
            Text = "ThreadModeler update";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            Font = SystemFonts.MessageBoxFont;
            ClientSize = new Size(520, 360);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowOnly;
            Padding = new Padding(12);

            var heading = new Label
            {
                Text = "ThreadModeler " + UpdateChecker.Format(release.Version) + " is available",
                Font = new Font(Font.FontFamily, Font.Size * 1.3f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 12)
            };

            var info = new Label
            {
                Text = "You have version " + UpdateChecker.Format(installed) +
                       ". The update is downloaded now and installed after you close Inventor.",
                AutoSize = false,
                Location = new Point(12, 46),
                Size = new Size(496, 36)
            };

            var notes = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(12, 86),
                Size = new Size(496, 200),
                Text = UpdateLogic.TrimNotes(release.Notes, 4000).Replace("\n", Environment.NewLine)
            };

            var link = new LinkLabel
            {
                Text = "Release page",
                AutoSize = true,
                Location = new Point(12, 296)
            };
            link.LinkClicked += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(release.PageUrl) { UseShellExecute = true }); }
                catch (Exception) { }
            };

            var install = new Button { Text = "Download and install", AutoSize = true };
            var later = new Button { Text = "Remind me later", AutoSize = true };
            var skip = new Button { Text = "Skip this version", AutoSize = true };

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Location = new Point(9, 318)
            };
            buttons.Controls.AddRange(new Control[] { install, later, skip });

            install.Click += (s, e) => { _choice = UpdateChoice.Install; Close(); };
            later.Click += (s, e) => { _choice = UpdateChoice.Later; Close(); };
            skip.Click += (s, e) => { _choice = UpdateChoice.Skip; Close(); };

            AcceptButton = install;
            CancelButton = later;

            Controls.AddRange(new Control[] { heading, info, notes, link, buttons });
        }

        public UpdateChoice ShowAndGetChoice(IWin32Window owner)
        {
            if (owner != null)
                ShowDialog(owner);
            else
                ShowDialog();
            return _choice;
        }
    }
}
