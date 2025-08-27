using ABCRetailers.Models;

namespace ABCRetailers.Services
{
    public interface IAzureStorageService
    {
        // Table Storage - Customers
        Task<Customer> GetCustomerAsync(string customerId);
        Task<List<Customer>> GetAllCustomersAsync();
        Task<Customer> CreateCustomerAsync(Customer customer);
        Task<Customer> UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(string customerId);

        // Table Storage - Products
        Task<Product> GetProductAsync(string productId);
        Task<List<Product>> GetAllProductsAsync();
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task DeleteProductAsync(string productId);

        // Table Storage - Orders
        Task<Order> GetOrderAsync(string orderId);
        Task<List<Order>> GetAllOrdersAsync();
        Task<Order> CreateOrderAsync(Order order);
        Task<Order> UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(string orderId);

        // Blob Storage - Product Images
        Task<string> UploadProductImageAsync(IFormFile file, string productId);
        Task<Stream> GetProductImageAsync(string imageName);
        Task DeleteProductImageAsync(string imageName);

        // File Share - Payment Proofs
        Task<string> UploadPaymentProofAsync(IFormFile file, string orderId, string customerName);
        Task<List<string>> GetAllPaymentProofsAsync();
        Task<Stream> DownloadPaymentProofAsync(string fileName);
        Task DeletePaymentProofAsync(string fileName);

        // Queue Storage - Order Processing
        Task SendOrderMessageAsync(Order order);
        Task<List<Order>> GetOrderMessagesAsync(int count = 10);
    }
}
