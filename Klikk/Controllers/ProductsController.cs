using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Klikk.Data;
using Klikk.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;

namespace Klikk.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Products
        public async Task<IActionResult> Index(
    string searchString,
    int? categoryId,
    string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategory"] = categoryId;
            ViewData["CurrentSort"] = sortOrder;

            var categories = await _context.Categories.ToListAsync();

            ViewBag.Categories = categories;

            var products = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            // SEARCH

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p =>
                    p.Name.Contains(searchString) ||
                    p.Description.Contains(searchString));
            }

            // CATEGORY FILTER

            if (categoryId.HasValue)
            {
                products = products.Where(p =>
                    p.CategoryId == categoryId.Value);
            }

            // SORTING

            switch (sortOrder)
            {
                case "price_asc":
                    products = products.OrderBy(p => p.Price);
                    break;

                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;

                case "name_asc":
                    products = products.OrderBy(p => p.Name);
                    break;

                case "name_desc":
                    products = products.OrderByDescending(p => p.Name);
                    break;

                default:
                    products = products.OrderByDescending(p => p.Id);
                    break;
            }

            return View(await products.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // ========================================
            // SUGGESTED PRODUCTS
            // ========================================

            var suggestedProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p =>
                    p.CategoryId == product.CategoryId &&
                    p.Id != product.Id)
                .OrderByDescending(p => p.Id)
                .Take(4)
                .ToListAsync();

            ViewBag.SuggestedProducts = suggestedProducts;

            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex(string searchString)
        {
            var products = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p =>
                    p.Name.Contains(searchString));
            }

            return View(await products.ToListAsync());
        }

        // GET: Products/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] =
                new SelectList(_context.Categories, "Id", "Name");

            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                string? uniqueFileName = null;

                if (model.ImageFile != null)
                {
                    string uploadsFolder =
                        Path.Combine(
                            _webHostEnvironment.WebRootPath,
                            "uploads");

                    uniqueFileName =
                        Guid.NewGuid().ToString() + "_" +
                        model.ImageFile.FileName;

                    string filePath =
                        Path.Combine(
                            uploadsFolder,
                            uniqueFileName);

                    using (var fileStream =
                           new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                }

                Product product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    StockQuantity = model.StockQuantity,
                    CategoryId = model.CategoryId,
                    ImageUrl = uniqueFileName
                };

                _context.Add(product);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] =
                new SelectList(_context.Categories,
                    "Id",
                    "Name",
                    model.CategoryId);

            return View(model);
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,StockQuantity,ImageUrl,CategoryId")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Products/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
