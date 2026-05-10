using HRB.Platform.Client.WPF.ControlsAppModule.Languages;
using HRB.Platform.Client.WPF.Core.ControlExtensions;

namespace HRB.Platform.Client.WPF.ControlsAppModule.Core.ControlExtensions
{



    public class LanguageEnumExtension : BaseWpfLanguageEnumExtension<LanguageEnums>
    {
        public LanguageEnumExtension() : base(GlobalSettings.CurrentAppContext.CurrentTranslator)
        {
        }
    }

}
