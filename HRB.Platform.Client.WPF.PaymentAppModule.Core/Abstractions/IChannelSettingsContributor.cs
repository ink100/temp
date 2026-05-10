namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Abstractions
{
    /// <summary>
    /// 插件向设置页贡献配置项。
    /// 设置页通过此接口动态收集各渠道的设置项，无需硬编码。
    /// </summary>
    public interface IChannelSettingsContributor
    {
        /// <summary>
        /// 返回该渠道的设置项列表
        /// </summary>
        IReadOnlyList<ChannelSettingItem> GetSettings();

        /// <summary>
        /// 设置项变更回调
        /// </summary>
        Task OnSettingChangedAsync(string key, object value);
    }
}
