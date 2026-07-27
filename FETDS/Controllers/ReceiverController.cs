using FETDS.Data;
using FETDS.Models;
using FETDS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FETDS.Controllers
{
    [Authorize(Roles = "Receiver")]
    public class ReceiverController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductExpiryService _expiryService;
        private readonly UserManager<ApplicationUser> _userManager;


        public ReceiverController(ApplicationDbContext context, IProductExpiryService expiryService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _expiryService = expiryService;
            _userManager = userManager;

        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // GET: Receiver  -> browse available products


        public async Task<IActionResult> Index(ProductCategory? category)
        {
            await _expiryService.ExpireOverdueProductsAsync();

            var query = _context.Products
                .Include(p => p.Donor)
                .Where(p => p.Status == "Available");

            if (category.HasValue)
            {
                query = query.Where(p => p.Category == category.Value);
            }

            var availableProducts = await query
                .OrderBy(p => p.ExpiryDate)
                .ToListAsync();

            // Product Ids this receiver already has a pending request for
            var appliedProductIds = await _context.DonationRequests
                .Where(r => r.ReceiverId == CurrentUserId && r.Status == "Pending")
                .Select(r => r.ProductId)
                .ToListAsync();

            ViewBag.SelectedCategory = category;
            ViewBag.AppliedProductIds = appliedProductIds;
            return View(availableProducts);
        }

        // GET: Receiver/Apply/5
        public async Task<IActionResult> Apply(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Donor)
                .FirstOrDefaultAsync(p => p.Id == id && p.Status == "Available");

            if (product == null) return NotFound();

            var request = new DonationRequest { ProductId = product.Id, Product = product };
            return View(request);
        }

        // POST: Receiver/Apply/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int id, DonationRequest request)
        {
            var product = await _context.Products
                .Include(p => p.Donor)
                .FirstOrDefaultAsync(p => p.Id == id && p.Status == "Available");

            if (product == null) return NotFound();

            var alreadyApplied = await _context.DonationRequests
                .AnyAsync(r => r.ProductId == id && r.ReceiverId == CurrentUserId && r.Status == "Pending");

            if (alreadyApplied)
            {
                ModelState.AddModelError("", "You've already applied for this product — your request is awaiting admin review.");
                var vm = new DonationRequest { ProductId = id, Product = product };
                return View(vm);
            }

            var donationRequest = new DonationRequest
            {
                ProductId = product.Id,
                ReceiverId = CurrentUserId,
                Note = request.Note,
                Status = "Pending",
                RequestedAt = DateTime.Now
            };

            _context.DonationRequests.Add(donationRequest);

            // Notify every Admin that a new request needs review
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Message = $"New donation request for '{product.Name}' needs review."
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyRequests));
        }

        // GET: Receiver/MyRequests
        public async Task<IActionResult> MyRequests()
        {
            var myRequests = await _context.DonationRequests
                .Include(r => r.Product)
                .Where(r => r.ReceiverId == CurrentUserId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return View(myRequests);
        }
    }
}