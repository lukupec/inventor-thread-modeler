////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// About dialog (built in code; no designer file).
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ThreadModeler.Update;

namespace ThreadModeler.Commands
{
    internal sealed class AboutForm : Form
    {
        public AboutForm()
        {
            Text = "About ThreadModeler";
            try { Icon = global::ThreadModeler.resources.threadModeler1; }
            catch (Exception) { }
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            Font = SystemFonts.MessageBoxFont;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);

            var layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var icon = new PictureBox
            {
                Size = new Size(48, 48),
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = new Padding(0, 4, 16, 0)
            };
            try
            {
                using (Icon ico = new Icon(resources.threadModeler1, 48, 48))
                    icon.Image = ico.ToBitmap();
            }
            catch (Exception)
            {
            }

            var text = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            text.Controls.Add(new Label
            {
                Text = "ThreadModeler",
                Font = new Font(Font.FontFamily, Font.Size * 1.6f, FontStyle.Bold),
                AutoSize = true
            });
            text.Controls.Add(new Label
            {
                Text = "Version " + UpdateChecker.Format(UpdateChecker.InstalledVersion),
                AutoSize = true,
                Margin = new Padding(3, 0, 3, 10)
            });
            text.Controls.Add(new Label
            {
                Text = "Generates realistic modeled threads from Autodesk Inventor thread features.",
                AutoSize = true,
                MaximumSize = new Size(380, 0)
            });
            text.Controls.Add(new Label
            {
                Text = "Open source under the MIT License. See LICENSE.txt and THIRD-PARTY-NOTICES.txt " +
                       "in the add-in folder for copyright notices.",
                AutoSize = true,
                MaximumSize = new Size(380, 0),
                Margin = new Padding(3, 10, 3, 6)
            });
            text.Controls.Add(Link("Project page", UpdateLogic.ProjectUrl));
            text.Controls.Add(Link("License", UpdateLogic.ProjectUrl + "/blob/master/LICENSE"));

            var close = new Button
            {
                Text = "Close",
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(3, 16, 0, 0),
                DialogResult = DialogResult.OK
            };
            AcceptButton = close;
            CancelButton = close;

            layout.Controls.Add(icon, 0, 0);
            layout.Controls.Add(text, 1, 0);
            layout.Controls.Add(close, 1, 1);
            Controls.Add(layout);
        }

        private static LinkLabel Link(string text, string url)
        {
            var link = new LinkLabel { Text = text, AutoSize = true };
            link.LinkClicked += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                catch (Exception) { }
            };
            return link;
        }
    }
}
