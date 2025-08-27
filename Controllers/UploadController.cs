using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetailers.Controllers
{
    public class UploadController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IAzureStorageService storageService, ILogger<UploadController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        // GET: Upload
        public async Task<IActionResult> Index()
        {
            try
            {
                var paymentProofs = await _storageService.GetAllPaymentProofsAsync();
                ViewBag.PaymentProofs = paymentProofs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment proofs");
                ViewBag.PaymentProofs = new List<string>();
                TempData["Error"] = "Error loading payment proof files.";
            }

            return View(new FileUploadModel());
        }

        // POST: Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    var paymentProofs = await _storageService.GetAllPaymentProofsAsync();
                    ViewBag.PaymentProofs = paymentProofs;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving payment proofs");
                    ViewBag.PaymentProofs = new List<string>();
                }
                return View(model);
            }

            // Validate file
            if (model.ProofOfPayment == null || model.ProofOfPayment.Length == 0)
            {
                ModelState.AddModelError("ProofOfPayment", "Please select a file to upload.");
                return await Index();
            }

            // Validate file type
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
            var fileExtension = Path.GetExtension(model.ProofOfPayment.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("ProofOfPayment",
                    "Only PDF, Image (JPG, JPEG, PNG), and Document (DOC, DOCX) files are allowed.");
                return await Index();
            }

            // Validate file size (5MB max)
            const int maxFileSize = 5 * 1024 * 1024; // 5MB in bytes
            if (model.ProofOfPayment.Length > maxFileSize)
            {
                ModelState.AddModelError("ProofOfPayment", "File size cannot exceed 5MB.");
                return await Index();
            }

            try
            {
                // Set default values if not provided
                var orderId = !string.IsNullOrEmpty(model.OrderId) ? model.OrderId : "GENERAL";
                var customerName = !string.IsNullOrEmpty(model.CustomerName) ? model.CustomerName : "UNKNOWN";

                var fileName = await _storageService.UploadPaymentProofAsync(
                    model.ProofOfPayment,
                    orderId,
                    customerName);

                TempData["Success"] = $"Payment proof uploaded successfully as '{fileName}'.";
                _logger.LogInformation("Payment proof uploaded: {FileName}", fileName);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading payment proof");
                ModelState.AddModelError("", $"Error uploading file: {ex.Message}");

                try
                {
                    var paymentProofs = await _storageService.GetAllPaymentProofsAsync();
                    ViewBag.PaymentProofs = paymentProofs;
                }
                catch (Exception listEx)
                {
                    _logger.LogError(listEx, "Error retrieving payment proofs after upload error");
                    ViewBag.PaymentProofs = new List<string>();
                }

                return View(model);
            }
        }

        // GET: Upload/Download/filename
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return NotFound();

            try
            {
                var fileStream = await _storageService.DownloadPaymentProofAsync(fileName);
                if (fileStream == null)
                    return NotFound();

                var contentType = GetContentType(fileName);
                return File(fileStream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading payment proof: {FileName}", fileName);
                TempData["Error"] = "Error downloading file.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Upload/DeleteFile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return NotFound();

            try
            {
                await _storageService.DeletePaymentProofAsync(fileName);
                TempData["Success"] = "Payment proof deleted successfully.";
                _logger.LogInformation("Payment proof deleted: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment proof: {FileName}", fileName);
                TempData["Error"] = "Error deleting file.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper method to get content type based on file extension
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }
    }
}
