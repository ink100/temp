using HRB.Platform.Client.Core.ExtensionFunctions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Abstractions;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Plugins.Alipay
{
    /// <summary>
    /// 支付宝渠道设置项贡献者。
    /// 向设置页提供支付宝相关的配置项，设置页通过 IChannelSettingsContributor 动态收集并渲染。
    ///
    /// 贡献的设置项：
    ///   - alipay_enabled           : 支付宝启用开关（Toggle）
    ///   - alipay_nickname_reminder : 未支付昵称提醒播报开关（Toggle）
    /// </summary>
    public sealed class AlipaySettingsContributor : IChannelSettingsContributor
    {
        /// <summary>
        /// 设置项 Key 常量，供 ViewModel 和插件内部引用
        /// </summary>
        public static class Keys
        {
            public const string Enabled = "alipay_enabled";
            public const string NicknameReminder = "alipay_nickname_reminder";
        }

        private readonly AlipayChannelPlugin _plugin;
        private readonly PaymentAppContext _appContext;

        public AlipaySettingsContributor(
            AlipayChannelPlugin plugin,
            PaymentAppContext appContext)
        {
            _plugin = plugin;
            _appContext = appContext;
        }

        /// <summary>
        /// 返回支付宝渠道的设置项列表
        /// </summary>
        public IReadOnlyList<ChannelSettingItem> GetSettings()
        {
            var settings = _appContext.CurrentSettings;
            var isAvailable = _plugin.IsAvailable;

            return
            [
                new ChannelSettingItem
                {
                    Key = Keys.Enabled,
                    Label = "支付宝收款播报",
                    Type = SettingType.Toggle,
                    CurrentValue = _plugin.IsEnabled,
                    IsEnabled = isAvailable,
                    DisabledHint = isAvailable ? null : "支付宝插件未就绪，等待启动",
                    Order = 10
                },
                new ChannelSettingItem
                {
                    Key = Keys.NicknameReminder,
                    Label = "未支付昵称提醒播报",
                    Type = SettingType.Toggle,
                    CurrentValue = settings.IsAlipayNicknameReminderEnabled,
                    IsEnabled = false,
                    DisabledHint = _plugin.IsEnabled ? null : "请先启用支付宝收款播报",
                    Order = 11
                }
            ];
        }

        /// <summary>
        /// 设置项变更回调。由设置页 ViewModel 在用户操作时调用。
        /// </summary>
        public async Task OnSettingChangedAsync(string key, object value)
        {
            if (value is not bool boolValue)
                return;
            var settings = _appContext.CurrentSettings;
            switch (key)
            {
                case Keys.Enabled:

                    settings.IsAlipayEnabled = boolValue;
                    _appContext.SaveCurrentSettings(settings);
                    if (boolValue)
                        await _plugin.EnableAsync();
                    else
                        await _plugin.DisableAsync();
                    break;

                case Keys.NicknameReminder:
                    settings.IsAlipayNicknameReminderEnabled = boolValue;
                    _appContext.SaveCurrentSettings(settings);
                    break;
            }
        }
    }
}
