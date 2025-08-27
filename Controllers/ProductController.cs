using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;
using System.IO;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ABCRetailers.Models.ViewModels;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public ProductController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: Product
        public async Task<IActionResult> Index()
        {
            var products = await _storageService.GetAllProductsAsync();
            return View(products);
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            return View(new ProductCreateViewModel());
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string imageUrl = null;
                    if (viewModel.ProductImageFile != null && viewModel.ProductImageFile.Length > 0)
                    {
                        imageUrl = await _storageService.UploadProductImageAsync(
                            viewModel.ProductImageFile, viewModel.ProductId.ToString()
                        );
                    }

                    var product = new Product
                    {
                        ProductId = viewModel.ProductId.ToString(),
                        ProductName = viewModel.ProductName,
                        Description = viewModel.Description,
                        Price = viewModel.Price,
                        StockAvailable = viewModel.StockAvailable,
                        ImageUrl = imageUrl
                    };

                    await _storageService.CreateProductAsync(product);
                    TempData["Success"] = "✅ Product created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                }
            }
            return View(viewModel);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var product = await _storageService.GetProductAsync(id);
            if (product == null)
                return NotFound();

            // Create and populate the ViewModel with the product's data
            var editViewModel = new ProductCreateViewModel
            {
                ProductId = int.Parse(product.ProductId),
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                StockAvailable = product.StockAvailable,
                // Do not populate ProductImageFile here, as it's for new uploads
            };

            return View(editViewModel);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var productToUpdate = await _storageService.GetProductAsync(viewModel.ProductId.ToString());
                    if (productToUpdate == null)
                    {
                        return NotFound();
                    }

                    // Map all properties from the viewModel to the productToUpdate entity
                    productToUpdate.ProductName = viewModel.ProductName;
                    productToUpdate.Description = viewModel.Description;
                    productToUpdate.Price = viewModel.Price; // This is the fix to update the price
                    productToUpdate.StockAvailable = viewModel.StockAvailable;

                    // Handle the image file upload
                    if (viewModel.ProductImageFile != null && viewModel.ProductImageFile.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(productToUpdate.ImageUrl))
                        {
                            var oldImageName = Path.GetFileName(new Uri(productToUpdate.ImageUrl).LocalPath);
                            await _storageService.DeleteProductImageAsync(oldImageName);
                        }
                        productToUpdate.ImageUrl = await _storageService.UploadProductImageAsync(
                            viewModel.ProductImageFile, productToUpdate.ProductId.ToString()
                        );
                    }

                    await _storageService.UpdateProductAsync(productToUpdate);
                    TempData["Success"] = "✅ Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }
            return View(viewModel);
        }

        // POST: Product/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var product = await _storageService.GetProductAsync(id);
                if (product != null && !string.IsNullOrEmpty(product.ImageUrl))
                {
                    var imageName = Path.GetFileName(new Uri(product.ImageUrl).LocalPath);
                    await _storageService.DeleteProductImageAsync(imageName);
                }

                await _storageService.DeleteProductAsync(id);
                TempData["Success"] = "✅ Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}