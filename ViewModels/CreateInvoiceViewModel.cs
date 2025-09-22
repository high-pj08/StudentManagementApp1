using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace StudentManagementApp.ViewModels
{
    // This ViewModel is used for both Create and Edit Invoice forms in the Admin panel.
    public class CreateInvoiceViewModel
    {
        public int Id { get; set; } // For Edit scenario

        [Required(ErrorMessage = "Invoice Number is required.")]
        [StringLength(50, ErrorMessage = "Invoice Number cannot exceed 50 characters.")]
        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Issue Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Issue Date")]
        public DateTime IssueDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Due Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; } = DateTime.Today.AddMonths(1);

        [Required(ErrorMessage = "Student is required.")]
        [Display(Name = "Student")]
        public int StudentId { get; set; }
        public IEnumerable<SelectListItem>? Students { get; set; }

        [Display(Name = "Parent Responsible")]
        public int? ParentId { get; set; }
        public IEnumerable<SelectListItem>? Parents { get; set; }

        [Required(ErrorMessage = "At least one invoice item is required.")]
        [MinLength(1, ErrorMessage = "At least one invoice item is required.")]
        public List<InvoiceItemViewModel> InvoiceItems { get; set; } = new List<InvoiceItemViewModel>();

        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; } = 0.00m;

        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Outstanding";

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }

        public string? StudentFullName { get; set; }
        public string? ParentFullName { get; set; }
        public decimal BalanceDue => TotalAmount - AmountPaid;
    }

    public class InvoiceItemViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Fee Type is required.")]
        [Display(Name = "Fee Type")]
        public int FeeTypeId { get; set; }
        public IEnumerable<SelectListItem>? FeeTypes { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 1000000.00, ErrorMessage = "Amount must be greater than 0.")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        public string? FeeTypeName { get; set; }
    }
}
