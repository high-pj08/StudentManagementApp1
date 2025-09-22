// Inside ViewModels/PaymentViewModels.cs

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList

namespace StudentManagementApp.ViewModels
{
    public class PaymentListViewModel
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; } // Added to link to invoice details
        [Display(Name = "Invoice No.")]
        public string InvoiceNumber { get; set; }
        [Display(Name = "Student Name")]
        public string StudentName { get; set; }
        [Display(Name = "Parent Name")]
        public string ParentName { get; set; }
        [Display(Name = "Payment Date")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; }
        [Display(Name = "Amount")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }
        [Display(Name = "Method")]
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
    }

    public class CreatePaymentViewModel
    {
        [Required(ErrorMessage = "Please select an invoice.")]
        [Display(Name = "Select Invoice")]
        public int InvoiceId { get; set; }
        public SelectList Invoices { get; set; } // For the dropdown list of invoices

        [Required]
        [Display(Name = "Payment Date")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash"; // Default value for admin entry

        [Display(Name = "Status")]
        public string Status { get; set; } = "Completed"; // Default status for manually recorded payments

        [Display(Name = "Transaction ID")]
        public string? TransactionId { get; set; } // Optional, for online payments mostly

        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }

    public class PaymentDetailsViewModel
    {
        public int Id { get; set; }
        public int? InvoiceId { get; set; }
        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; }
        [Display(Name = "Student Name")]
        public string StudentFullName { get; set; }
        [Display(Name = "Parent Name")]
        public string ParentFullName { get; set; }
        [Display(Name = "Payment Date")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; }
        [Display(Name = "Amount Paid")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }
        [Display(Name = "Status")]
        public string Status { get; set; }
        [Display(Name = "Transaction ID")]
        public string? TransactionId { get; set; }
        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
}