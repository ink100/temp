using System;
using System.Collections.Generic;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.DtoModels
{
    public sealed class CursorPageResult<T>
    {
        public List<T> Items { get; set; } = new();

        public bool HasMore { get; set; }

        /// <summary>
        /// 下一次继续查询时使用的游标时间。
        /// 注意：这是本次扫描候选集中的最后一条记录时间，不一定是 Items 里的最后一条。
        /// </summary>
        public DateTime? NextCursorTime { get; set; }

        /// <summary>
        /// 下一次继续查询时使用的游标 Id。
        /// </summary>
        public int NextCursorId { get; set; }
    }
}