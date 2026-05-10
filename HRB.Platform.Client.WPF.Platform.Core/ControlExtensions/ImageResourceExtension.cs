using HRB.Platform.Client.WPF.Core.ControlExtensions;

namespace HRB.Platform.Client.WPF.Platform.Core.ControlExtensions
{



    public class ImageResourceExtension : BaseWpfImageResourceExtension
    {

        public ImageResourceExtension() : base(GlobalSettings.CurrentAppContext.CurrentResourcesGetter)
        {

        }


    }


}
