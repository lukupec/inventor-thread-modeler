using System;
using Inventor;
using ThreadModeler.Addin;
using ThreadModeler.Utilities;

namespace ThreadModeler.Commands
{
    /// <summary>Ribbon command: assigns the ISO and BSW thread profile templates.</summary>
    class ConfigCtrlCmd : AdnButtonCommandBase
    {
        public ConfigCtrlCmd(Application application) : base(application)
        {
        }

        public override string DisplayName
        {
            get { return "Configuration"; }
        }

        public override string InternalName
        {
            get { return "ThreadModeler.ConfigCtrlCmd"; }
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
            get { return "Assigns the ISO and BSW thread profile templates"; }
        }

        public override string ToolTipText
        {
            get { return "Assigns the ISO and BSW thread profile templates"; }
        }

        public override ButtonDisplayEnum ButtonDisplay
        {
            get { return ButtonDisplayEnum.kDisplayTextInLearningMode; }
        }

        public override string StandardIconName
        {
            get { return "ThreadModeler.resources.settings.ico"; }
        }

        public override string LargeIconName
        {
            get { return "ThreadModeler.resources.settings.ico"; }
        }

        protected override void OnExecute(NameValueMap context)
        {
            RegisterCommandForm(new ConfigForm(), true);
        }

        protected override void OnHelp(NameValueMap context)
        {
        }
    }
}
