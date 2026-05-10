using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Services;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels;
using HRB.Platform.Client.WPF.PaymentAppModule.Views;

namespace HRB.Platform.Client.WPF.PaymentAppModule
{

    public class PaymentViewAppModule : BaseWpfViewModule<PaymentCoreAppModule>
    {

        protected override string AppModuleName => GlobalSettings.APP_MODULE_NAME;


        protected override async Task OnInitializedAsync(IContainerProvider containerProvider)
        {
            await base.OnInitializedAsync(containerProvider);
        }


        protected override void OnRegisterTypes(IContainerRegistry containerRegistry)
        {
            base.OnRegisterTypes(containerRegistry);
            containerRegistry.RegisterSingleton<INotificationService, NotificationService>();
            containerRegistry.RegisterSingleton<ILoadingOverlayService, LoadingOverlayService>();
            containerRegistry.RegisterSingleton<IPluginKeepAliveService, PluginKeepAliveService>();

            // 注册 Dialog
            containerRegistry.RegisterDialog<MessageDialog, MessageDialogViewModel>();
            containerRegistry.RegisterDialog<UserAgreementDialog, UserAgreementDialogViewModel>();
            containerRegistry.RegisterDialog<NotificationConsentDialog, NotificationConsentDialogViewModel>();
        }

    }
}