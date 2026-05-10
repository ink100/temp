using HRB.Platform.Client.WPF.Core.ControlExtensions;
using HRB.Platform.Client.WPF.Platform.Languages;

namespace HRB.Platform.Client.WPF.Platform.Core.ControlExtensions
{



    public class LanguageEnumExtension : BaseWpfLanguageEnumExtension<LanguageEnums>
    {
        public LanguageEnumExtension() : base(GlobalSettings.CurrentAppContext.CurrentTranslator)
        {
        }
    }

}
