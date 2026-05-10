using HRB.Payment.Core.Services;
using HRB.Platform.Client.Core.ExtensionFunctions;
using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.Core.Instruments;
using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DtoModels;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Plugins.Alipay;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Plugins.WeChat;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Repository;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Services;
using HRB.Platform.Client.WPF.PaymentAppModule.Languages;
using HRB.Platform.Client.WPF.PaymentAppModule.Resources;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core
{
    public class PaymentCoreAppModule : BaseWpfCoreModule<PaymentAppContext, PaymentLogger, Translator, ResourcesGetter>
    {
        protected override string AppModuleName => GlobalSettings.APP_MODULE_NAME;

        protected override void OnSetAppContext(PaymentAppContext appContext)
        {
            GlobalSettings.SetPlatformAppContext(appContext);
        }


        protected override async Task OnInitializedAsync(IContainerProvider containerProvider)
        {

            await base.OnInitializedAsync(containerProvider);

            var cprRepository = containerProvider.Resolve<IPaymentRepository>();



        }


        protected override void OnRegisterTypes(IContainerRegistry containerRegistry)
        {
            base.OnRegisterTypes(containerRegistry);

            containerRegistry.RegisterRepository<PaymentAppContext, PaymentDbContext, IPaymentRepository, PaymentRepository>(EnvironmentSettings.DB_SECRET_KEY);
            containerRegistry.RegisterSettings<PaymentAppContext, SettingsDto>();

            //containerRegistry.RegisterSingleton<ITransactionService, Services.TransactionService>();
            containerRegistry.RegisterSingleton<IPaymentEventPublisher, PaymentEventPublisher>();
            containerRegistry.RegisterSingleton<INumberToSpeechService, NumberToSpeechService>();
            containerRegistry.RegisterSingleton<IPaymentVoiceService, PaymentVoiceService>();
            containerRegistry.RegisterSingleton<IOrderStateManager, OrderStateManager>();
            containerRegistry.RegisterSingleton<ITimerCoordinator, TimerCoordinator>();
            containerRegistry.RegisterSingleton<IPaymentChannelCoordinator, PaymentChannelCoordinator>();


            containerRegistry.RegisterSingleton<IWeChatMessageParser, WeChatMessageParser>();
            containerRegistry.RegisterSingleton<IPaymentMessageConverter, PaymentMessageConverter>();
            containerRegistry.RegisterSingleton<IWeChatConnectionService, WeChatConnectionService>();
            containerRegistry.RegisterSingleton<IPaymentNotificationHandler, PaymentNotificationHandler>();
            containerRegistry.RegisterSingleton<ILicenseService, LicenseService>();
            //containerRegistry.RegisterSingleton<IAlipayConfigService, AlipayConfigService>();
            containerRegistry.RegisterSingleton<IPluginProcessService, PluginProcessService>();
            containerRegistry.RegisterSingleton<IPluginRuntimeStatusService, PluginRuntimeStatusService>();
            //containerRegistry.RegisterSingleton<IAlipayTradeService, AlipayTradeService>();
            //containerRegistry.RegisterSingleton<IAlipayBillPollingService, AlipayBillPollingService>();
            containerRegistry.RegisterSingleton<IWeChatService, WeChatService>();
            containerRegistry.RegisterSingleton<ITtsService, TtsService>();
            containerRegistry.RegisterSingleton<IHttpNotificationService, HttpNotificationService>();

            // 2.0 新增
            containerRegistry.RegisterSingleton<IPaymentTransactionService, PaymentTransactionService>();
            containerRegistry.RegisterSingleton<AlipayChannelPlugin>();
            containerRegistry.RegisterSingleton<AlipaySettingsContributor>();
            containerRegistry.RegisterSingleton<WeChatChannelPlugin>();
            containerRegistry.RegisterSingleton<WeChatSettingsContributor>();

        }
    }
}