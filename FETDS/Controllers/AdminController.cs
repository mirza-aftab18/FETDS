using FETDS.Data;
using FETDS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FETDS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin  -> all pending requests
        public async Task<IActionResult> Index()
        {
            var pendingRequests = await _context.DonationRequests
                .Include(r => r.Product)
                .Include(r => r.Receiver)
                .Where(r => r.Status == "Pending")
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            return View(pendingRequests);
        }

        // POST: Admin/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.DonationRequests
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null || request.Product == null) return NotFound();

            // Approve this request
            request.Status = "Approved";
            request.Product.Status = "Reserved";

            // Auto-reject any other pending requests for the same product
            var otherRequests = await _context.DonationRequests
                .Where(r => r.ProductId == request.ProductId && r.Id != request.Id && r.Status == "Pending")
                .ToListAsync();

            foreach (var other in otherRequests)
            {
                other.Status = "Rejected";
                _context.Notifications.Add(new Notification
                {
                    UserId = other.ReceiverId,
                    Message = $"Your request for '{request.Product.Name}' was not approved — it was reserved for another receiver."
                });
            }

            // Notify donor and receiver of the approval
            _context.Notifications.Add(new Notification
            {
                UserId = request.Product.DonorId,
                Message = $"Your donation of '{request.Product.Name}' was approved for a receiver. Please arrange pickup."
            });
            _context.Notifications.Add(new Notification
            {
                UserId = request.ReceiverId,
                Message = $"Your request for '{request.Product.Name}' was approved! Please contact the donor to arrange pickup."
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var request = await _context.DonationRequests
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null || request.Product == null) return NotFound();

            request.Status = "Rejected";

            _context.Notifications.Add(new Notification
            {
                UserId = request.ReceiverId,
                Message = $"Your request for '{request.Product.Name}' was rejected by the admin."
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Dashboard()
        {
            var donors = await _userManager.GetUsersInRoleAsync("Donor");
            var receivers = await _userManager.GetUsersInRoleAsync("Receiver");

            var stats = new AdminDashboardViewModel
            {
                TotalDonors = donors.Count,
                TotalReceivers = receivers.Count,
                TotalProductsListed = await _context.Products.CountAsync(),
                TotalDonationsCompleted = await _context.Products.CountAsync(p => p.Status == "Reserved"),
                TotalPendingRequests = await _context.DonationRequests.CountAsync(r => r.Status == "Pending"),
                TotalExpiredProducts = await _context.Products.CountAsync(p => p.Status == "Expired"),
                ProductsByCategory = (await _context.Products
                    .GroupBy(p => p.Category)
                    .Select(g => new { Category = g.Key.ToString(), Count = g.Count() })
                    .ToListAsync())
                    .Select(g => (g.Category, g.Count))
                    .ToList()
            };

            return View(stats);
        }
    }
}