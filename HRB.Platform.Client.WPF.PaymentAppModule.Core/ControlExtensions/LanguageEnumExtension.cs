using HRB.Platform.Client.WPF.Core.ControlExtensions;
using HRB.Platform.Client.WPF.PaymentAppModule.Languages;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.ControlExtensions
{



    public class LanguageEnumExtension : BaseWpfLanguageEnumExtension<LanguageEnums>
    {
        public LanguageEnumExtension() : base(GlobalSettings.CurrentAppContext.CurrentTranslator)
        {
        }
    }

}
