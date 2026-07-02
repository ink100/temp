using HRB.Platform.Client.Core.ExtensionFunctions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Abstractions;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Plugins.WeChat
{
    /// <summary>
    /// 微信渠道设置项贡献者。
    /// 向设置页提供微信相关的配置项，设置页通过 IChannelSettingsContributor 动态收集并渲染。
    ///
    /// 贡献的设置项：
    ///   - wechat_enabled           : 微信启用开关（Toggle）
    ///   - wechat_nickname_reminder : 未支付昵称提醒播报开关（Toggle）
    ///   - wechat_auto_login        : 微信自动登录开关（Toggle）
    ///   - wechat_auto_hide         : 登录后自动隐藏微信窗口开关（Toggle）
    /// </summary>
    public sealed class WeChatSettingsContributor : IChannelSettingsContributor
    {
        public static class Keys
        {
            public const string Enabled = "wechat_enabled";
            public const string NicknameReminder = "wechat_nickname_reminder";
            public const string AutoLogin = "wechat_auto_login";
            public const string AutoHide = "wechat_auto_hide";
        }

        private readonly WeChatChannelPlugin _plugin;
        private readonly PaymentAppContext _appContext;

        public WeChatSettingsContributor(
            WeChatChannelPlugin plugin,
            PaymentAppContext appContext)
        {
            _plugin = plugin;
            _appContext = appContext;
        }

        public IReadOnlyList<ChannelSettingItem> GetSettings()
        {
            var settings = _appContext.CurrentSettings;
            var isAvailable = _plugin.IsAvailable;

            return
            [
                new ChannelSettingItem
                {
                    Key = Keys.Enabled,
                    Label = "微信收款播报",
                    Type = SettingType.Toggle,
                    CurrentValue = settings.IsWeChatEnabled,
                    IsEnabled = isAvailable,
                    DisabledHint = isAvailable ? null : "微信插件未就绪，等待启动",
                    Order = 20
                },
                new ChannelSettingItem
                {
                    Key = Keys.NicknameReminder,
                    Label = "未支付昵称提醒播报",
                    Type = SettingType.Toggle,
                    CurrentValue = settings.IsWeChatNicknameReminderEnabled,
                    IsEnabled = settings.IsWeChatEnabled,
                    DisabledHint = settings.IsWeChatEnabled ? null : "请先启用微信收款播报",
                    Order = 21
                },
                new ChannelSettingItem
                {
                    Key = Keys.AutoLogin,
                    Label = "自动登录微信（扫码后自动点击登录）",
                    Type = SettingType.Toggle,
                    CurrentValue = settings.IsWeChatAutoLoginEnabled,
                    IsEnabled = settings.IsWeChatEnabled,
                    DisabledHint = settings.IsWeChatEnabled ? null : "请先启用微信收款播报",
                    Order = 22
                },
                new ChannelSettingItem
                {
                    Key = Keys.AutoHide,
                    Label = "登录后自动隐藏微信窗口",
                    Type = SettingType.Toggle,
                    CurrentValue = settings.IsWeChatAutoHideEnabled,
                    IsEnabled = settings.IsWeChatEnabled,
                    DisabledHint = settings.IsWeChatEnabled ? null : "请先启用微信收款播报",
                    Order = 23
                }
            ];
        }

        public async Task OnSettingChangedAsync(string key, object value)
        {
            if (value is not bool boolValue) return;

            switch (key)
            {
                case Keys.Enabled:
                    var settings = _appContext.CurrentSettings;
                    settings.IsWeChatEnabled = boolValue;
                    settings.LastUpdateDateTime = DateTime.Now;
                    _appContext.SaveCurrentSettings(settings);

                    if (boolValue)
                        await _plugin.EnableAsync();
                    else
                        await _plugin.DisableAsync();
                    break;

                case Keys.NicknameReminder:
                    var s1 = _appContext.CurrentSettings;
                    s1.IsWeChatNicknameReminderEnabled = boolValue;
                    s1.LastUpdateDateTime = DateTime.Now;
                    _appContext.SaveCurrentSettings(s1);
                    break;

                case Keys.AutoLogin:
                    var s2 = _appContext.CurrentSettings;
                    s2.IsWeChatAutoLoginEnabled = boolValue;
                    s2.LastUpdateDateTime = DateTime.Now;
                    _appContext.SaveCurrentSettings(s2);
                    break;

                case Keys.AutoHide:
                    var s3 = _appContext.CurrentSettings;
                    s3.IsWeChatAutoHideEnabled = boolValue;
                    s3.LastUpdateDateTime = DateTime.Now;
                    _appContext.SaveCurrentSettings(s3);
                    break;
            }
        }
    }
}
