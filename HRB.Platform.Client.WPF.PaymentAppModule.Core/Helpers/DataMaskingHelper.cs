using System.Security.Cryptography;
using System.Text;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Helpers
{
    /// <summary>
    /// 数据脱敏工具类
    /// </summary>
    public static class DataMaskingHelper
    {
        /// <summary>
        /// 脱敏订单号
        /// 使用MD5哈希值完全替代真实订单号
        /// </summary>
        /// <param name="orderNumber">原始订单号</param>
        /// <returns>脱敏后的订单号（MD5哈希值）</returns>
        public static string MaskOrderNumber(string orderNumber)
        {
            if (string.IsNullOrEmpty(orderNumber))
            {
                return string.Empty;
            }

            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(orderNumber);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// 脱敏用户昵称
        /// 中文：保留首尾各1个字符，中间用星号替代
        /// 英文/数字：保留前2位和后2位，中间用星号替代
        /// </summary>
        /// <param name="nickname">原始昵称</param>
        /// <returns>脱敏后的昵称</returns>
        public static string MaskNickname(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return string.Empty;
            }

            // 判断是否包含中文字符
            bool hasChinese = nickname.Any(c => c >= 0x4E00 && c <= 0x9FA5);

            if (hasChinese)
            {
                // 中文昵称处理
                if (nickname.Length <= 2)
                {
                    // 两个字的昵称，只保留第一个字
                    return $"{nickname[0]}*";
                }
                else
                {
                    // 保留首尾各1个字符
                    return $"{nickname[0]}{new string('*', nickname.Length - 2)}{nickname[^1]}";
                }
            }
            else
            {
                // 英文/数字昵称处理
                if (nickname.Length <= 4)
                {
                    // 太短的昵称，只保留首字符
                    return $"{nickname[0]}{new string('*', nickname.Length - 1)}";
                }
                else
                {
                    // 保留前2位和后2位
                    string prefix = nickname.Substring(0, 2);
                    string suffix = nickname.Substring(nickname.Length - 2);
                    int maskLength = nickname.Length - 4;

                    return $"{prefix}{new string('*', maskLength)}{suffix}";
                }
            }
        }

        /// <summary>
        /// 脱敏用户ID
        /// 使用MD5哈希值完全替代真实ID
        /// </summary>
        /// <param name="userId">原始用户ID</param>
        /// <returns>脱敏后的用户ID（MD5哈希值）</returns>
        public static string MaskUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return string.Empty;
            }

            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(userId);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }
}
