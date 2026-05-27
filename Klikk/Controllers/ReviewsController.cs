using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Klikk.Data;
using Klikk.Models;

namespace Klikk.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    int productId,
    int rating,
    string Comment)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var productExists =
                await _context.Products.AnyAsync(p => p.Id == productId);

            if (!productExists)
            {
                return NotFound();
            }

            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Invalid rating.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new { id = productId });
            }

            Review review = new Review
            {
                ProductId = productId,
                UserId = userId ?? "",
                Rating = rating,
                Comment = Comment
            };

            try
            {
                _context.Reviews.Add(review);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Content(ex.ToString());
            }

            TempData["Success"] = "Review submitted successfully.";

            return RedirectToAction(
                "Details",
                "Products",
                new { id = productId });
        }
    }
}