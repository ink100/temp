//using HRB.Payment.Core.Models;

//namespace HRB.Payment.Core.Services
//{
//    public interface ITransactionService
//    {
//        Task<List<TransactionRecord>> GetRecentTransactionsAsync(int count = 10);
//        Task<List<TransactionRecord>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate);
//        Task<List<TransactionRecord>> GetAllTransactionsAsync();
//        Task<TransactionRecord?> GetTransactionByIdAsync(int id);
//        Task<TransactionRecord?> GetTransactionByOrderAsync(string order);
//        Task<int> AddTransactionAsync(TransactionRecord transaction);
//        Task<bool> UpdateTransactionAsync(TransactionRecord transaction);
//        Task<bool> DeleteTransactionAsync(int id);
//        Task<decimal> GetTotalAmountAsync(DateTime? startDate = null, DateTime? endDate = null);
//    }
//}

