using HRB.Platform.Client.Core.Helpers;
using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;
using NLog;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core
{

    public class PaymentLogger : BaseWpfLogger
    {
        public PaymentLogger(PaymentAppContext appContext) : base(appContext)
        {
        }

        protected override LogFactory CreateLogFactory()
        {
            return PlatformLogsHelper.GetDefaultLogFactory(_CurrentAppContextDirectoryPath.CurrentPublicRootDirectoryFullPath);
        }
    }

}
