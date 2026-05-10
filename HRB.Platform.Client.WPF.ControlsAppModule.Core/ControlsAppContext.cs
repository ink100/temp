using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;
using HRB.Platform.Client.WPF.Core.Services.IServices;

namespace HRB.Platform.Client.WPF.ControlsAppModule.Core
{


    public class ControlsAppContext : BaseWpfAppContext<ControlsLogger>
    {



        public ControlsAppContext(string appModuleName, IContainerProvider containerProvider, ITranslator translator, IWpfResourcesGetter resourcesGetter, IWpfCommonRequestService commonRequestService)
            : base(appModuleName, containerProvider, translator, resourcesGetter, commonRequestService)
        {

        }

    }

}
