using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Klikk.Data;
using Klikk.Models;

namespace Klikk.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // VIEW CART

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(cartItems);
        }

        // ADD TO CART

        public async Task<IActionResult> AddToCart(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(c =>
                    c.ProductId == productId &&
                    c.UserId == userId);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity++;
            }
            else
            {
                CartItem cartItem = new CartItem
                {
                    ProductId = productId,
                    Quantity = 1,
                    UserId = userId
                };

                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product added to cart successfully!";

            return Ok(new { success = true });
        }

        // REMOVE ITEM

        public async Task<IActionResult> Remove(int id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // INCREASE QUANTITY

        public async Task<IActionResult> IncreaseQuantity(int id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);

            if (cartItem != null)
            {
                cartItem.Quantity++;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // DECREASE QUANTITY

        public async Task<IActionResult> DecreaseQuantity(int id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);

            if (cartItem != null)
            {
                if (cartItem.Quantity > 1)
                {
                    cartItem.Quantity--;
                }
                else
                {
                    _context.CartItems.Remove(cartItem);
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}