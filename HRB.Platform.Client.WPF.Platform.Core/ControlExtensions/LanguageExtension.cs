using HRB.Platform.Client.WPF.Core.ControlExtensions;

namespace HRB.Platform.Client.WPF.Platform.Core.ControlExtensions
{


    public class LanguageExtension : BaseWpfLanguageExtension
    {

        public LanguageExtension() : base(GlobalSettings.CurrentAppContext.CurrentTranslator)
        {

        }

    }

}
