using Klikk.Data;
using Klikk.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Klikk.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminOrdersController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ========================================
        // ADMIN ORDERS LIST
        // ========================================
        public async Task<IActionResult> Index(string searchString)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.OrderItems)
                .AsQueryable();

            // SEARCH (Order ID or UserId)
            if (!string.IsNullOrEmpty(searchString))
            {
                ordersQuery = ordersQuery.Where(o =>
                    o.Id.ToString().Contains(searchString) ||
                    o.UserId.Contains(searchString)
                );
            }

            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ========================================
        // ADMIN ORDER DETAILS
        // ========================================
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(order.UserId);
            ViewBag.UserEmail = user?.Email;

            return View(order);
        }
    }
}