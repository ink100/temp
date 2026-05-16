using HRB.Payment.Core.Models;
using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Repository
{

    public class PaymentRepository : BaseWpfLiteDbRepository<PaymentDbContext>, IPaymentRepository
    {
        private const string ALIPAY_COLLECTION_NAME = "AlipayConfigs";
        //private readonly PaymentDbContext _CurrentDbContext;

        private readonly IHrbLogger _log;

        public PaymentRepository(PaymentDbContext dbContext) : base(dbContext)
        {
            //  _CurrentDbContext = dbContext;

            _log = GlobalSettings.CurrentAppContext.CurrentLogger;
        }



        /// <summary>
        /// 保存成绩
        /// </summary>
        /// <param name="dbo"></param>
        /// <returns></returns>
        public async Task<bool> SaveTestAsync(TestDemoDbo dbo)
        {
            var aaa = await _CurrentDbContext.GetCollection<TestDemoDbo>().InsertAsync(dbo);

            return true;
        }

        /// <summary>
        /// 获取成绩列表
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<TestDemoDbo>> GetAllTestAsync()
        {
            var list = await _CurrentDbContext.GetCollection<TestDemoDbo>()
                .FindAllAsync();

            return list;
        }



        #region 支付宝配置管理

        /// <summary>
        /// 保存或更新支付宝配置
        /// </summary>
        public async Task<bool> SaveAlipayConfig(AlipayAppInfoModel config)
        {
            try
            {
                if (config == null || string.IsNullOrWhiteSpace(config.LoginAccount))
                {
                    return false;
                }

                var collection = _CurrentDbContext.GetCollection<AlipayAppInfoModel>(ALIPAY_COLLECTION_NAME);

                // 账号是业务唯一键，不能只按 Id 判断，否则 UI 传入新对象时会插入重复配置。
                var existing = await collection.FindOneAsync(x => x.LoginAccount == config.LoginAccount);

                if (existing != null)
                {
                    // 更新：保留原ID和创建时间
                    config.Id = existing.Id;
                    config.CreateDateTime = existing.CreateDateTime;
                    config.LastUpdateDateTime = DateTime.Now;
                    return await collection.UpdateAsync(config);
                }
                else
                {
                    // 新增
                    config.CreateDateTime = DateTime.Now;
                    config.LastUpdateDateTime = DateTime.Now;
                    var result = await collection.InsertAsync(config);
                    return result != null;
                }
            }
            catch (Exception ex)
            {
                _log.Info($"保存支付宝配置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 根据登录账号获取配置
        /// </summary>
        public async Task<AlipayAppInfoModel> GetAlipayConfigByAccount(string loginAccount)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loginAccount))
                {
                    return null;
                }

                var collection = _CurrentDbContext.GetCollection<AlipayAppInfoModel>(ALIPAY_COLLECTION_NAME);
                return await collection.FindOneAsync(x => x.LoginAccount == loginAccount);
            }
            catch (Exception ex)
            {
                _log.Info($"获取支付宝配置失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取所有支付宝配置
        /// </summary>
        public async Task<IEnumerable<AlipayAppInfoModel>> GetAllAlipayConfigs()
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<AlipayAppInfoModel>(ALIPAY_COLLECTION_NAME);
                return await collection.FindAllAsync();
            }
            catch (Exception ex)
            {
                _log.Info($"获取所有支付宝配置失败: {ex.Message}");
                return new List<AlipayAppInfoModel>();
            }
        }

        /// <summary>
        /// 删除支付宝配置
        /// </summary>
        public async Task<bool> DeleteAlipayConfig(string loginAccount)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loginAccount))
                {
                    return false;
                }

                var collection = _CurrentDbContext.GetCollection<AlipayAppInfoModel>(ALIPAY_COLLECTION_NAME);
                var existing = await collection.FindOneAsync(x => x.LoginAccount == loginAccount);

                if (existing != null)
                {
                    return await collection.DeleteAsync(existing.Id);
                }

                return false;
            }
            catch (Exception ex)
            {
                _log.Info($"删除支付宝配置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        public async Task<bool> AlipayConfigExists(string loginAccount)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loginAccount))
                {
                    return false;
                }

                var collection = _CurrentDbContext.GetCollection<AlipayAppInfoModel>(ALIPAY_COLLECTION_NAME);
                return await collection.ExistsAsync(x => x.LoginAccount == loginAccount);
            }
            catch (Exception ex)
            {
                _log.Info($"检查支付宝配置是否存在失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 插件日志管理

        private const string PLUGIN_LOG_COLLECTION_NAME = "PluginLogs";

        /// <summary>
        /// 添加插件日志
        /// </summary>
        public async Task<bool> AddPluginLog(PluginLogModel log)
        {
            try
            {
                if (log == null)
                {
                    return false;
                }

                var collection = _CurrentDbContext.GetCollection<PluginLogModel>(PLUGIN_LOG_COLLECTION_NAME);
                log.CreateDateTime = DateTime.Now;
                var result = await collection.InsertAsync(log);
                return result != null;
            }
            catch (Exception ex)
            {
                _log.Info($"添加插件日志失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取指定插件的日志
        /// </summary>
        public async Task<IEnumerable<PluginLogModel>> GetPluginLogs(string pluginName, int limit = 100)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pluginName))
                {
                    return new List<PluginLogModel>();
                }

                var collection = _CurrentDbContext.GetCollection<PluginLogModel>(PLUGIN_LOG_COLLECTION_NAME);
                var logs = await collection.FindAsync(x => x.PluginName == pluginName);
                return logs.OrderByDescending(x => x.CreateDateTime).Take(limit);
            }
            catch (Exception ex)
            {
                _log.Info($"获取插件日志失败: {ex.Message}");
                return new List<PluginLogModel>();
            }
        }

        /// <summary>
        /// 获取所有插件日志
        /// </summary>
        public async Task<IEnumerable<PluginLogModel>> GetAllPluginLogs(int limit = 100)
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<PluginLogModel>(PLUGIN_LOG_COLLECTION_NAME);
                var logs = await collection.FindAllAsync();
                return logs.OrderByDescending(x => x.CreateDateTime).Take(limit);
            }
            catch (Exception ex)
            {
                _log.Info($"获取所有插件日志失败: {ex.Message}");
                return new List<PluginLogModel>();
            }
        }

        /// <summary>
        /// 清理指定天数之前的日志
        /// </summary>
        public async Task<int> CleanOldPluginLogs(int days = 30)
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<PluginLogModel>(PLUGIN_LOG_COLLECTION_NAME);
                var cutoffDate = DateTime.Now.AddDays(-days);
                var oldLogs = await collection.FindAsync(x => x.CreateDateTime < cutoffDate);

                int count = 0;
                foreach (var log in oldLogs)
                {
                    if (await collection.DeleteAsync(log.Id))
                    {
                        count++;
                    }
                }

                return count;
            }
            catch (Exception ex)
            {
                _log.Info($"清理旧插件日志失败: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region 交易记录管理

        //  private const string TRANSACTION_COLLECTION_NAME = "Transactions";

        /// <summary>
        /// 获取最近的交易记录
        /// </summary>
        public async Task<List<TransactionRecordDbo>> GetRecentTransactionsAsync(int count = 10)
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<TransactionRecordDbo>();
                var transactions = await collection.FindAllAsync();
                return transactions.OrderByDescending(t => t.TransactionTime).Take(count).ToList();
            }
            catch (Exception ex)
            {
                _log.Info($"获取最近交易记录失败: {ex.Message}");
                return new List<TransactionRecordDbo>();
            }
        }

        /// <summary>
        /// 根据日期范围获取交易记录
        /// </summary>
        public async Task<List<TransactionRecordDbo>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<TransactionRecordDbo>();
                var transactions = await collection.FindAsync(t => t.TransactionTime >= startDate && t.TransactionTime < endDate);
                return transactions.OrderByDescending(t => t.TransactionTime).ToList();
            }
            catch (Exception ex)
            {
                _log.Info($"根据日期范围获取交易记录失败: {ex.Message}");
                return new List<TransactionRecordDbo>();
            }
        }

        /// <summary>
        /// 获取所有交易记录
        /// </summary>
        public async Task<List<TransactionRecordDbo>> GetAllTransactionsAsync()
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<TransactionRecordDbo>();
                var transactions = await collection.FindAllAsync();
                return transactions.OrderByDescending(t => t.TransactionTime).ToList();
            }
            catch (Exception ex)
            {
                _log.Info($"获取所有交易记录失败: {ex.Message}");
                return new List<TransactionRecordDbo>();
            }
        }

        /// <summary>
        /// 根据ID获取交易记录
        /// </summary>
        public async Task<TransactionRecordDbo?> GetTransactionByIdAsync(int id)
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<TransactionRecordDbo>();
                return await collection.FindByIdAsync(id);
            }
            catch (Exception ex)
            {
                _log.Info($"根据ID获取交易记录失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 根据订单号获取交易记录
        /// </summary>
        public async Task<TransactionRecordDbo?> GetTransactionByOrderAsync(string orderNumber)
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<TransactionRecordDbo>();
                var transactions = await collection.FindAsync(t => t.OrderNumber == orderNumber);
                return transactions.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _log.Info($"根据订单号获取交易记录失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 添加交易记录
        /// </summary>
        public async Task<int> AddTransactionAsync(TransactionRecordDbo transaction)
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<TransactionRecordDbo>();
                var id = await collection.InsertAsync(transaction);
                return id;
            }
            catch (Exception ex)
            {
                _log.Info($"添加交易记录失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 更新交易记录
        /// </summary>
        public async Task<bool> UpdateTransactionAsync(TransactionRecordDbo transaction)
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<TransactionRecordDbo>();
                return await collection.UpdateAsync(transaction);
            }
            catch (Exception ex)
            {
                _log.Info($"更新交易记录失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除交易记录
        /// </summary>
        public async Task<bool> DeleteTransactionAsync(int id)
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<TransactionRecordDbo>();
                return await collection.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _log.Info($"删除交易记录失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取总金额
        /// </summary>
        public async Task<decimal> GetTotalAmountAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<TransactionRecordDbo>();
                IEnumerable<TransactionRecordDbo> transactions;

                if (startDate.HasValue && endDate.HasValue)
                {
                    transactions = await collection.FindAsync(t => t.TransactionTime >= startDate.Value && t.TransactionTime < endDate.Value);
                }
                else
                {
                    transactions = await collection.FindAllAsync();
                }

                return transactions.Sum(t => t.Amount);
            }
            catch (Exception ex)
            {
                _log.Info($"获取总金额失败: {ex.Message}");
                return 0;
            }
        }


        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<List<TransactionRecordDbo>> GetTransactionsByUserIdAsync(string userId)
        {
            var collection = _CurrentDbContext.GetCollection<TransactionRecordDbo>();
            var list = await collection.FindAsync(c => c.UserId == userId);
            return list.ToList();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<TransactionRecordDbo?> GetOrderLastOrderByUserIdAsync(string userId)
        {
            var collection = _CurrentDbContext.GetCollection<TransactionRecordDbo>();
            var list = await collection.FindAsync(c => c.UserId == userId);
            return list.OrderByDescending(c => c.TransactionTime).FirstOrDefault();
        }
        public async Task<TransactionRecordDbo?> GetOrderLastOrderByUserIdAsync(string userId,string paymentChannel)
        {
            var collection = _CurrentDbContext.GetCollection<TransactionRecordDbo>();
            var list = await collection.FindAsync(c => c.UserId == userId);
            return list.OrderByDescending(c => c.TransactionTime).FirstOrDefault();
        }
        /// <summary>
        /// 获取指定用户在指定渠道下的最新一条订单。
        /// “上次未支付”必须区分支付渠道，
        /// 否则可能出现微信记录影响支付宝、支付宝记录影响微信的问题。
        /// </summary>
        public async Task<TransactionRecordDbo> GetOrderLastOrderByUserIdAndChannelAsync(
            string userId,
            PaymentChannel paymentChannel)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var collection = _CurrentDbContext.GetCollection<TransactionRecordDbo>();

            var list = await collection.FindAsync(c =>
                c.UserId == userId &&
                c.PaymentChannel == paymentChannel);

            return list
                .OrderByDescending(c =>
                    c.TransactionTime == default
                        ? c.CreatedAt
                        : c.TransactionTime)
                .FirstOrDefault();
        }
        #endregion

        #region 用户协议管理

        private const string USER_AGREEMENT_COLLECTION_NAME = "UserAgreements";

        /// <summary>
        /// 检查用户是否已同意指定版本的条款
        /// </summary>
        public async Task<bool> HasUserAgreedVersionAsync(string version)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(version))
                {
                    return false;
                }

                var collection = _CurrentDbContext.GetCollection<UserAgreementDbo>(USER_AGREEMENT_COLLECTION_NAME);
                var agreement = await collection.FindOneAsync(x => x.IsAgreed == true && x.Version == version);
                return agreement != null;
            }
            catch (Exception ex)
            {
                _log.Info($"检查用户是否已同意指定版本条款失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 保存用户同意记录
        /// </summary>
        public async Task<bool> SaveUserAgreementAsync(UserAgreementDbo agreement)
        {
            try
            {
                if (agreement == null)
                {
                    return false;
                }

                var collection = _CurrentDbContext.GetCollection<UserAgreementDbo>(USER_AGREEMENT_COLLECTION_NAME);
                var result = await collection.InsertAsync(agreement);
                return result != null;
            }
            catch (Exception ex)
            {
                _log.Info($"保存用户同意记录失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取用户协议记录
        /// </summary>
        public async Task<UserAgreementDbo?> GetUserAgreementAsync()
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<UserAgreementDbo>(USER_AGREEMENT_COLLECTION_NAME);
                var agreements = await collection.FindAllAsync();
                return agreements.OrderByDescending(x => x.AgreementTime).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _log.Info($"获取用户协议记录失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取用户最新同意的版本号
        /// </summary>
        public async Task<string?> GetLatestAgreedVersionAsync()
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<UserAgreementDbo>(USER_AGREEMENT_COLLECTION_NAME);
                var agreements = await collection.FindAsync(x => x.IsAgreed == true);
                var latest = agreements.OrderByDescending(x => x.AgreementTime).FirstOrDefault();
                return latest?.Version;
            }
            catch (Exception ex)
            {
                _log.Info($"获取用户最新同意的版本号失败: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region 消息通知配置管理

        private const string NOTIFICATION_CONFIG_COLLECTION_NAME = "NotificationConfigs";
        private const string NOTIFICATION_CONSENT_COLLECTION_NAME = "NotificationConsents";

        /// <summary>
        /// 获取消息通知配置
        /// </summary>
        public async Task<NotificationConfigDbo?> GetNotificationConfigAsync()
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<NotificationConfigDbo>(NOTIFICATION_CONFIG_COLLECTION_NAME);
                var configs = await collection.FindAllAsync();
                return configs.OrderByDescending(x => x.LastUpdatedAt).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _log.Info($"获取消息通知配置失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存消息通知配置
        /// </summary>
        public async Task<bool> SaveNotificationConfigAsync(NotificationConfigDbo config)
        {
            try
            {
                if (config == null)
                {
                    return false;
                }

                var collection = _CurrentDbContext.GetCollection<NotificationConfigDbo>(NOTIFICATION_CONFIG_COLLECTION_NAME);

                // 通知配置在业务上是单例，优先更新最近一条，避免重复插入多份配置。
                var existing = (await collection.FindAllAsync())
                    .OrderByDescending(x => x.LastUpdatedAt)
                    .FirstOrDefault();

                if (existing != null)
                {
                    // 更新现有配置
                    config.Id = existing.Id;
                    config.CreatedAt = existing.CreatedAt;
                    config.LastUpdatedAt = DateTime.Now;
                    return await collection.UpdateAsync(config);
                }
                else
                {
                    // 新增配置
                    config.CreatedAt = DateTime.Now;
                    config.LastUpdatedAt = DateTime.Now;
                    var result = await collection.InsertAsync(config);
                    return result != null;
                }
            }
            catch (Exception ex)
            {
                _log.Info($"保存消息通知配置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查用户是否已同意消息通知知情同意书
        /// </summary>
        public async Task<bool> HasNotificationConsentAsync()
        {
            try
            {
                var collection = _CurrentDbContext.GetCollection<NotificationConsentDbo>(NOTIFICATION_CONSENT_COLLECTION_NAME);
                var consent = await collection.FindOneAsync(x => x.IsAgreed == true);
                return consent != null;
            }
            catch (Exception ex)
            {
                _log.Info($"检查消息通知知情同意失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 保存消息通知知情同意记录
        /// </summary>
        public async Task<bool> SaveNotificationConsentAsync(NotificationConsentDbo consent)
        {
            try
            {
                if (consent == null)
                {
                    return false;
                }

                var collection = _CurrentDbContext.GetCollection<NotificationConsentDbo>(NOTIFICATION_CONSENT_COLLECTION_NAME);
                var result = await collection.InsertAsync(consent);
                return result != null;
            }
            catch (Exception ex)
            {
                _log.Info($"保存消息通知知情同意记录失败: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
