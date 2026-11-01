////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved 
// Written by Philippe Leefsma 2011 - ADN/Developer Technical Services
//
// This software is provided as is, without any warranty that it will work. You choose to use this tool at your own risk.
// Neither Autodesk nor the author Philippe Leefsma can be taken as responsible for any damage this tool can cause to 
// your data. Please always make a back up of your data prior to use this tool, as it will modify the documents involved 
// in the feature transformation.
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Inventor;
using ThreadModeler.Addin;
using ThreadModeler.Settings;
using ThreadModeler.Utilities;

namespace ThreadModeler.Commands
{
    /////////////////////////////////////////////////////////////////
    // ThreadModeler control Dialog
    //
    /////////////////////////////////////////////////////////////////
    public partial class Controller : Form
    {
        /////////////////////////////////////////////////////////////
        // Members
        //
        /////////////////////////////////////////////////////////////
        private double _extraPitch;

        private bool _selecSetPopulated;

        private bool _extraPitchValid;

        private Inventor.Application _Application;

        private PartDocument _Document;

        private ButtonDefinition _btnDef;

        private AdnInteractionManager _InteractionManager;

        private string _ThreadTemplatePath = string.Empty;
        private bool _cleaned;


        /////////////////////////////////////////////////////////////
        // Constructor
        //
        /////////////////////////////////////////////////////////////
        public Controller(Inventor.Application Application,
            AdnInteractionManager InteractionManager)
        {
            InitializeComponent();

            bOk.Enabled = false;

            foreach (ThreadStandard standard in
                Enum.GetValues(typeof(ThreadStandard)))
            {
                cbStandard.Items.Add(new StandardItem(standard));
            }

            // ISO by default, every time the dialog opens.
            cbStandard.SelectedIndex = 0;

            tbExtraPitch.Text = (0.1).ToString("0.00");

            tbExtraPitch.TextChanged +=
                new EventHandler(this.tbExtraPitch_TextChanged);

            lvFeatures.Items.Clear();

            _Application = Application;

            _Document = _Application.ActiveEditDocument as PartDocument;

            _selecSetPopulated = false;

            _extraPitch = 0.1;

            _extraPitchValid = true;

            _InteractionManager = InteractionManager;

            //Event Handlers Init

            _InteractionManager.SelectEvents.OnSelect +=
                new SelectEventsSink_OnSelectEventHandler(
                    SelectEvents_OnSelect);

            _InteractionManager.SelectEvents.OnUnSelect +=
                new SelectEventsSink_OnUnSelectEventHandler(
                    SelectEvents_OnUnSelect);
            this.FormClosing += Controller_FormClosing;
        }

