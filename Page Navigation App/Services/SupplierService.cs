using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Page_Navigation_App.Data;
using Page_Navigation_App.Model;

namespace Page_Navigation_App.Services
{
    public class VendorService
    {
        private readonly AppDbContext _context;

        public VendorService(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Vendor> GetAllVendors()
        {
            return _context.Vendors.ToList();
        }

        public void AddVendor(Vendor vendor)
        {
            _context.Vendors.Add(vendor);
            _context.SaveChanges();
        }

        public void UpdateVendor(Vendor vendor)
        {
            _context.Vendors.Update(vendor);
            _context.SaveChanges();
        }

        public void DeleteVendor(int vendorId)
        {
            var vendor = _context.Vendors.Find(vendorId);
            if (vendor != null)
            {
                _context.Vendors.Remove(vendor);
                _context.SaveChanges();
            }
        }

        public IEnumerable<Vendor> SearchVendors(string searchTerm)
        {
            return _context.Vendors
                .Where(v => v.VendorName.Contains(searchTerm) || v.PhoneNumber.Contains(searchTerm))
                .ToList();
        }
    }
}