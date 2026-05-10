using HRB.Platform.Client.WPF.PaymentAppModule.Core.DtoModels;
using System;
using System.Collections.Generic;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Configuration
{
    public static class PluginLifecycleModeHelper
    {
        private const string LifecycleModeEnvKey = "HRB_PAYMENT_PLUGIN_LIFECYCLE_MODE";

        private static readonly HashSet<string> NewModeValues = new(StringComparer.OrdinalIgnoreCase)
        {
            "new",
            "watchdog",
            "true",
            "1",
            "on",
            "enabled"
        };

        private static readonly HashSet<string> LegacyModeValues = new(StringComparer.OrdinalIgnoreCase)
        {
            "legacy",
            "old",
            "false",
            "0",
            "off",
            "disabled"
        };

        public static bool IsNewModeEnabled(SettingsDto? settings)
        {
            var envMode = Environment.GetEnvironmentVariable(LifecycleModeEnvKey);
            if (!string.IsNullOrWhiteSpace(envMode))
            {
                var mode = envMode.Trim();

                if (NewModeValues.Contains(mode))
                {
                    return true;
                }

                if (LegacyModeValues.Contains(mode))
                {
                    return false;
                }
            }

            return settings?.UseNewPluginLifecycleMode ?? false;
        }
    }
}
