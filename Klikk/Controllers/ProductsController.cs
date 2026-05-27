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
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;

namespace Klikk.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly Cloudinary _cloudinary;

        public ProductsController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;

            var account = new Account(
                configuration["CloudinarySettings:CloudName"],
                configuration["CloudinarySettings:ApiKey"],
                configuration["CloudinarySettings:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
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
                .ThenInclude(r => r.User)
                .Include(p => p.GalleryImages)
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
            // ========================================
            // VALIDATE GALLERY IMAGE COUNT
            // ========================================

            if (model.GalleryImages == null ||
                model.GalleryImages.Count < 3 ||
                model.GalleryImages.Count > 5)
            {
                ModelState.AddModelError(
                    "GalleryImages",
                    "Please upload between 3 and 5 gallery images.");
            }

            if (ModelState.IsValid)
            {
                string? imageUrl = null;

                if (model.ImageFile != null)
                {
                    using var stream = model.ImageFile.OpenReadStream();

                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(
                            model.ImageFile.FileName,
                            stream)
                    };

                    var uploadResult =
                        await _cloudinary.UploadAsync(uploadParams);

                    imageUrl = uploadResult.SecureUrl.ToString();
                }

                Product product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    StockQuantity = model.StockQuantity,
                    CategoryId = model.CategoryId,
                    ImageUrl = imageUrl
                };

                _context.Add(product);

                await _context.SaveChangesAsync();

                // ========================================
                // SAVE GALLERY IMAGES
                // ========================================

                if (model.GalleryImages != null)
                {
                    foreach (var image in model.GalleryImages)
                    {
                        using var stream = image.OpenReadStream();

                        var uploadParams = new ImageUploadParams
                        {
                            File = new FileDescription(
                                image.FileName,
                                stream)
                        };

                        var uploadResult =
                            await _cloudinary.UploadAsync(uploadParams);

                        var galleryImage = new ProductGalleryImage
                        {
                            ProductId = product.Id,
                            ImageUrl = uploadResult.SecureUrl.ToString()
                        };

                        _context.ProductGalleryImages.Add(galleryImage);
                    }

                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] =
                new SelectList(_context.Categories,
                    "Id",
                    "Name",
                    model.CategoryId);

            return View(model);
        }

        // POST: Products/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var product = await _context.Products
                    .Include(p => p.GalleryImages)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return NotFound();
                }

                product.Name = model.Name;
                product.Description = model.Description;
                product.Price = model.Price;
                product.StockQuantity = model.StockQuantity;
                product.CategoryId = model.CategoryId;

                // ========================================
                // UPLOAD NEW IMAGE TO CLOUDINARY
                // ========================================

                if (model.ImageFile != null)
                {
                    using var stream = model.ImageFile.OpenReadStream();

                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(
                            model.ImageFile.FileName,
                            stream)
                    };

                    var uploadResult =
                        await _cloudinary.UploadAsync(uploadParams);

                    product.ImageUrl =
                        uploadResult.SecureUrl.ToString();
                }

                try
                {
                    // ========================================
                    // REPLACE GALLERY IMAGES IF NEW ONES EXIST
                    // ========================================

                    if (model.GalleryImages != null &&
                        model.GalleryImages.Count > 0)
                    {
                        // REMOVE OLD GALLERY IMAGES

                        if (product.GalleryImages != null)
                        {
                            _context.ProductGalleryImages.RemoveRange(
                                product.GalleryImages);
                        }

                        // ADD NEW GALLERY IMAGES

                        foreach (var image in model.GalleryImages)
                        {
                            using var stream = image.OpenReadStream();

                            var uploadParams = new ImageUploadParams
                            {
                                File = new FileDescription(
                                    image.FileName,
                                    stream)
                            };

                            var uploadResult =
                                await _cloudinary.UploadAsync(uploadParams);

                            var galleryImage = new ProductGalleryImage
                            {
                                ProductId = product.Id,
                                ImageUrl = uploadResult.SecureUrl.ToString()
                            };

                            _context.ProductGalleryImages.Add(galleryImage);
                        }
                    }

                    _context.Update(product);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(model.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(AdminIndex));
            }

            ViewData["CategoryId"] =
                new SelectList(
                    _context.Categories,
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

            var product = await _context.Products
                .Include(p => p.GalleryImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                ExistingImageUrl = product.ImageUrl,
                ExistingGalleryImages = product.GalleryImages?.ToList()
            };

            ViewData["CategoryId"] =
                new SelectList(
                    _context.Categories,
                    "Id",
                    "Name",
                    product.CategoryId);

            return View(viewModel);
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
            return RedirectToAction(nameof(AdminIndex));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
