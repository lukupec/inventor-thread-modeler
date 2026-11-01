namespace ThreadModeler.Commands
{
    partial class Controller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (_InteractionManager != null && _InteractionManager.SelectEvents != null)
            {
                _InteractionManager.SelectEvents.OnSelect -= SelectEvents_OnSelect;

                _InteractionManager.SelectEvents.OnUnSelect -= SelectEvents_OnUnSelect;
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "Thread Feature 1",
            "Pitch1",
            "Standard",
            "Exterior"}, -1);
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Controller));
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.bOk = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.gbTemplate = new System.Windows.Forms.GroupBox();
            this.cbStandard = new System.Windows.Forms.ComboBox();
            this.lbTemplateFile = new System.Windows.Forms.Label();
            this.lvFeatures = new System.Windows.Forms.ListView();
            this.hThread = new System.Windows.Forms.ColumnHeader();
            this.hPitch = new System.Windows.Forms.ColumnHeader();
            this.hType = new System.Windows.Forms.ColumnHeader();
            this.hFace = new System.Windows.Forms.ColumnHeader();
            this.gbPitchOffset = new System.Windows.Forms.GroupBox();
            this.lbExtraPitch2 = new System.Windows.Forms.Label();
            this.tbExtraPitch = new System.Windows.Forms.TextBox();
            this.lbExtraPitch = new System.Windows.Forms.Label();
            this.scrollPitch = new System.Windows.Forms.VScrollBar();
            this.gbTemplate.SuspendLayout();
            this.gbPitchOffset.SuspendLayout();
            this.SuspendLayout();
            // 
            // bOk
            // 
            this.bOk.Location = new System.Drawing.Point(209, 239);
            this.bOk.Name = "bOk";
            this.bOk.Size = new System.Drawing.Size(75, 23);
            this.bOk.TabIndex = 0;
            this.bOk.Text = "Ok";
            this.bOk.UseVisualStyleBackColor = true;
            this.bOk.Click += new System.EventHandler(this.bOk_Click);
            // 
            // bCancel
            // 
            this.bCancel.Location = new System.Drawing.Point(290, 239);
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size(75, 23);
            this.bCancel.TabIndex = 1;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = true;
            this.bCancel.Click += new System.EventHandler(this.bCancel_Click);
            //
            // gbTemplate
            //
            this.gbTemplate.Controls.Add(this.cbStandard);
            this.gbTemplate.Controls.Add(this.lbTemplateFile);
            this.gbTemplate.Location = new System.Drawing.Point(12, 12);
            this.gbTemplate.Name = "gbTemplate";
            this.gbTemplate.Size = new System.Drawing.Size(353, 51);
            this.gbTemplate.TabIndex = 3;
            this.gbTemplate.TabStop = false;
            this.gbTemplate.Text = "Thread Profile";
            //
            // cbStandard
            //
            this.cbStandard.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbStandard.Location = new System.Drawing.Point(6, 19);
            this.cbStandard.Name = "cbStandard";
            this.cbStandard.Size = new System.Drawing.Size(150, 21);
            this.cbStandard.TabIndex = 2;
            this.cbStandard.SelectedIndexChanged += new System.EventHandler(this.cbStandard_SelectedIndexChanged);
            //
            // lbTemplateFile
            //
            this.lbTemplateFile.AutoEllipsis = true;
            this.lbTemplateFile.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lbTemplateFile.Location = new System.Drawing.Point(162, 22);
            this.lbTemplateFile.Name = "lbTemplateFile";
            this.lbTemplateFile.Size = new System.Drawing.Size(185, 16);
            this.lbTemplateFile.TabIndex = 3;
            //
            // lvFeatures
            // 
            this.lvFeatures.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.hThread,
            this.hPitch,
            this.hType,
            this.hFace});
            this.lvFeatures.FullRowSelect = true;
            this.lvFeatures.GridLines = true;
            this.lvFeatures.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1});
            this.lvFeatures.Location = new System.Drawing.Point(12, 128);
            this.lvFeatures.MultiSelect = false;
            this.lvFeatures.Name = "lvFeatures";
            this.lvFeatures.Size = new System.Drawing.Size(353, 99);
            this.lvFeatures.TabIndex = 4;
            this.lvFeatures.UseCompatibleStateImageBehavior = false;
            this.lvFeatures.View = System.Windows.Forms.View.Details;
            // 
            // hThread
            // 
            this.hThread.Text = "Selected Threads";
            this.hThread.Width = 110;
            // 
            // hPitch
            // 
            this.hPitch.Text = "Pitch";
            this.hPitch.Width = 77;
            // 
            // hType
            // 
            this.hType.Text = "Thread Type";
            this.hType.Width = 82;
            // 
            // hFace
            // 
            this.hFace.Text = "Face Type";
            this.hFace.Width = 80;
            // 
            // gbPitchOffset
            // 
            this.gbPitchOffset.Controls.Add(this.lbExtraPitch2);
            this.gbPitchOffset.Controls.Add(this.tbExtraPitch);
            this.gbPitchOffset.Controls.Add(this.lbExtraPitch);
            this.gbPitchOffset.Controls.Add(this.scrollPitch);
            this.gbPitchOffset.Location = new System.Drawing.Point(12, 70);
            this.gbPitchOffset.Name = "gbPitchOffset";
            this.gbPitchOffset.Size = new System.Drawing.Size(353, 47);
            this.gbPitchOffset.TabIndex = 5;
            this.gbPitchOffset.TabStop = false;
            this.gbPitchOffset.Text = "Pitch Offset";
            // 
            // lbExtraPitch2
            // 
            this.lbExtraPitch2.AutoSize = true;
            this.lbExtraPitch2.Location = new System.Drawing.Point(133, 23);
            this.lbExtraPitch2.Name = "lbExtraPitch2";
            this.lbExtraPitch2.Size = new System.Drawing.Size(15, 13);
            this.lbExtraPitch2.TabIndex = 3;
            this.lbExtraPitch2.Text = "%";
            // 
            // tbExtraPitch
            // 
            this.tbExtraPitch.Location = new System.Drawing.Point(87, 20);
            this.tbExtraPitch.Name = "tbExtraPitch";
            this.tbExtraPitch.Size = new System.Drawing.Size(43, 20);
            this.tbExtraPitch.TabIndex = 2;
            this.tbExtraPitch.Text = "0.1";
            // 
            // lbExtraPitch
            // 
            this.lbExtraPitch.AutoSize = true;
            this.lbExtraPitch.Location = new System.Drawing.Point(6, 22);
            this.lbExtraPitch.Name = "lbExtraPitch";
            this.lbExtraPitch.Size = new System.Drawing.Size(77, 13);
            this.lbExtraPitch.TabIndex = 1;
            this.lbExtraPitch.Text = "Thread Pitch +";
            // 
            // scrollPitch
            // 
            this.scrollPitch.LargeChange = 1;
            this.scrollPitch.Location = new System.Drawing.Point(165, 13);
            this.scrollPitch.Maximum = 990;
            this.scrollPitch.Name = "scrollPitch";
            this.scrollPitch.Size = new System.Drawing.Size(28, 28);
            this.scrollPitch.TabIndex = 0;
            this.scrollPitch.Value = 990;
            this.scrollPitch.Scroll += new System.Windows.Forms.ScrollEventHandler(this.OnScroll);
            // 
            // Controller
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(377, 270);
            this.Controls.Add(this.gbPitchOffset);
            this.Controls.Add(this.lvFeatures);
            this.Controls.Add(this.gbTemplate);
            this.Controls.Add(this.bCancel);
            this.Controls.Add(this.bOk);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = global::ThreadModeler.resources.threadModeler1;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Controller";
            this.Text = "Thread Modeler Control";
            this.gbTemplate.ResumeLayout(false);
            this.gbPitchOffset.ResumeLayout(false);
            this.gbPitchOffset.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button bOk;
        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.GroupBox gbTemplate;
        private System.Windows.Forms.ComboBox cbStandard;
        private System.Windows.Forms.Label lbTemplateFile;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ListView lvFeatures;
        private System.Windows.Forms.ColumnHeader hThread;
        private System.Windows.Forms.ColumnHeader hPitch;
        private System.Windows.Forms.GroupBox gbPitchOffset;
        private System.Windows.Forms.Label lbExtraPitch;
        private System.Windows.Forms.VScrollBar scrollPitch;
        private System.Windows.Forms.ColumnHeader hType;
        private System.Windows.Forms.ColumnHeader hFace;
        private System.Windows.Forms.TextBox tbExtraPitch;
        private System.Windows.Forms.Label lbExtraPitch2;
    }
}