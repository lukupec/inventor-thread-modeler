////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved 
// Written by Jan Liska & Philippe Leefsma 2011 - ADN/Developer Technical Services
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted, 
// provided that the above copyright notice appears in all copies and 
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting 
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS. 
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC. 
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// ThreadModeler Inventor Add-in
//  
// Author: Philippe Leefsma
// Creation date: 11/7/2011 9:15:24 AM
// 
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using ThreadModeler.Commands;
using ThreadModeler.Update;
using ThreadModeler.Utilities;

namespace ThreadModeler.Addin
{
    /// <summary>
    /// This is the primary AddIn Server class that implements the ApplicationAddInServer interface
    /// that all Inventor AddIns are required to implement. The communication between Inventor and
    /// the AddIn is via the methods on this interface.
    /// </summary>
    [Guid("90f6009a-a2ed-400f-9cbd-cee55ad30d23"),ComVisible(true)]
    public class StandardAddInServer : Inventor.ApplicationAddInServer
    {
        // Inventor application object.
        private Inventor.Application m_inventorApplication;

        public StandardAddInServer()
        {
            // No static/instance state here that could throw: a failing constructor makes Inventor
            // leave the add-in "Unloaded" without any message.
            AddinLog.Info("StandardAddInServer created.");
        }

        #region ApplicationAddInServer Members

        public void Activate(Inventor.ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            // This method is called by Inventor when it loads the addin.
            // Inventor does not report exceptions thrown here (the add-in just stays "Unloaded"),
            // so they are logged, written to %TEMP%\ThreadModeler-load-error.txt and shown to the user.
            // The work is in separate non-inlined methods so that even type-load/JIT failures of the
            // code they use happen inside this try block.
            AddinLog.Info("Activate (firstTime=" + firstTime + ").");
            try
            {
                ActivateCore(addInSiteObject);
            }
            catch (Exception ex)
            {
                AddinLog.Error("ThreadModeler failed to load.", ex);
                string file = WriteLoadError("ThreadModeler failed to load", ex, addInSiteObject);
                try
                {
                    MessageBox.Show(
                        "ThreadModeler failed to load:\n\n" + ex.GetBaseException().Message +
                        "\n\nDetails: " + (file ?? AddinLog.Path),
                        "ThreadModeler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception)
                {
                }
                throw;
            }

            // The update check must never affect loading.
            try
            {
                StartUpdateCheck();
            }
            catch (Exception ex)
            {
                AddinLog.Warn("Update check could not be started.", ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ActivateCore(Inventor.ApplicationAddInSite addInSiteObject)
        {
            // Initialize AddIn members.
            m_inventorApplication = addInSiteObject.Application;

            Type addinType = this.GetType();

            AdnInventorUtilities.Initialize(m_inventorApplication, addinType);

            Toolkit.Initialize(m_inventorApplication);
            ThreadWorker.Initialize(m_inventorApplication);

            AdnCommand.AddCommand(new ThreadModelerCmd(m_inventorApplication));

            AdnCommand.AddCommand(new ConfigCtrlCmd(m_inventorApplication));

            AdnCommand.AddCommand(new AboutCtrlCmd(m_inventorApplication));

            AdnCommand.AddCommand(new HelpCtrlCmd(m_inventorApplication));

            AdnCommand.AddCommand(new CheckUpdatesCmd(m_inventorApplication));

            // Only after all commands have been added,
            // load Ribbon UI from customized xml file.
            // Make sure "InternalName" of above commands is matching
            // "internalName" tag described in xml of corresponding command.
            AdnRibbonBuilder.CreateRibbon(
                m_inventorApplication,
                addinType,
                "ThreadModeler.resources.ribbons.xml");

            AddinLog.Info("Activated in Inventor " + SafeInventorVersion(m_inventorApplication) + ".");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void StartUpdateCheck()
        {
            UpdateChecker.Start(m_inventorApplication);
        }

        public void Deactivate()
        {
            // This method is called by Inventor when the AddIn is unloaded.
            // The AddIn will be unloaded either manually by the user or
            // when the Inventor session is terminated

            AddinLog.Info("Deactivate.");
            try
            {
                StopUpdateCheck();
            }
            catch (Exception ex)
            {
                AddinLog.Warn("Stopping the update check failed.", ex);
            }

            // Release objects.
            if (m_inventorApplication != null)
                Marshal.ReleaseComObject(m_inventorApplication);
            m_inventorApplication = null;
        }

        public void ExecuteCommand(int commandID)
        {
            // Note:this method is now obsolete, you should use the 
            // ControlDefinition functionality for implementing commands.
        }

        public object Automation
        {
            // This property is provided to allow the AddIn to expose an API 
            // of its own to other programs. Typically, this  would be done by
            // implementing the AddIn's API interface in a class and returning 
            // that class object through this property.

            get
            {
                // TODO: Add ApplicationAddInServer.Automation getter implementation
                return null;
            }
        }
        #endregion

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StopUpdateCheck()
        {
            UpdateChecker.Stop();
        }

        private static string SafeInventorVersion(Inventor.Application app)
        {
            try
            {
                return app.SoftwareVersion.DisplayVersion;
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        /// <summary>
        /// Writes load diagnostics to %TEMP%\ThreadModeler-load-error.txt. Returns the file path, or null.
        /// </summary>
        private static string WriteLoadError(string title, Exception ex, Inventor.ApplicationAddInSite site)
        {
            try
            {
                string inventorVersion = "unknown";
                try
                {
                    if (site != null)
                        inventorVersion = SafeInventorVersion(site.Application);
                }
                catch (Exception)
                {
                }

                string file = Path.Combine(Path.GetTempPath(), "ThreadModeler-load-error.txt");
                string text =
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + title + Environment.NewLine +
                    "ThreadModeler: " + typeof(StandardAddInServer).Assembly.GetName().Version + Environment.NewLine +
                    "Inventor:      " + inventorVersion + Environment.NewLine +
                    ".NET runtime:  " + RuntimeInformation.FrameworkDescription + Environment.NewLine +
                    "Location:      " + typeof(StandardAddInServer).Assembly.Location + Environment.NewLine +
                    "Log:           " + AddinLog.Path + Environment.NewLine +
                    Environment.NewLine + ex + Environment.NewLine + new string('-', 80) + Environment.NewLine;
                File.AppendAllText(file, text);
                return file;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
