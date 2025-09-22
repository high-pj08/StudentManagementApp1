using StudentManagementApp.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // For List

namespace StudentManagementApp.ViewModels
{
    public class InvoiceDetailsViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Display(Name = "Issue Date")]
        [DataType(DataType.Date)]
        public DateTime IssueDate { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Display(Name = "Student")]
        public string StudentFullName { get; set; } = string.Empty;
        public int StudentId { get; set; }

        [Display(Name = "Parent Responsible")]
        public string ParentFullName { get; set; } = string.Empty;
        public int ParentId { get; set; }

        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; }

        [Display(Name = "Balance Due")]
        public decimal BalanceDue => TotalAmount - AmountPaid;

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public List<InvoiceItemViewModel> InvoiceItems { get; set; } = new List<InvoiceItemViewModel>();
        public List<Payment> Payments { get; set; } = new List<Payment>();
    }
}
