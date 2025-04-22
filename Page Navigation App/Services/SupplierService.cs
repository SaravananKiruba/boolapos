using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    public class SupplierService
    {
        private readonly AppDbContext _dbContext;

        public SupplierService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<Supplier> GetAllSuppliers()
        {
            return _dbContext.Suppliers.Include(s => s.Products).ToList();
        }

        public void AddSupplier(Supplier supplier)
        {
            _dbContext.Suppliers.Add(supplier);
            _dbContext.SaveChanges();
        }

        public void UpdateSupplier(Supplier supplier)
        {
            _dbContext.Suppliers.Update(supplier);
            _dbContext.SaveChanges();
        }

        public void DeleteSupplier(Supplier supplier)
        {
            _dbContext.Suppliers.Remove(supplier);
            _dbContext.SaveChanges();
        }

        public List<Supplier> FilterSuppliers(string name)
        {
            return _dbContext.Suppliers
                .Where(s => EF.Functions.Like(s.SupplierName, $"%{name}%"))
                .ToList();
        }
    }
}