using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;
using HRB.Platform.Client.WPF.Platform.Core;

namespace HRB.Platform.Client.WPF.Platform
{
    public class PlatformViewAppModule : BaseWpfViewModule<PlatformCoreAppModule>
    {
        protected override string AppModuleName => GlobalSettings.APP_MODULE_NAME;


        protected override async Task OnInitializedAsync(IContainerProvider containerProvider)
        {
            await base.OnInitializedAsync(containerProvider);
        }


        protected override void OnRegisterTypes(IContainerRegistry containerRegistry)
        {
            base.OnRegisterTypes(containerRegistry);


            //containerRegistry.Register<TaskNotifyCardViewModel>();
        }
    }
}