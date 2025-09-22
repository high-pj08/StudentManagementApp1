using System.ComponentModel.DataAnnotations;
using StudentManagementApp.Models; // Assuming Payment and Invoice models are here
using System.Collections.Generic; // For List

namespace StudentManagementApp.ViewModels
{
    public class ParentInvoiceListViewModel
    {
        public int ParentId { get; set; }

        [Display(Name = "Parent Name")]
        public string ParentFullName { get; set; } = string.Empty;

        public List<ParentInvoiceSummaryViewModel> Invoices { get; set; } = new List<ParentInvoiceSummaryViewModel>();
    }

    public class ParentInvoiceSummaryViewModel
    {
        public int InvoiceId { get; set; }

        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Display(Name = "For Student")]
        public string StudentFullName { get; set; } = string.Empty;

        [Display(Name = "Issue Date")]
        [DataType(DataType.Date)]
        public DateTime IssueDate { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; }

        [Display(Name = "Balance Due")]
        public decimal BalanceDue { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        public bool IsOverdue => Status == "Outstanding" && DueDate < DateTime.Today;
    }
}
