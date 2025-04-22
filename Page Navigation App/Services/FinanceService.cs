using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    public class FinanceService
    {
        private readonly AppDbContext _dbContext;

        public FinanceService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<Finance> GetAllTransactions()
        {
            return _dbContext.Finances.Include(f => f.Order).ToList();
        }

        public void AddTransaction(Finance transaction)
        {
            _dbContext.Finances.Add(transaction);
            _dbContext.SaveChanges();
        }

        public void UpdateTransaction(Finance transaction)
        {
            _dbContext.Finances.Update(transaction);
            _dbContext.SaveChanges();
        }

        public void DeleteTransaction(Finance transaction)
        {
            _dbContext.Finances.Remove(transaction);
            _dbContext.SaveChanges();
        }

        public List<Finance> FilterTransactionsByType(string transactionType)
        {
            return _dbContext.Finances
                .Where(f => f.TransactionType == transactionType)
                .ToList();
        }

        public List<Finance> FilterTransactionsByDate(DateTime startDate, DateTime endDate)
        {
            return _dbContext.Finances
                .Where(f => f.TransactionDate >= startDate && f.TransactionDate <= endDate)
                .ToList();
        }
    }
}