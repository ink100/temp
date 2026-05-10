using HRB.Platform.Client.WPF.ControlsAppModule.Core;
using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;

namespace HRB.Platform.Client.WPF.ControlsAppModule
{

    public class ControlsViewAppModule : BaseWpfViewModule<ControlsCoreAppModule>
    {

        protected override string AppModuleName => GlobalSettings.APP_MODULE_NAME;


        protected override async Task OnInitializedAsync(IContainerProvider containerProvider)
        {
            await base.OnInitializedAsync(containerProvider);
        }


        protected override void OnRegisterTypes(IContainerRegistry containerRegistry)
        {
            base.OnRegisterTypes(containerRegistry);

            //  containerRegistry.RegisterForNavigation<TestDialog, TestDialogViewModel>();
        }

    }
}