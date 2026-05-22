using System;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Helpers
{
    public static class PaymentDisplaySettingsChangedNotifier
    {
        public static event Action? Changed;

        public static void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}