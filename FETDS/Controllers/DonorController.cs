using FETDS.Data;
using FETDS.Models;
using FETDS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FETDS.Controllers
{
    [Authorize(Roles = "Donor")]
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IProductExpiryService _expiryService;


        public DonorController(ApplicationDbContext context, IWebHostEnvironment env, IProductExpiryService expiryService)
        {
            _context = context;
            _env = env;
            _expiryService = expiryService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // GET: Donor
        public async Task<IActionResult> Index()
        {
            await _expiryService.ExpireOverdueProductsAsync(CurrentUserId);

            var myProducts = await _context.Products
                .Where(p => p.DonorId == CurrentUserId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(myProducts);
        }

        // GET: Donor/Create
        public IActionResult Create()
        {
            return View();
        }
        

        // POST: Donor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            product.DonorId = CurrentUserId;
            product.Status = "Available";
            product.CreatedAt = DateTime.Now;

            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Only .jpg, .jpeg, and .png images are allowed.");
                    return View(product);
                }

                var fileName = $"{Guid.NewGuid()}{extension}";
                var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "products", fileName);

                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                product.ImagePath = $"/uploads/products/{fileName}";
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Donor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.DonorId == CurrentUserId);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Donor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product updatedProduct)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.DonorId == CurrentUserId);

            if (product == null) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(updatedProduct);
            }

            product.Name = updatedProduct.Name;
            product.Description = updatedProduct.Description;
            product.Quantity = updatedProduct.Quantity;
            product.Unit = updatedProduct.Unit;
            product.ExpiryDate = updatedProduct.ExpiryDate;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Donor/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.DonorId == CurrentUserId);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Donor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.DonorId == CurrentUserId);

            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}