using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Klikk.Data;
using Klikk.Models;

namespace Klikk.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // VIEW WISHLIST

        public async Task<IActionResult> Index()
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            var wishlistItems =
                await _context.WishlistItems
                    .Include(w => w.Product)
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.AddedDate)
                    .ToListAsync();

            return View(wishlistItems);
        }

        // ADD TO WISHLIST

        public async Task<IActionResult> Add(int productId)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            bool alreadyExists =
                await _context.WishlistItems
                    .AnyAsync(w =>
                        w.ProductId == productId &&
                        w.UserId == userId);

            if (!alreadyExists)
            {
                WishlistItem item = new WishlistItem
                {
                    ProductId = productId,
                    UserId = userId
                };

                _context.WishlistItems.Add(item);

                await _context.SaveChangesAsync();
            }

            TempData["Success"] =
                "Product added to wishlist!";

            return RedirectToAction(
                "Details",
                "Products",
                new { id = productId });
        }

        // REMOVE FROM WISHLIST

        public async Task<IActionResult> Remove(int id)
        {
            var item =
                await _context.WishlistItems
                    .FindAsync(id);

            if (item != null)
            {
                _context.WishlistItems.Remove(item);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}