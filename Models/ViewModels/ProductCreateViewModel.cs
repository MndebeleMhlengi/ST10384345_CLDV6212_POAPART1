using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models.ViewModels
{
    // ViewModel specifically for the product creation form
    public class ProductCreateViewModel
    {
        [Required]
        [Display(Name = "Product ID")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than $0.00.")]
        [Display(Name = "Price")]
        public double Price { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
        [Display(Name = "Initial Stock")]
        public int StockAvailable { get; set; }

        // This property is specifically for the file upload from the form
        [Required(ErrorMessage = "Please upload a product image.")]
        [Display(Name = "Product Image")]
        public IFormFile ProductImageFile { get; set; }
    }
}