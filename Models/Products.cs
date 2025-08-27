using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailers.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Required]
        [Display(Name = "Product ID")]
        public string ProductId
        {
            get => RowKey;
            set => RowKey = value;
        }

        [Required]
        [Display(Name = "Product Name")]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public double Price { get; set; }

        [Required]
        [Display(Name = "Stock Available")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int StockAvailable { get; set; }

        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = string.Empty;

        // 👇 This is the uploaded file (not stored in Table Storage)
        [NotMapped]
        [Display(Name = "Product Image")]
        public IFormFile? ProductImage { get; set; }
    }
}