        void Controller_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_cleaned)
                CleanUp();
        }

        /////////////////////////////////////////////////////////////
        // Use: OnSelect ThreadFeature Handler.
        //
        /////////////////////////////////////////////////////////////
        void SelectEvents_OnSelect(
            ObjectsEnumerator JustSelectedEntities,
            SelectionDeviceEnum SelectionDevice,
            Point ModelPosition,
            Point2d ViewPosition,
            Inventor.View View)
        {
            try
            {
                AddSelectedThreads(JustSelectedEntities);
            }
            catch (Exception ex)
            {
                AddinLog.Error("Adding the selected thread failed.", ex);
            }
        }

        private void AddSelectedThreads(
            ObjectsEnumerator JustSelectedEntities)
        {
            foreach (System.Object obj in JustSelectedEntities)
            {
                PartFeature feature = obj as PartFeature;

                ThreadFeature thread = obj as ThreadFeature;

                ThreadInfo threadInfo = thread.ThreadInfo;

                Face threadedFace = thread.ThreadedFace[1];


                if (feature.Suppressed)
                    continue;

                if (thread.ThreadInfoType == ThreadTypeEnum.kTaperedThread &&
                    threadedFace.SurfaceType != SurfaceTypeEnum.kConeSurface)
                {
                    DialogResult res = MessageBox.Show(
                        "Threaded face surface type is not cone surface but it is applied" +
                        System.Environment.NewLine +
                        "with tapered thread. ThreadModeler cannot modelize this thread.",
                        "Invalid Surface Type",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    continue;
                }

                string iMateName = string.Empty;

                if (Toolkit.HasiMate(threadedFace,
                    out iMateName))
                {
                    DialogResult res = MessageBox.Show(
                        "Threaded face or one of its edge has" +
                        " iMate associated to it." +
                        System.Environment.NewLine +
                        "Please delete iMate " + iMateName  +
                        " before modelizing this thread.",
                        "Invalid iMate",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    continue;
                }

                double pitch = ThreadWorker.GetThreadPitch(threadInfo);

                string pitchStr =
                    ThreadWorker.GetThreadPitchStr(threadInfo,
                        (Document)_Document);

                string minStr =
                    _Document.UnitsOfMeasure.GetStringFromValue(
                        ThreadWorker.ThresholdPitchCm,
                        UnitsTypeEnum.kDefaultDisplayLengthUnits);

                if (pitch < ThreadWorker.ThresholdPitchCm)
                {
                    DialogResult res = MessageBox.Show(
                       "Selected thread pitch " +
                       "is too small (" + pitchStr + ")." +
                       System.Environment.NewLine +
                       "The minimum thread pitch that can " +
                       "be modelized is " + minStr + " .",
                       "Invalid Thread Pitch",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Error);

                    continue;
                }

                ListViewItem item =
                    lvFeatures.Items.Add(feature.Name);

                item.Tag = feature;

                item.SubItems.Add(pitchStr);

                item.SubItems.Add(ThreadWorker.GetThreadTypeStr(
                    feature));

                item.SubItems.Add(
                    ThreadWorker.GetThreadedFaceTypeStr(
                        threadedFace));
            }

            _selecSetPopulated = (lvFeatures.Items.Count != 0);

            bOk.Enabled = ValidateOkButton();
        }

        /////////////////////////////////////////////////////////////
        // use: OnUnselect ThreadFeature Handler.
        //
        /////////////////////////////////////////////////////////////
        void SelectEvents_OnUnSelect(
            ObjectsEnumerator UnSelectedEntities,
            SelectionDeviceEnum SelectionDevice,
            Point ModelPosition,
            Point2d ViewPosition,
            Inventor.View View)
        {
            foreach (System.Object obj in UnSelectedEntities)
            {
                foreach (ListViewItem item in lvFeatures.Items)
                {
                    if (item.Tag == obj)
                    {
                        item.Remove();
                        break;
                    }
                }
            }

            if (_InteractionManager.SelectedEntities.Count == 0)
                bOk.Enabled = false;
        }

        /////////////////////////////////////////////////////////////
        // Use: Ok Button clicked Handler
        //
        /////////////////////////////////////////////////////////////
        private void bOk_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Modelize())
                    return;
            }
            catch (Exception ex)
            {
                AddinLog.Error("Modelling failed.", ex);

                MessageBox.Show(
                    "Modelling failed: " + ex.Message +
                    System.Environment.NewLine + System.Environment.NewLine +
                    "Details: " + AddinLog.Path,
                    "Modelization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            // Problem: closing the template doc here
            // will empty the undo stack (as designed)...

            if (!_cleaned)
                CleanUp();
            _InteractionManager.Terminate();
        }

        /////////////////////////////////////////////////////////////
        // Use: Modelizes the selected threads with the template of
        //      the selected standard. Returns False if the dialog
        //      should stay open.
        /////////////////////////////////////////////////////////////
        private bool Modelize()
        {
            AddinLog.Info("Modelling in " +
                ((Document)_Document).DisplayName + " with " +
                SelectedStandard + " template " + _ThreadTemplatePath + ".");

            PartDocument template;

            string error = ThreadWorker.OpenTemplate(
                _ThreadTemplatePath, out template);

            if (error != null)
            {
                AddinLog.Warn("Invalid template: " + error);

                MessageBox.Show(
                    error + System.Environment.NewLine +
                    System.Environment.NewLine +
                    "Assign a valid " + SelectedStandard +
                    " template in ThreadModeler > Configuration.",
                    "Invalid Template",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }

            List<ThreadFeature> threads = new List<ThreadFeature>();

            foreach (System.Object selectedObj in
                _InteractionManager.SelectedEntities)
            {
                ThreadFeature thread = selectedObj as ThreadFeature;

                if (thread == null || thread.Suppressed)
                    continue;

                threads.Add(thread);
            }

            PlanarSketch templateSketch =
                template.ComponentDefinition.Sketches[1];

            if (!ThreadWorker.ModelizeThreads(
                _Document,
                templateSketch,
                threads,
                _extraPitch))
            {
                string message =
                    "Failed to modelize the thread." +
                    System.Environment.NewLine + System.Environment.NewLine +
                    ThreadWorker.LastError + System.Environment.NewLine +
                    System.Environment.NewLine;

                if (ThreadWorker.LastErrorIsCoil)
                    message += "Try with a bigger Pitch Offset value." +
                        System.Environment.NewLine;

                message += "Details: " + AddinLog.Path;

                DialogResult res = MessageBox.Show(
                    message,
                    "Modelization Error",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Error);

                // OK: back to the dialog. Cancel: close it.
                return (res != DialogResult.OK);
            }

            return true;
        }

        /////////////////////////////////////////////////////////////
        // Use: Cancel Button clicked Handler
        //
        /////////////////////////////////////////////////////////////
        private void bCancel_Click(object sender, EventArgs e)
        {
            if (!_cleaned)
                CleanUp();
            _InteractionManager.Terminate();
        }

        /////////////////////////////////////////////////////////////
        // Use: Thread standard selected in the drop-down.
        //
        /////////////////////////////////////////////////////////////
        private ThreadStandard SelectedStandard
        {
            get
            {
                StandardItem item = cbStandard.SelectedItem as StandardItem;

                return (item == null ? ThreadStandard.ISO : item.Standard);
            }
        }

        /////////////////////////////////////////////////////////////
        // Use: Thread standard changed handler: resolves the
        //      template configured for it.
        /////////////////////////////////////////////////////////////
        private void cbStandard_SelectedIndexChanged(object sender,
            EventArgs e)
        {
            _ThreadTemplatePath = ThreadSettings.Load().ResolveTemplate(
                SelectedStandard, ThreadSettings.AddinDirectory);

            bool exists = System.IO.File.Exists(_ThreadTemplatePath);

            lbTemplateFile.Text = exists ?
                System.IO.Path.GetFileName(_ThreadTemplatePath) :
                "(not found – see Configuration)";

            toolTip.SetToolTip(lbTemplateFile, _ThreadTemplatePath);

            if (!exists)
                _ThreadTemplatePath = string.Empty;

            bOk.Enabled = ValidateOkButton();
        }

        /////////////////////////////////////////////////////////////
        // Drop-down entry for a thread standard.
        //
        /////////////////////////////////////////////////////////////
        private sealed class StandardItem
        {
            public readonly ThreadStandard Standard;

            public StandardItem(ThreadStandard standard)
            {
                Standard = standard;
            }

            public override string ToString()
            {
                return ThreadSettings.DisplayName(Standard);
            }
        }

        /////////////////////////////////////////////////////////////
        // Use: Scroll control handler.
        //
        /////////////////////////////////////////////////////////////
        private void OnScroll(object sender,
            ScrollEventArgs e)
        {
            _extraPitch = (1000 - scrollPitch.Value) * 0.01;

            tbExtraPitch.Text = _extraPitch.ToString("0.00");
        }

        /////////////////////////////////////////////////////////////
        // Use: Pitch Offset text changed Handler.
        //
        /////////////////////////////////////////////////////////////
        private void tbExtraPitch_TextChanged(object sender,
            EventArgs e)
        {
            try
            {
                double extraPitch = double.Parse(tbExtraPitch.Text);

                if (extraPitch < 0.1 || extraPitch > 10.0)
                {
                    tbExtraPitch.ForeColor = System.Drawing.Color.Red;
                    _extraPitchValid = false;
                    bOk.Enabled = ValidateOkButton();
                    return;
                }

                _extraPitch = extraPitch;

                scrollPitch.Value = (int)(1000.0 - _extraPitch * 100.0);

                tbExtraPitch.ForeColor = System.Drawing.Color.Black;
                _extraPitchValid = true;

                bOk.Enabled = ValidateOkButton();
            }
            catch
            {
                tbExtraPitch.ForeColor = System.Drawing.Color.Red;
                _extraPitchValid = false;

                bOk.Enabled = ValidateOkButton();
            }
        }

        /////////////////////////////////////////////////////////////
        // Use: Returns True if Ok Button can be enabled.
        //
        /////////////////////////////////////////////////////////////
        private bool ValidateOkButton()
        {
            return (_selecSetPopulated &&
                (_ThreadTemplatePath != string.Empty) &&
                _extraPitchValid);
        }

        private void CleanUp()
        {
            _InteractionManager.SelectEvents.OnSelect -= new SelectEventsSink_OnSelectEventHandler(
                    SelectEvents_OnSelect);

            _InteractionManager.SelectEvents.OnUnSelect -=
                new SelectEventsSink_OnUnSelectEventHandler(
                    SelectEvents_OnUnSelect);
            _cleaned = true;
        }
    }
}
