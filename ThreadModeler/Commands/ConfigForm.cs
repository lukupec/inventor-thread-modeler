////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Configuration dialog: assigns the thread profile template (.ipt) used for each thread standard
// (built in code; no designer file).
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ThreadModeler.Addin;
using ThreadModeler.Settings;

namespace ThreadModeler.Commands
{
    internal sealed class ConfigForm : Form
    {
        private readonly Dictionary<ThreadStandard, TextBox> _paths = new Dictionary<ThreadStandard, TextBox>();

        public ConfigForm()
        {
            Text = "ThreadModeler Configuration";
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
                ColumnCount = 3,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var intro = new Label
            {
                Text = "Thread profile template (.ipt) used for each thread standard. The template's first sketch " +
                       "needs parameters with the comments " + string.Join(", ", ThreadWorker.TemplateParameters) + ".",
                AutoSize = true,
                MaximumSize = new Size(520, 0),
                Margin = new Padding(3, 0, 3, 12)
            };
            layout.Controls.Add(intro, 0, 0);
            layout.SetColumnSpan(intro, 3);

            ThreadSettings settings = ThreadSettings.Load();
            int row = 1;
            foreach (ThreadStandard standard in Enum.GetValues(typeof(ThreadStandard)))
            {
                var label = new Label
                {
                    Text = ThreadSettings.DisplayName(standard) + ":",
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(3, 6, 8, 6)
                };
                var path = new TextBox
                {
                    Width = 380,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right,
                    Text = settings.ResolveTemplate(standard, ThreadSettings.AddinDirectory)
                };
                var browse = new Button { Text = "Browse…", AutoSize = true, Anchor = AnchorStyles.Left };
                ThreadStandard s = standard;
                browse.Click += (o, e) => Browse(s);

                _paths[standard] = path;
                layout.Controls.Add(label, 0, row);
                layout.Controls.Add(path, 1, row);
                layout.Controls.Add(browse, 2, row);
                row++;
            }

            var reset = new Button { Text = "Reset to defaults", AutoSize = true, Anchor = AnchorStyles.Left };
            reset.Click += (o, e) =>
            {
                foreach (var pair in _paths)
                    pair.Value.Text = ThreadSettings.BundledTemplate(pair.Key, ThreadSettings.AddinDirectory);
            };

            var ok = new Button { Text = "OK", AutoSize = true, MinimumSize = new Size(75, 0) };
            ok.Click += (o, e) => OnOk();
            var cancel = new Button
            {
                Text = "Cancel",
                AutoSize = true,
                MinimumSize = new Size(75, 0),
                DialogResult = DialogResult.Cancel
            };
            AcceptButton = ok;
            CancelButton = cancel;

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 16, 0, 0)
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);

            reset.Margin = new Padding(3, 19, 3, 3);
            layout.Controls.Add(reset, 0, row);
            layout.SetColumnSpan(reset, 2);
            layout.Controls.Add(buttons, 1, row);
            layout.SetColumnSpan(buttons, 2);
            Controls.Add(layout);
        }

        private void Browse(ThreadStandard standard)
        {
            TextBox box = _paths[standard];
            string dir = null;
            try
            {
                dir = System.IO.Path.GetDirectoryName(box.Text);
            }
            catch (Exception)
            {
            }
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                dir = System.IO.Path.Combine(ThreadSettings.AddinDirectory, "Thread Templates");

            using (var dlg = new OpenFileDialog
            {
                Title = "Select " + ThreadSettings.DisplayName(standard) + " Thread Template",
                Filter = "Inventor Parts (*.ipt)|*.ipt",
                InitialDirectory = dir,
                FileName = System.IO.Path.GetFileName(box.Text),
                CheckFileExists = true
            })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    box.Text = dlg.FileName;
            }
        }

        private void OnOk()
        {
            var settings = new ThreadSettings();

            Cursor = Cursors.WaitCursor;
            try
            {
                foreach (var pair in _paths)
                {
                    string path = pair.Value.Text.Trim();

                    Inventor.PartDocument template;
                    string error = ThreadWorker.OpenTemplate(path, out template);
                    if (error != null)
                    {
                        Cursor = Cursors.Default;
                        MessageBox.Show(this, error, "Invalid " + pair.Key + " Template",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        pair.Value.Focus();
                        pair.Value.SelectAll();
                        return;
                    }

                    // The bundled template is stored as "default", so it follows the add-in when it is updated.
                    string bundled = ThreadSettings.BundledTemplate(pair.Key, ThreadSettings.AddinDirectory);
                    if (!string.Equals(System.IO.Path.GetFullPath(path), System.IO.Path.GetFullPath(bundled),
                            StringComparison.OrdinalIgnoreCase))
                        settings.SetTemplatePath(pair.Key, path);
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            if (!settings.Save())
            {
                MessageBox.Show(this, "Could not save " + ThreadSettings.DefaultFile + ".", "ThreadModeler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AddinLog.Info("Templates: ISO = " + (settings.IsoTemplatePath ?? "bundled") +
                ", BSW = " + (settings.BswTemplatePath ?? "bundled") + ".");
            DialogResult = DialogResult.OK;
        }
    }
}
