using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Klikk.Data;

namespace Klikk.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user =
                await _userManager.GetUserAsync(User);

            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            ViewBag.Email = user?.Email;

            ViewBag.TotalOrders =
                await _context.Orders
                    .Where(o => o.UserId == userId)
                    .CountAsync();

            ViewBag.TotalWishlist =
                await _context.WishlistItems
                    .Where(w => w.UserId == userId)
                    .CountAsync();

            ViewBag.TotalReviews =
                await _context.Reviews
                    .Where(r => r.UserId == userId)
                    .CountAsync();

            return View();
        }
    }
}