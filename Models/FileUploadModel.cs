using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class FileUploadModel
    {
        [Required]
        [Display(Name = "Proof of Payment")]
        public IFormFile ProofOfPayment { get; set; } = null!;

        [Display(Name = "Order ID")]
        public string OrderId { get; set; } = string.Empty;

        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;
    }
}
