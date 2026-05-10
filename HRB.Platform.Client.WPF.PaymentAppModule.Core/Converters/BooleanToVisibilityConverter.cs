using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Converters
{
    /// <summary>
    /// 布尔值转可见性转换器
    /// 将 true 转换为 Visible，false 转换为 Collapsed
    /// 支持 Inverse 参数进行反向转换
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 正向转换：布尔值转可见性
        /// </summary>
        /// <param name="value">输入的布尔值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数（"Inverse" 表示反向转换）</param>
        /// <param name="culture">区域信息</param>
        /// <returns>Visibility 枚举值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;

            if (value is bool b)
            {
                boolValue = b;
            }

            // 检查是否需要反向转换
            bool inverse = parameter != null && parameter.ToString()?.ToLower() == "inverse";

            if (inverse)
            {
                boolValue = !boolValue;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 反向转换：可见性转布尔值
        /// </summary>
        /// <param name="value">输入的 Visibility 值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数（"Inverse" 表示反向转换）</param>
        /// <param name="culture">区域信息</param>
        /// <returns>布尔值</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool boolValue = visibility == Visibility.Visible;

                // 检查是否需要反向转换
                bool inverse = parameter != null && parameter.ToString()?.ToLower() == "inverse";

                if (inverse)
                {
                    boolValue = !boolValue;
                }

                return boolValue;
            }

            return false;
        }
    }
}
