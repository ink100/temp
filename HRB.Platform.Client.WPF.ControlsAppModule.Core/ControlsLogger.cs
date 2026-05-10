using HRB.Platform.Client.Core.Helpers;
using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;
using NLog;

namespace HRB.Platform.Client.WPF.ControlsAppModule.Core
{

    public class ControlsLogger : BaseWpfLogger
    {
        public ControlsLogger(ControlsAppContext appContext) : base(appContext)
        {
        }

        protected override LogFactory CreateLogFactory()
        {
            return PlatformLogsHelper.GetDefaultLogFactory(_CurrentAppContextDirectoryPath.CurrentPublicRootDirectoryFullPath);
        }
    }

}
