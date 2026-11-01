using System;
using Inventor;
using ThreadModeler.Addin;
using ThreadModeler.Update;
using ThreadModeler.Utilities;

namespace ThreadModeler.Commands
{
    /// <summary>Ribbon command: checks GitHub Releases for a newer ThreadModeler now.</summary>
    class CheckUpdatesCmd : AdnButtonCommandBase
    {
        public CheckUpdatesCmd(Application application) : base(application)
        {
        }

        public override string DisplayName
        {
            get { return "Check for Updates"; }
        }

        public override string InternalName
        {
            get { return "ThreadModeler.CheckUpdatesCmd"; }
        }

        public override CommandTypesEnum Classification
        {
            get { return CommandTypesEnum.kEditMaskCmdType; }
        }

        public override string ClientId
        {
            get { return typeof(StandardAddInServer).GUID.ToString("B"); }
        }

        public override string Description
        {
            get { return "Checks whether a newer ThreadModeler version is available"; }
        }

        public override string ToolTipText
        {
            get { return "Checks whether a newer ThreadModeler version is available"; }
        }

        public override ButtonDisplayEnum ButtonDisplay
        {
            get { return ButtonDisplayEnum.kDisplayTextInLearningMode; }
        }

        public override string StandardIconName
        {
            get { return "ThreadModeler.resources.update.ico"; }
        }

        public override string LargeIconName
        {
            get { return "ThreadModeler.resources.update.ico"; }
        }

        protected override void OnExecute(NameValueMap context)
        {
            try
            {
                if (UpdateChecker.Instance != null)
                    UpdateChecker.Instance.CheckNow();
            }
            finally
            {
                Terminate();
            }
        }

        protected override void OnHelp(NameValueMap context)
        {
        }
    }
}
