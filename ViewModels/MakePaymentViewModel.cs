using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentManagementApp.ViewModels
{
    public class MakePaymentViewModel
    {
        [Display(Name = "Parent Name")]
        public string ParentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a student.")]
        [Display(Name = "Paying For Student")]
        public int StudentId { get; set; }
        public IEnumerable<SelectListItem>? Students { get; set; }

        [Display(Name = "Pay Against Invoice (Optional)")]
        public int? SelectedInvoiceId { get; set; }
        public IEnumerable<SelectListItem>? OutstandingInvoices { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [DataType(DataType.Currency)]
        [Range(0.01, 1000000.00, ErrorMessage = "Amount must be greater than 0.")]
        [Display(Name = "Amount to Pay")]
        public decimal Amount { get; set; }

        [Display(Name = "Payment Date")]
        [Required(ErrorMessage = "Payment date is required.")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes (Optional)")]
        public string? Notes { get; set; }
    }
}
