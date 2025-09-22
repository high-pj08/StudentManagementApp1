using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.ViewModels
{
    public class InvoiceListViewModel
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

        [Display(Name = "Student Name")]
        public string? StudentName { get; set; }

        [Display(Name = "Parent Name")]
        public string? ParentName { get; set; }

        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; }

        [Display(Name = "Balance Due")]
        public decimal BalanceDue { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;
    }
}
