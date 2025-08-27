using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Required]
        [Display(Name = "Order ID")]
        public string OrderId
        {
            get => RowKey;
            set => RowKey = value;
        }

        [Required]
        [Display(Name = "Customer ID")]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product ID")]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required]
        [Display(Name = "Unit Price")]
        public double UnitPrice { get; set; }

        [Required]
        [Display(Name = "Total Price")]
        public double TotalPrice { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";
    }
}
