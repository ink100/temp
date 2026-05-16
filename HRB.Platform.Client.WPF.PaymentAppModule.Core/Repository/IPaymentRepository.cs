using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels;
using HRB.Payment.Core.Models;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Repository
{
    public interface IPaymentRepository : IRepository
    {
        #region 支付宝配置管理

        /// <summary>
        /// 保存或更新支付宝配置
        /// </summary>
        /// <param name="config">配置信息</param>
        /// <returns>是否成功</returns>
        Task<bool> SaveAlipayConfig(AlipayAppInfoModel config);

        /// <summary>
        /// 保存成绩
        /// </summary>
        /// <param name="dbo"></param>
        /// <returns></returns>
        Task<bool> SaveTestAsync(TestDemoDbo dbo);

        /// <summary>
        /// 获取成绩列表
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<TestDemoDbo>> GetAllTestAsync();

        /// <summary>
        /// 根据登录账号获取配置
        /// </summary>
        /// <param name="loginAccount">登录账号</param>
        /// <returns>配置信息，不存在返回null</returns>
        Task<AlipayAppInfoModel> GetAlipayConfigByAccount(string loginAccount);

        /// <summary>
        /// 获取所有支付宝配置
        /// </summary>
        /// <returns>配置列表</returns>
        Task<IEnumerable<AlipayAppInfoModel>> GetAllAlipayConfigs();

        /// <summary>
        /// 删除支付宝配置
        /// </summary>
        /// <param name="loginAccount">登录账号</param>
        /// <returns>是否成功</returns>
        Task<bool> DeleteAlipayConfig(string loginAccount);

        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        /// <param name="loginAccount">登录账号</param>
        /// <returns>是否存在</returns>
        Task<bool> AlipayConfigExists(string loginAccount);

        #endregion

        #region 插件日志管理

        /// <summary>
        /// 添加插件日志
        /// </summary>
        /// <param name="log">日志信息</param>
        /// <returns>是否成功</returns>
        Task<bool> AddPluginLog(PluginLogModel log);

        /// <summary>
        /// 获取指定插件的日志
        /// </summary>
        /// <param name="pluginName">插件名称</param>
        /// <param name="limit">获取数量限制</param>
        /// <returns>日志列表</returns>
        Task<IEnumerable<PluginLogModel>> GetPluginLogs(string pluginName, int limit = 100);

        /// <summary>
        /// 获取所有插件日志
        /// </summary>
        /// <param name="limit">获取数量限制</param>
        /// <returns>日志列表</returns>
        Task<IEnumerable<PluginLogModel>> GetAllPluginLogs(int limit = 100);

        /// <summary>
        /// 清理指定天数之前的日志
        /// </summary>
        /// <param name="days">保留天数</param>
        /// <returns>删除的日志数量</returns>
        Task<int> CleanOldPluginLogs(int days = 30);

        #endregion

        #region 交易记录管理

        /// <summary>
        /// 获取最近的交易记录
        /// </summary>
        /// <param name="count">获取数量</param>
        /// <returns>交易记录列表</returns>
        Task<List<TransactionRecordDbo>> GetRecentTransactionsAsync(int count = 10);

        /// <summary>
        /// 根据日期范围获取交易记录
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>交易记录列表</returns>
        Task<List<TransactionRecordDbo>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取所有交易记录
        /// </summary>
        /// <returns>交易记录列表</returns>
        Task<List<TransactionRecordDbo>> GetAllTransactionsAsync();

        /// <summary>
        /// 根据ID获取交易记录
        /// </summary>
        /// <param name="id">交易ID</param>
        /// <returns>交易记录，不存在返回null</returns>
        Task<TransactionRecordDbo?> GetTransactionByIdAsync(int id);

        /// <summary>
        /// 根据订单号获取交易记录
        /// </summary>
        /// <param name="orderNumber">订单号</param>
        /// <returns>交易记录，不存在返回null</returns>
        Task<TransactionRecordDbo?> GetTransactionByOrderAsync(string orderNumber);

        /// <summary>
        /// 添加交易记录
        /// </summary>
        /// <param name="transaction">交易记录</param>
        /// <returns>新记录的ID</returns>
        Task<int> AddTransactionAsync(TransactionRecordDbo transaction);

        /// <summary>
        /// 更新交易记录
        /// </summary>
        /// <param name="transaction">交易记录</param>
        /// <returns>是否成功</returns>
        Task<bool> UpdateTransactionAsync(TransactionRecordDbo transaction);

        /// <summary>
        /// 删除交易记录
        /// </summary>
        /// <param name="id">交易ID</param>
        /// <returns>是否成功</returns>
        Task<bool> DeleteTransactionAsync(int id);

        /// <summary>
        /// 获取总金额
        /// </summary>
        /// <param name="startDate">开始日期（可选）</param>
        /// <param name="endDate">结束日期（可选）</param>
        /// <returns>总金额</returns>
        Task<decimal> GetTotalAmountAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 根据用户ID获取订单
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<List<TransactionRecordDbo>> GetTransactionsByUserIdAsync(string userId);

        /// <summary>
        /// 获取用户最新一条订单数据
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<TransactionRecordDbo?> GetOrderLastOrderByUserIdAsync(string userId);

        /// <summary>
        /// 获取指定用户在指定支付渠道下的最新一条订单数据。
        /// 用于判断“上次未支付”，避免微信与支付宝记录互相串扰。
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="paymentChannel">支付渠道</param>
        /// <returns>最新一条订单，不存在则返回 null</returns>
        Task<TransactionRecordDbo> GetOrderLastOrderByUserIdAndChannelAsync(
            string userId,
            PaymentChannel paymentChannel);


        #endregion

        #region 用户协议管理

        /// <summary>
        /// 检查用户是否已同意指定版本的条款
        /// </summary>
        /// <param name="version">版本号</param>
        /// <returns>是否已同意</returns>
        Task<bool> HasUserAgreedVersionAsync(string version);

        /// <summary>
        /// 保存用户同意记录
        /// </summary>
        /// <param name="agreement">同意记录</param>
        /// <returns>是否成功</returns>
        Task<bool> SaveUserAgreementAsync(UserAgreementDbo agreement);

        /// <summary>
        /// 获取用户协议记录
        /// </summary>
        /// <returns>协议记录，不存在返回null</returns>
        Task<UserAgreementDbo?> GetUserAgreementAsync();

        /// <summary>
        /// 获取用户最新同意的版本号
        /// </summary>
        /// <returns>版本号，不存在返回null</returns>
        Task<string?> GetLatestAgreedVersionAsync();

        #endregion

        #region 消息通知配置管理

        /// <summary>
        /// 获取消息通知配置
        /// </summary>
        /// <returns>配置信息，不存在返回null</returns>
        Task<NotificationConfigDbo?> GetNotificationConfigAsync();

        /// <summary>
        /// 保存消息通知配置
        /// </summary>
        /// <param name="config">配置信息</param>
        /// <returns>是否成功</returns>
        Task<bool> SaveNotificationConfigAsync(NotificationConfigDbo config);

        /// <summary>
        /// 检查用户是否已同意消息通知知情同意书
        /// </summary>
        /// <returns>是否已同意</returns>
        Task<bool> HasNotificationConsentAsync();

        /// <summary>
        /// 保存消息通知知情同意记录
        /// </summary>
        /// <param name="consent">同意记录</param>
        /// <returns>是否成功</returns>
        Task<bool> SaveNotificationConsentAsync(NotificationConsentDbo consent);

        #endregion

        
    }
}