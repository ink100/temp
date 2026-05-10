using System;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    public interface IPluginRuntimeStatusService
    {
        event EventHandler? StatusChanged;

        bool IsMessageCenterRunning { get; }
        DateTime? MessageCenterLastSeenAt { get; }

        bool IsAlipayPluginRunning { get; }
        bool HasAlipayAppStarted { get; }
        DateTime? AlipayLastReplyAt { get; }

        bool IsWeChatPluginRunning { get; }
        DateTime? WeChatLastReplyAt { get; }

        /// <summary>
        /// Wait until message center process is detected as running.
        /// </summary>
        Task WaitForMessageCenterRunningAsync(CancellationToken ct = default);
    }
}
