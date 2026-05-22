using System;
using System.Collections.Concurrent;
using System.Windows.Media;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Helpers
{
    public static class PaymentAmountColorHelper
    {
        private const string DefaultAlipayAmountColor = "#3E86FD";
        private const string DefaultAlipayFailedAmountColor = "#F01F1B";
        private const string DefaultWeChatAmountColor = "#00897B";
        private const string DefaultWeChatFailedAmountColor = "#F01F1B";
        private const string DefaultLegacyAmountColor = "#F01F1B";

        private static readonly ConcurrentDictionary<string, Brush> BrushCache = new();

        private static string _alipayAmountColorHex = DefaultAlipayAmountColor;
        private static string _alipayFailedAmountColorHex = DefaultAlipayFailedAmountColor;
        private static string _weChatAmountColorHex = DefaultWeChatAmountColor;
        private static string _weChatFailedAmountColorHex = DefaultWeChatFailedAmountColor;
        private static string _legacyAmountColorHex = DefaultLegacyAmountColor;

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

        public static Brush GetAmountBrush(object? channel, object? status)
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

            if (int.TryParse(text, out var value))
                return value == 2;

            return false;
        }

        private static bool IsFailedStatus(object? status)
        {
            if (status == null)
                return false;

            var text = status.ToString() ?? string.Empty;

            if (text.Contains("Fail", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("Cancel", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("Cancelled", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("失败", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("取消", StringComparison.OrdinalIgnoreCase))
                return true;

            if (text.Contains("超时", StringComparison.OrdinalIgnoreCase))
                return true;

            if (int.TryParse(text, out var value))
                return value != 0;

            return false;
        }

        private static Brush GetCachedBrush(string? colorHex, string fallback)
        {
            var normalized = NormalizeColorHex(colorHex, fallback);

            return BrushCache.GetOrAdd(normalized, hex =>
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
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
                _ = (Color)ColorConverter.ConvertFromString(value);
                return value.ToUpperInvariant();
            }
            catch
            {
                return fallback.ToUpperInvariant();
            }
        }
    }
}