using HRB.Platform.Client.WPF.Core.ControlExtensions;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.ControlExtensions
{



    public class ImageResourceExtension : BaseWpfImageResourceExtension
    {

        public ImageResourceExtension() : base(GlobalSettings.CurrentAppContext.CurrentResourcesGetter)
        {

        }


    }


}
