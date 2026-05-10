using Lanymy.Common.Helpers;
using System.IO;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Configuration
{
    /// <summary>
    /// 插件配置
    /// </summary>
    public static class PluginSettings
    {
        /// <summary>
        /// 插件根目录
        /// </summary>
        public static readonly string PluginRootPath;

        /// <summary>
        /// 总服务路径（必须最先启动）
        /// </summary>
        public static readonly string MessageServicePath;

        /// <summary>
        /// 支付宝通知插件路径
        /// </summary>
        public static readonly string AlipayShellPath;



        /// <summary>
        /// 微信通知插件路径
        /// </summary>
        public static readonly string WeChatShellPath;


        /// <summary>
        /// 消息中心插件执行exe
        /// </summary>
        public const string MessageServiceExe = "HRB.Payment.Message.Service.Client.exe";

        /// <summary>
        /// 微信模块执行exe
        /// </summary>
        public const string WeChatShellExe = "VXModule.Shell.exe";

        /// <summary>
        /// 支付宝模块执行exe
        /// </summary>
        public const string AlipayShellExe = "HRB.Payment.Alipay.Shell.Client.exe";



        /// <summary>
        /// 微信Exe
        /// </summary>
        public const string WeChatExe = "WeChat.exe";




        static PluginSettings()
        {
            // 获取应用程序根目录
            var rootPath = PathHelper.GetCallDomainPath();

            // 插件根目录在应用根目录的上一层
            PluginRootPath = PathHelper.CombineRelativePath(rootPath, "../");

            // 总服务（与应用根目录同级）
            MessageServicePath = Path.Combine(PluginRootPath, "HRB.Payment.Message.Service.Client", MessageServiceExe);


            // 支付宝插件（与应用根目录同级）
            AlipayShellPath = Path.Combine(PluginRootPath, "Alipay.Shell", AlipayShellExe);


            // 微信插件（与应用根目录同级）
            WeChatShellPath = Path.Combine(PluginRootPath, "VXModule.Shell", WeChatShellExe);

            var validate = ValidatePlugins();

            if (!validate.IsValid)
            {
                throw new IOException($"{validate.MissingPlugin} 不存在");
            }

        }

        /// <summary>
        /// 验证所有插件文件是否存在
        /// </summary>
        public static (bool IsValid, string MissingPlugin) ValidatePlugins()
        {
            if (!File.Exists(MessageServicePath))
                return (false, MessageServiceExe);

            if (!File.Exists(AlipayShellPath))
                return (false, AlipayShellExe);

            if (!File.Exists(WeChatShellPath))
                return (false, WeChatShellExe);

            return (true, "");
        }
    }
}
