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
        public async Task<IActionResult> Create(
            int productId,
            int rating,
            string comment)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            Review review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = comment
            };

            _context.Reviews.Add(review);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                "Products",
                new { id = productId });
        }
    }
}