using HRB.Platform.Client.Core.ExtensionFunctions;
using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.ControlsAppModule;
using HRB.Platform.Client.WPF.Core.ExtensionFunctions;
using HRB.Platform.Client.WPF.Core.Instruments;
using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;
using HRB.Platform.Client.WPF.Platform.Languages;
using HRB.Platform.Client.WPF.Platform.Resources;

namespace HRB.Platform.Client.WPF.Platform.Core
{
    public class PlatformCoreAppModule : BaseWpfCoreModule<PlatformWpfAppContext, PlatformWpfLogger, Translator, ResourcesGetter>
    {
        protected override string AppModuleName => GlobalSettings.APP_MODULE_NAME;

        protected override void OnSetAppContext(PlatformWpfAppContext appContext)
        {
            GlobalSettings.SetPlatformAppContext(appContext);
        }


        protected override async Task OnInitializedAsync(IContainerProvider containerProvider)
        {

            await base.OnInitializedAsync(containerProvider);

            containerProvider.LoadModule<ControlsViewAppModule>();

        }


        protected override void OnRegisterTypes(IContainerRegistry containerRegistry)
        {

            base.OnRegisterTypes(containerRegistry);

            containerRegistry.RegisterSingleton<IAudioPlayer, AudioPlayerWPF>();

            //containerRegistry.RegisterSingleton<ILoadingService, LoadingService>();

            //containerRegistry.RegisterSingleton<IToastService, ToastService>();

            //containerRegistry.RegisterSingleton<ITaskNotifyServices, TaskNotifyServices>();

            //containerRegistry.RegisterSingleton<IPlatformDialogService, PlatformDialogService>();

            containerRegistry.RegisterSettings<PlatformWpfAppContext, PlatformWpfSettingsDto>();





        }
    }
}