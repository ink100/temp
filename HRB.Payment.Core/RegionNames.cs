using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRB.Payment.Core
{
    public static class RegionNames
    {
        /// <summary>
        /// ZIndex最底层，保留层
        /// </summary>
        public const string ZIndex0Region = nameof(ZIndex0Region);

        /// <summary>
        /// 内容层
        /// </summary>
        public const string ContentRegion = nameof(ContentRegion);


        /// <summary>
        /// 任务弹窗层
        /// </summary>
        public const string TaskNotifyRegion = nameof(TaskNotifyRegion);

        /// <summary>
        /// 弹窗层
        /// </summary>
        public const string DialogRegion = nameof(DialogRegion);

        /// <summary>
        /// Loading层
        /// </summary>
        public const string LoadingRegion = nameof(LoadingRegion);

        /// <summary>
        /// Toast层
        /// </summary>
        public const string ToastRegion = nameof(ToastRegion);
    }
}
