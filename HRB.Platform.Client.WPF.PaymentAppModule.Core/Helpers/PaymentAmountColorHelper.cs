using System;
using System.Collections.Concurrent;
using WpfBrush = System.Windows.Media.Brush;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Helpers
{
    /// <summary>
    /// 支付金额颜色辅助类。
    /// 用于根据支付渠道和状态返回金额显示颜色，并缓存 WPF Brush，避免频繁创建对象。
    /// </summary>
    public static class PaymentAmountColorHelper
    {
        private const string DefaultAlipayAmountColor = "#3E86FD";
        private const string DefaultAlipayFailedAmountColor = "#F01F1B";
        private const string DefaultWeChatAmountColor = "#00897B";
        private const string DefaultWeChatFailedAmountColor = "#F01F1B";
        private const string DefaultLegacyAmountColor = "#F01F1B";

        private static readonly ConcurrentDictionary<string, WpfBrush> BrushCache = new();

        private static string _alipayAmountColorHex = DefaultAlipayAmountColor;
        private static string _alipayFailedAmountColorHex = DefaultAlipayFailedAmountColor;
        private static string _weChatAmountColorHex = DefaultWeChatAmountColor;
        private static string _weChatFailedAmountColorHex = DefaultWeChatFailedAmountColor;
        private static string _legacyAmountColorHex = DefaultLegacyAmountColor;

        /// <summary>
        /// 应用设置页保存的金额颜色配置。
        /// </summary>
        public static void ApplySettings(
            string? alipayAmountColorHex,
            string? alipayFailedAmountColorHex,
            string? weChatAmountColorHex,
            string? weChatFailedAmountColorHex,
            string? legacyAmountColorHex = null)
        {
            _alipayAmountColorHex = NormalizeColorHex(alipayAmountColorHex, DefaultAlipayAmountColor);
            _alipayFailedAmountColorHex = NormalizeColorHex(alipayFailedAmountColorHex, DefaultAlipayFailedAmountColor);
            _weChatAmountColorHex = NormalizeColorHex(weChatAmountColorHex, DefaultWeChatAmountColor);
            _weChatFailedAmountColorHex = NormalizeColorHex(weChatFailedAmountColorHex, DefaultWeChatFailedAmountColor);
            _legacyAmountColorHex = NormalizeColorHex(legacyAmountColorHex, DefaultLegacyAmountColor);

            ClearCache();
        }

        /// <summary>
        /// 根据支付渠道和支付状态获取金额颜色。
        /// channel 可以传 PaymentChannel、int 或 string；status 可以传枚举、int 或 string。
        /// </summary>
        public static WpfBrush GetAmountBrush(object? channel, object? status)
        {
            var isWeChat = IsWeChatChannel(channel);
            var isAlipay = IsAlipayChannel(channel);
            var isFailed = IsFailedStatus(status);

            string colorHex;
            string fallback;

            if (isWeChat)
            {
                colorHex = isFailed ? _weChatFailedAmountColorHex : _weChatAmountColorHex;
                fallback = isFailed ? DefaultWeChatFailedAmountColor : DefaultWeChatAmountColor;
            }
            else if (isAlipay)
            {
                colorHex = isFailed ? _alipayFailedAmountColorHex : _alipayAmountColorHex;
                fallback = isFailed ? DefaultAlipayFailedAmountColor : DefaultAlipayAmountColor;
            }
            else
            {
                colorHex = _legacyAmountColorHex;
                fallback = DefaultLegacyAmountColor;
            }

            return GetCachedBrush(colorHex, fallback);
        }

        /// <summary>
        /// 清空 Brush 缓存。设置颜色变更后调用，确保界面能使用新颜色。
        /// </summary>
        public static void ClearCache()
        {
            BrushCache.Clear();
        }

        private static bool IsAlipayChannel(object? channel)
        {
            if (channel == null)
                return false;

            var text = channel.ToString() ?? string.Empty;

            if (text.Contains("Alipay", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("支付宝", StringComparison.OrdinalIgnoreCase))
                return true;

            // 兼容数据库中 PaymentChannel = 1 表示支付宝的情况。
            if (int.TryParse(text, out var value))
                return value == 1;

            return false;
        }

        private static bool IsWeChatChannel(object? channel)
        {
            if (channel == null)
                return false;

            var text = channel.ToString() ?? string.Empty;

            if (text.Contains("WeChat", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("Wechat", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("Wx", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("微信", StringComparison.OrdinalIgnoreCase))
                return true;

            // 兼容数据库中 PaymentChannel = 2 表示微信的情况。
            if (int.TryParse(text, out var value))
                return value == 2;

            return false;
        }

        private static bool IsFailedStatus(object? status)
        {
            if (status == null)
                return false;

            var text = status.ToString() ?? string.Empty;

            // 明确成功状态
            if (text.Contains("Success", StringComparison.OrdinalIgnoreCase))
                return false;

            if (text.Contains("Paid", StringComparison.OrdinalIgnoreCase))
                return false;

            if (text.Contains("Complete", StringComparison.OrdinalIgnoreCase))
                return false;

            if (text.Contains("完成", StringComparison.OrdinalIgnoreCase))
                return false;

            if (text.Contains("成功", StringComparison.OrdinalIgnoreCase))
                return false;

            if (text.Contains("已支付", StringComparison.OrdinalIgnoreCase))
                return false;

            // 明确失败/取消状态
            if (text.Contains("Fail", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("Cancel", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("Cancelled", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("失败", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("取消", StringComparison.OrdinalIgnoreCase))
                return true;

            // 兼容 PaymentStatus 默认枚举：
            // Scan = 0
            // Success = 1
            // Cancel = 2
            if (int.TryParse(text, out var value))
                return value == 2 || value < 0;

            return false;
        }

        private static WpfBrush GetCachedBrush(string? colorHex, string fallback)
        {
            var normalized = NormalizeColorHex(colorHex, fallback);

            return BrushCache.GetOrAdd(normalized, hex =>
            {
                var brush = new WpfSolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString(hex));
                brush.Freeze();
                return brush;
            });
        }

        private static string NormalizeColorHex(string? colorHex, string fallback)
        {
            var value = string.IsNullOrWhiteSpace(colorHex)
                ? fallback
                : colorHex.Trim();

            if (!value.StartsWith("#"))
                value = "#" + value;

            try
            {
                _ = (WpfColor)WpfColorConverter.ConvertFromString(value);
                return value.ToUpperInvariant();
            }
            catch
            {
                return fallback.ToUpperInvariant();
            }
        }
    }
}
