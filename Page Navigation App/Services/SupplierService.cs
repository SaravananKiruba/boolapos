using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace Page_Navigation_App.Services
{
    public class SupplierService
    {
        private readonly AppDbContext _context;
        
        public SupplierService(
            AppDbContext context
            )
        {
            _context = context;
           
        }        

        // Create supplier
        public async Task<Supplier> AddSupplier(Supplier supplier)
        {
            try
            {
                supplier.IsActive = true;
                supplier.RegistrationDate = DateTime.Now;
                
                await _context.Suppliers.AddAsync(supplier);
                await _context.SaveChangesAsync();
                return supplier;
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                Console.WriteLine($"Error adding supplier: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        // Read suppliers
        public async Task<Supplier> GetSupplierById(int supplierId)
        {
            return await _context.Suppliers.FindAsync(supplierId);
        }

        public async Task<IEnumerable<Supplier>> GetAllSuppliers(bool includeInactive = false)
        {
            var query = _context.Suppliers.AsQueryable();
            
            if (!includeInactive)
                query = query.Where(s => s.IsActive);
                
            return await query.OrderBy(s => s.SupplierName).ToListAsync();
        }

        public async Task<IEnumerable<Supplier>> SearchSuppliers(string searchTerm)
        {
            return await _context.Suppliers
                .Where(s => s.IsActive && 
                          (s.SupplierName.Contains(searchTerm) || 
                           s.ContactPerson.Contains(searchTerm) || 
                           s.PhoneNumber.Contains(searchTerm) || 
                           s.Email.Contains(searchTerm) || 
                           s.GSTNumber.Contains(searchTerm)))
                .OrderBy(s => s.SupplierName)
                .ToListAsync();
        }        

        // Update supplier
        public async Task<bool> UpdateSupplier(Supplier supplier)
        {
            try
            {
                var existingSupplier = await _context.Suppliers.FindAsync(supplier.SupplierID);
                if (existingSupplier == null)
                {
                    Debug.WriteLine($"Supplier with ID {supplier.SupplierID} not found.");
                    return false;
                }

                _context.Entry(existingSupplier).CurrentValues.SetValues(supplier);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating supplier: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        // Delete supplier (soft delete)
        public async Task<bool> DeactivateSupplier(int supplierId)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(supplierId);
                if (supplier == null) return false;

                supplier.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Purchase operations



        // Get the first supplier from the database
        public async Task<Supplier> GetFirstSupplier()
        {
            return await _context.Suppliers
                .Where(s => s.IsActive)
                .OrderBy(s => s.SupplierID)
                .FirstOrDefaultAsync();
        }
    }
}