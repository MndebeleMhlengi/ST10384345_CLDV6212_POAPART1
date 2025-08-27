using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models.ViewModels
{
    public class OrderCreateViewModel
    {
        [Required]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product")]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        [Required]
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        public string Status { get; set; } = "Pending";

        // For dropdown lists
        public List<Customer> Customers { get; set; } = new List<Customer>();
        public List<Product> Products { get; set; } = new List<Product>();
    }
}
