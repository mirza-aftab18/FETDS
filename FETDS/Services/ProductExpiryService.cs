using FETDS.Data;
using Microsoft.EntityFrameworkCore;

namespace FETDS.Services
{
    public class ProductExpiryService : IProductExpiryService
    {
        private readonly ApplicationDbContext _context;

        public ProductExpiryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ExpireOverdueProductsAsync()
        {
            var expired = await _context.Products
                .Where(p => p.Status == "Available" && p.ExpiryDate < DateTime.Now.Date)
                .ToListAsync();

            if (expired.Any())
            {
                foreach (var p in expired)
                {
                    p.Status = "Expired";
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task ExpireOverdueProductsAsync(string donorId)
        {
            var expired = await _context.Products
                .Where(p => p.DonorId == donorId && p.Status == "Available" && p.ExpiryDate < DateTime.Now.Date)
                .ToListAsync();

            if (expired.Any())
            {
                foreach (var p in expired)
                {
                    p.Status = "Expired";
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}