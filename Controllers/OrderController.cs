using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Services;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public OrderController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: Order
        public async Task<IActionResult> Index()
        {
            try
            {
                var orders = await _storageService.GetAllOrdersAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading orders: {ex.Message}";
                return View(new List<Order>());
            }
        }

        // GET: Order/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = new OrderCreateViewModel
                {
                    Customers = await _storageService.GetAllCustomersAsync(),
                    Products = await _storageService.GetAllProductsAsync(),
                    OrderDate = DateTime.Now,
                    Status = "Pending"
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading create form: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var customer = await _storageService.GetCustomerAsync(viewModel.CustomerId);
                    var product = await _storageService.GetProductAsync(viewModel.ProductId);

                    if (customer != null && product != null)
                    {
                        // FIX: Explicitly set the DateTimeKind to Utc before creating the order.
                        // The Azure SDK requires this to avoid an "Unspecified" kind error.
                        var utcOrderDate = DateTime.SpecifyKind(viewModel.OrderDate, DateTimeKind.Utc);

                        var order = new Order
                        {
                            OrderId = Guid.NewGuid().ToString(),
                            CustomerId = viewModel.CustomerId,
                            Username = customer.Username,
                            ProductId = viewModel.ProductId,
                            ProductName = product.ProductName,
                            OrderDate = utcOrderDate, // Use the corrected UTC date here
                            Quantity = viewModel.Quantity,
                            UnitPrice = product.Price,
                            TotalPrice = product.Price * viewModel.Quantity,
                            Status = viewModel.Status
                        };

                        await _storageService.CreateOrderAsync(order);
                        TempData["Success"] = "Order created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid customer or product selected.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                }
            }

            // If we got this far, something failed, redisplay form
            viewModel.Customers = await _storageService.GetAllCustomersAsync();
            viewModel.Products = await _storageService.GetAllProductsAsync();
            return View(viewModel);
        }

        // GET: Order/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var order = await _storageService.GetOrderAsync(id);
                if (order == null)
                    return NotFound();

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading order: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Order/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            // Remove ETag validation errors since we're handling it manually
            ModelState.Remove("ETag");
            ModelState.Remove("Timestamp");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the current order to retrieve the ETag and all current data
                    var currentOrder = await _storageService.GetOrderAsync(id);
                    if (currentOrder == null)
                    {
                        TempData["Error"] = "Order not found or may have been modified by another user.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Preserve all the original values and only update the status
                    currentOrder.Status = order.Status;

                    // Ensure UTC datetime
                    currentOrder.OrderDate = DateTime.SpecifyKind(currentOrder.OrderDate, DateTimeKind.Utc);

                    await _storageService.UpdateOrderAsync(currentOrder);
                    TempData["Success"] = "Order status updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");

                    // Reload the order data if update fails
                    try
                    {
                        var reloadedOrder = await _storageService.GetOrderAsync(id);
                        if (reloadedOrder != null)
                        {
                            return View(reloadedOrder);
                        }
                    }
                    catch
                    {
                        // If we can't reload, redirect to index
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            return View(order);
        }

        // GET: Order/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var order = await _storageService.GetOrderAsync(id);
                if (order == null)
                    return NotFound();

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading order details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Order/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var order = await _storageService.GetOrderAsync(id);
                if (order != null)
                {
                    await _storageService.DeleteOrderAsync(id);
                    TempData["Success"] = "Order deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Order not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX: Get Product Price
        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _storageService.GetProductAsync(productId);
                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        name = product.ProductName
                    });
                }
                return Json(new { success = false, message = "Product not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}