using System;
using System.Globalization;
using System.Windows.Data;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Helpers;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Converters
{
    public class PaymentAmountBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            object? channel = values.Length > 0 ? values[0] : null;
            object? status = values.Length > 1 ? values[1] : null;

            return PaymentAmountColorHelper.GetAmountBrush(channel, status);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
