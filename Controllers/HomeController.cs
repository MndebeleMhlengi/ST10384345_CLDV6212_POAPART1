using System.Diagnostics;
using ABCRetailers.Models;
using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Services;
using ABCRetailers.Models.ViewModels;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public HomeController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _storageService.GetAllProductsAsync();
            var customers = await _storageService.GetAllCustomersAsync();
            var orders = await _storageService.GetAllOrdersAsync();

            var viewModel = new HomeViewModel
            {
                FeaturedProducts = products.Take(5).ToList(),
                CustomerCount = customers.Count,
                ProductCount = products.Count,
                OrderCount = orders.Count
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
