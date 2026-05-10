using HRB.Platform.Client.WPF.ControlsAppModule.Core.ViewModels;
using HRB.Platform.Client.WPF.ControlsAppModule.Languages;
using HRB.Platform.Client.WPF.ControlsAppModule.Resources;
using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;

namespace HRB.Platform.Client.WPF.ControlsAppModule.Core
{


    public class ControlsCoreAppModule : BaseWpfCoreModule<ControlsAppContext, ControlsLogger, Translator, ResourcesGetter>
    {

        protected override string AppModuleName => GlobalSettings.APP_MODULE_NAME;

        protected override void OnSetAppContext(ControlsAppContext appContext)
        {
            GlobalSettings.SetPlatformAppContext(appContext);
        }


        protected override async Task OnInitializedAsync(IContainerProvider containerProvider)
        {

            await base.OnInitializedAsync(containerProvider);

        }



        protected override void OnRegisterTypes(IContainerRegistry containerRegistry)
        {

            base.OnRegisterTypes(containerRegistry);

            //containerRegistry.Register<TaskNotifyCardViewModel>();
            //containerRegistry.Register<TestDialogViewModel>();

        }

    }

}