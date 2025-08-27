using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public CustomerController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: Customer
        public async Task<IActionResult> Index()
        {
            var customers = await _storageService.GetAllCustomersAsync();
            return View(customers);
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _storageService.CreateCustomerAsync(customer);
                    TempData["Success"] = "Customer created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var customer = await _storageService.GetCustomerAsync(id);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch the original entity from the database
                    var originalCustomer = await _storageService.GetCustomerAsync(customer.RowKey);
                    if (originalCustomer == null)
                    {
                        return NotFound();
                    }

                    // Update the fields of the original entity
                    originalCustomer.Name = customer.Name;
                    originalCustomer.Surname = customer.Surname;
                    originalCustomer.Email = customer.Email;
                    originalCustomer.Username = customer.Username;
                    originalCustomer.ShippingAddress = customer.ShippingAddress;

                    // Save the updated original entity back to the database
                    await _storageService.UpdateCustomerAsync(originalCustomer);
                    TempData["Success"] = "Customer updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        // POST: Customer/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteCustomerAsync(id);
                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}