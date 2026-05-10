using HRB.Payment.Core.ConstKeys;
using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core
{

    public class GlobalSettings : BaseWpfModuleGlobalSettings<PaymentAppContext>
    {


        public const string APP_MODULE_NAME = PlatformWpfAppModuleKeys.PAYMENT_APP_MODULE_NAME;


        /// <summary>
        /// 支付宝插件壳是否已启动的标志
        /// </summary>
        public static bool IsAlipayShellStarted = false;

        /// <summary>
        /// 支付宝轮询是否已启动的标志
        /// </summary>
        public static bool IsAlipayPoolingStarted = false;


    }

}
