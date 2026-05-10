using System.Globalization;
using System.Windows.Data;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Converters
{
    /// <summary>
    /// 布尔值取反转换器
    /// 将 true 转换为 false，false 转换为 true
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        /// <summary>
        /// 正向转换：布尔值取反
        /// </summary>
        /// <param name="value">输入的布尔值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">区域信息</param>
        /// <returns>取反后的布尔值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        /// <summary>
        /// 反向转换：布尔值取反
        /// </summary>
        /// <param name="value">输入的布尔值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">区域信息</param>
        /// <returns>取反后的布尔值</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
}
