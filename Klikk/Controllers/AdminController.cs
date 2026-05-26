using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Klikk.Data;

namespace Klikk.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalProducts =
                await _context.Products.CountAsync();

            ViewBag.TotalOrders =
                await _context.Orders.CountAsync();

            ViewBag.TotalUsers =
                await _userManager.Users.CountAsync();

            ViewBag.TotalSales =
                await _context.Orders
                    .Where(o => o.Status == "Paid")
                    .SumAsync(o => (decimal?)o.TotalAmount)
                    ?? 0;

            ViewBag.LowStockProducts =
                await _context.Products
                    .Where(p => p.StockQuantity < 5)
                    .CountAsync();

            return View();
        }

        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }
    }
}