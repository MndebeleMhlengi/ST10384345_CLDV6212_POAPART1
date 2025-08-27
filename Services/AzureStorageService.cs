using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using System.Text.Json;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetailers.Services
{
    public class AzureStorageService : IAzureStorageService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly QueueServiceClient _queueServiceClient;
        private readonly ShareServiceClient _shareServiceClient;

        private readonly string _customersTableName = "customers";
        private readonly string _productsTableName = "products";
        private readonly string _ordersTableName = "orders";
        private readonly string _productImagesContainerName = "productimages";
        private readonly string _orderQueueName = "orderprocessing";
        private readonly string _paymentProofsShareName = "paymentproofs";

        public AzureStorageService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("AzureStorage");

            _tableServiceClient = new TableServiceClient(connectionString);
            _blobServiceClient = new BlobServiceClient(connectionString);
            _queueServiceClient = new QueueServiceClient(connectionString);
            _shareServiceClient = new ShareServiceClient(connectionString);

            InitializeStorageAsync().Wait();
        }

        private async Task InitializeStorageAsync()
        {
            // Create tables
            await _tableServiceClient.CreateTableIfNotExistsAsync(_customersTableName);
            await _tableServiceClient.CreateTableIfNotExistsAsync(_productsTableName);
            await _tableServiceClient.CreateTableIfNotExistsAsync(_ordersTableName);

            // Create blob container
            var containerClient = _blobServiceClient.GetBlobContainerClient(_productImagesContainerName);
            await containerClient.CreateIfNotExistsAsync();

            // Create queue
            var queueClient = _queueServiceClient.GetQueueClient(_orderQueueName);
            await queueClient.CreateIfNotExistsAsync();

            // Create file share
            var shareClient = _shareServiceClient.GetShareClient(_paymentProofsShareName);
            await shareClient.CreateIfNotExistsAsync();
        }

        // Customer Methods
        public async Task<Customer> GetCustomerAsync(string customerId)
        {
            var tableClient = _tableServiceClient.GetTableClient(_customersTableName);
            var response = await tableClient.GetEntityIfExistsAsync<Customer>("Customer", customerId);
            return response.HasValue ? response.Value : null;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient(_customersTableName);
            var customers = new List<Customer>();

            await foreach (var customer in tableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }

            return customers;
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            var tableClient = _tableServiceClient.GetTableClient(_customersTableName);
            customer.RowKey = customer.CustomerId;
            customer.PartitionKey = "Customer";

            await tableClient.AddEntityAsync(customer);
            return customer;
        }

        public async Task<Customer> UpdateCustomerAsync(Customer customer)
        {
            var tableClient = _tableServiceClient.GetTableClient(_customersTableName);
            customer.PartitionKey = "Customer";

            await tableClient.UpdateEntityAsync(customer, customer.ETag);
            return customer;
        }

        public async Task DeleteCustomerAsync(string customerId)
        {
            var tableClient = _tableServiceClient.GetTableClient(_customersTableName);
            await tableClient.DeleteEntityAsync("Customer", customerId);
        }

        // Product Methods
        public async Task<Product> GetProductAsync(string productId)
        {
            var tableClient = _tableServiceClient.GetTableClient(_productsTableName);
            var response = await tableClient.GetEntityIfExistsAsync<Product>("Product", productId);
            return response.HasValue ? response.Value : null;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient(_productsTableName);
            var products = new List<Product>();

            await foreach (var product in tableClient.QueryAsync<Product>())
            {
                products.Add(product);
            }

            return products;
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            var tableClient = _tableServiceClient.GetTableClient(_productsTableName);
            product.RowKey = product.ProductId;
            product.PartitionKey = "Product";

            await tableClient.AddEntityAsync(product);
            return product;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            var tableClient = _tableServiceClient.GetTableClient(_productsTableName);
            product.PartitionKey = "Product";

            await tableClient.UpdateEntityAsync(product, product.ETag);
            return product;
        }

        public async Task DeleteProductAsync(string productId)
        {
            var tableClient = _tableServiceClient.GetTableClient(_productsTableName);
            await tableClient.DeleteEntityAsync("Product", productId);
        }

        // Order Methods
        public async Task<Order> GetOrderAsync(string orderId)
        {
            var tableClient = _tableServiceClient.GetTableClient(_ordersTableName);
            var response = await tableClient.GetEntityIfExistsAsync<Order>("Order", orderId);
            return response.HasValue ? response.Value : null;
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient(_ordersTableName);
            var orders = new List<Order>();

            await foreach (var order in tableClient.QueryAsync<Order>())
            {
                orders.Add(order);
            }

            return orders;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            var tableClient = _tableServiceClient.GetTableClient(_ordersTableName);
            order.RowKey = order.OrderId;
            order.PartitionKey = "Order";

            await tableClient.AddEntityAsync(order);

            // Send to processing queue
            await SendOrderMessageAsync(order);

            return order;
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            var tableClient = _tableServiceClient.GetTableClient(_ordersTableName);
            order.PartitionKey = "Order";

            await tableClient.UpdateEntityAsync(order, order.ETag);
            return order;
        }

        public async Task DeleteOrderAsync(string orderId)
        {
            var tableClient = _tableServiceClient.GetTableClient(_ordersTableName);
            await tableClient.DeleteEntityAsync("Order", orderId);
        }

        // Blob Storage Methods - Product Images
        public async Task<string> UploadProductImageAsync(IFormFile file, string productId)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_productImagesContainerName);
            var fileName = $"{productId}_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(file.FileName)}";
            var blobClient = containerClient.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobClient.Uri.ToString();
        }

        public async Task<Stream> GetProductImageAsync(string imageName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_productImagesContainerName);
            var blobClient = containerClient.GetBlobClient(imageName);

            if (await blobClient.ExistsAsync())
            {
                var response = await blobClient.DownloadStreamingAsync();
                return response.Value.Content;
            }

            return null;
        }

        public async Task DeleteProductImageAsync(string imageName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_productImagesContainerName);
            var blobClient = containerClient.GetBlobClient(imageName);
            await blobClient.DeleteIfExistsAsync();
        }

        // File Share Methods - Payment Proofs
        public async Task<string> UploadPaymentProofAsync(IFormFile file, string orderId, string customerName)
        {
            var shareClient = _shareServiceClient.GetShareClient(_paymentProofsShareName);
            var directoryClient = shareClient.GetRootDirectoryClient();

            var fileName = $"{orderId}_{customerName}_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(file.FileName)}";
            var fileClient = directoryClient.GetFileClient(fileName);

            using var stream = file.OpenReadStream();
            await fileClient.CreateAsync(stream.Length);
            await fileClient.UploadAsync(stream);

            return fileName;
        }

        public async Task<List<string>> GetAllPaymentProofsAsync()
        {
            var shareClient = _shareServiceClient.GetShareClient(_paymentProofsShareName);
            var directoryClient = shareClient.GetRootDirectoryClient();
            var files = new List<string>();

            await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                {
                    files.Add(item.Name);
                }
            }

            return files;
        }

        public async Task<Stream> DownloadPaymentProofAsync(string fileName)
        {
            var shareClient = _shareServiceClient.GetShareClient(_paymentProofsShareName);
            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            if (await fileClient.ExistsAsync())
            {
                var response = await fileClient.DownloadAsync();
                return response.Value.Content;
            }

            return null;
        }

        public async Task DeletePaymentProofAsync(string fileName)
        {
            var shareClient = _shareServiceClient.GetShareClient(_paymentProofsShareName);
            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            await fileClient.DeleteIfExistsAsync();
        }

        // Queue Storage Methods
        public async Task SendOrderMessageAsync(Order order)
        {
            var queueClient = _queueServiceClient.GetQueueClient(_orderQueueName);
            var message = JsonSerializer.Serialize(order);
            await queueClient.SendMessageAsync(message);
        }

        public async Task<List<Order>> GetOrderMessagesAsync(int count = 10)
        {
            var queueClient = _queueServiceClient.GetQueueClient(_orderQueueName);
            var orders = new List<Order>();

            var response = await queueClient.ReceiveMessagesAsync(count);

            foreach (var message in response.Value)
            {
                try
                {
                    var order = JsonSerializer.Deserialize<Order>(message.MessageText);
                    if (order != null)
                    {
                        orders.Add(order);
                    }
                }
                catch (JsonException)
                {
                    // Handle invalid JSON messages
                    continue;
                }
            }

            return orders;
        }
    }
}

