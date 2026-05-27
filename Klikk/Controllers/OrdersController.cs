using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Klikk.Data;
using Klikk.Models;
using Stripe;
using Stripe.Checkout;
using Klikk.Services;
using Microsoft.AspNetCore.Identity;

namespace Klikk.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly EmailService _emailService;
        private readonly UserManager<IdentityUser> _userManager;

        public OrdersController(
            ApplicationDbContext context,
            EmailService emailService,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> CheckoutSelected(string selectedCartItems)
        {
            if (string.IsNullOrEmpty(selectedCartItems))
            {
                return RedirectToAction("Index", "Cart");
            }

            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            var selectedIds =
                selectedCartItems
                    .Split(',')
                    .Select(int.Parse)
                    .ToList();

            var cartItems =
                await _context.CartItems
                    .Include(c => c.Product)
                    .Where(c =>
                        c.UserId == userId &&
                        selectedIds.Contains(c.Id))
                    .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var domain = "https://klikk.onrender.com/";

            var options = new SessionCreateOptions
            {
                SuccessUrl =
                    domain + "Orders/PaymentSuccess",

                CancelUrl =
                    domain + "Orders/PaymentCancelled",

                Mode = "payment",

                LineItems =
                    new List<SessionLineItemOptions>()
            };

            foreach (var item in cartItems)
            {
                var sessionListItem =
                    new SessionLineItemOptions
                    {
                        PriceData =
                            new SessionLineItemPriceDataOptions
                            {
                                UnitAmount =
                                    (long)(item.Product.Price * 100),

                                Currency = "php",

                                ProductData =
                                    new SessionLineItemPriceDataProductDataOptions
                                    {
                                        Name = item.Product.Name
                                    }
                            },

                        Quantity = item.Quantity
                    };

                options.LineItems.Add(sessionListItem);
            }

            var service = new SessionService();

            Session session = service.Create(options);

            HttpContext.Session.SetString(
                "SelectedCartItems",
                selectedCartItems);

            Response.Headers.Add(
                "Location",
                session.Url);

            return new StatusCodeResult(303);
        }
        // CHECKOUT PAGE

        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var domain = "https://klikk.onrender.com/";

            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + "Orders/PaymentSuccess",
                CancelUrl = domain + "Orders/PaymentCancelled",
                Mode = "payment",
                LineItems = new List<SessionLineItemOptions>()
            };

            foreach (var item in cartItems)
            {
                var sessionListItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Product.Price * 100),

                        Currency = "php",

                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Name
                        }
                    },

                    Quantity = item.Quantity
                };

                options.LineItems.Add(sessionListItem);
            }

            var service = new SessionService();

            Session session = service.Create(options);

            Response.Headers.Add("Location", session.Url);

            return new StatusCodeResult(303);
        }

        public async Task<IActionResult> PaymentSuccess()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var recentOrder = await _context.Orders
                .Where(o => o.UserId == userId && o.Status == "Paid")
                .OrderByDescending(o => o.OrderDate)
                .FirstOrDefaultAsync();

            if (recentOrder != null &&
                recentOrder.OrderDate > DateTime.UtcNow.AddMinutes(-2))
            {
                return RedirectToAction("OrderSuccess");
            }

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                return RedirectToAction("OrderSuccess");
            }

            decimal total = 0;

            foreach (var item in cartItems)
            {
                total += item.Product.Price * item.Quantity;
            }

            Order order = new Order
            {
                UserId = userId,
                TotalAmount = total,
                Status = "Paid"
            };

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                OrderItem orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product.Price
                };

                _context.OrderItems.Add(orderItem);

                if (item.Product.StockQuantity < item.Quantity)
                {
                    throw new Exception("Insufficient stock for product: " + item.Product.Name);
                }

                item.Product.StockQuantity -= item.Quantity;
            }

            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            var user =
                await _userManager.GetUserAsync(User);

                    if (user != null)
                    {
                        string emailBody = $@"
                            <h2>Order Confirmation</h2>

                            <p>
                                Thank you for shopping with Klikk!
                            </p>

                            <p>
                                Your payment was successfully processed.
                            </p>

                            <h3>
                                Order Total: ₱{order.TotalAmount}
                            </h3>

                            <p>
                                Your order is now being prepared.
                            </p>
                        ";

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Klikk Order Confirmation",
                    emailBody);
            }

            return View();
        }

        public IActionResult PaymentCancelled()
        {
            return View();
        }

        // PLACE ORDER

        [HttpPost]
        public async Task<IActionResult> PlaceOrder()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            decimal total = 0;

            foreach (var item in cartItems)
            {
                total += item.Product.Price * item.Quantity;
            }

            Order order = new Order
            {
                UserId = userId,
                TotalAmount = total,
                Status = "Pending"
            };

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                OrderItem orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product.Price
                };

                _context.OrderItems.Add(orderItem);

                // DEDUCT STOCK

                if (item.Product.StockQuantity < item.Quantity)
                {
                    throw new Exception("Insufficient stock for product: " + item.Product.Name);
                }

                item.Product.StockQuantity -= item.Quantity;
            }

            // CLEAR CART

            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(OrderSuccess));
        }

        // SUCCESS PAGE

        public IActionResult OrderSuccess()
        {
            return View();
        }

        // ORDER HISTORY

        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ORDER DETAILS

        public async Task<IActionResult> Details(int id)
        {
            var orderItems = await _context.OrderItems
                .Include(o => o.Product)
                .Where(o => o.OrderId == id)
                .ToListAsync();

            return View(orderItems);
        }

        [Authorize]
        public async Task<IActionResult> BuyNow(int productId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            var domain = "https://klikk.onrender.com/";

            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"Orders/BuyNowSuccess?productId={product.Id}",
                CancelUrl = domain + "Orders/PaymentCancelled",
                Mode = "payment",
                LineItems = new List<SessionLineItemOptions>()
            };

            var sessionListItem = new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(product.Price * 100),
                    Currency = "php",

                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = product.Name
                    }
                },

                Quantity = 1
            };

            options.LineItems.Add(sessionListItem);

            var service = new SessionService();

            Session session = service.Create(options);

            Response.Headers.Add("Location", session.Url);

            return new StatusCodeResult(303);
        }

        [Authorize]
        public async Task<IActionResult> BuyNowSuccess(int productId)
        {
            try
            {
                var userId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                var product =
                    await _context.Products
                        .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                {
                    return RedirectToAction("Index", "Products");
                }

                if (product.StockQuantity < 1)
                {
                    TempData["Error"] = "Out of stock.";
                    return RedirectToAction("Details", "Products", new { id = productId });
                }

                // Prevent duplicate order on refresh
                var recentOrder =
                    await _context.Orders
                        .Where(o =>
                            o.UserId == userId &&
                            o.Status == "Paid")
                        .OrderByDescending(o => o.OrderDate)
                        .FirstOrDefaultAsync();

                if (recentOrder != null &&
                    recentOrder.OrderDate > DateTime.UtcNow.AddMinutes(-1))
                {
                    return RedirectToAction("OrderSuccess");
                }

                // Create order
                var order = new Order
                {
                    UserId = userId,
                    TotalAmount = product.Price,
                    Status = "Paid",
                    OrderDate = DateTime.UtcNow
                };

                _context.Orders.Add(order);

                await _context.SaveChangesAsync();

                // Create order item
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = 1,
                    Price = product.Price
                };

                _context.OrderItems.Add(orderItem);

                // Deduct stock
                product.StockQuantity -= 1;

                await _context.SaveChangesAsync();

                return RedirectToAction("OrderSuccess");
            }
            catch (Exception ex)
            {
                return Content(ex.InnerException?.Message ?? ex.Message);
            }
        }

    }


}