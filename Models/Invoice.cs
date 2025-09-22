using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace StudentManagementApp.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Invoice Number")]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty; // e.g., INV-2023-0001

        [Required]
        [Display(Name = "Issue Date")]
        [DataType(DataType.Date)]
        public DateTime IssueDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; } // Who the invoice is for

        [ForeignKey("StudentId")]
        public Student? Student { get; set; } // Navigation property to Student

        [Required]
        [Display(Name = "Parent Responsible")]
        public int ParentId { get; set; } // Who is responsible for paying

        [ForeignKey("ParentId")]
        public Parent? Parent { get; set; } // Navigation property to Parent

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; } = 0.00m; // Default to 0

        [NotMapped] // This property is calculated, not stored in DB
        [Display(Name = "Balance Due")]
        public decimal BalanceDue => TotalAmount - AmountPaid;

        [Required]
        [StringLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Outstanding"; // e.g., "Outstanding", "Partially Paid", "Paid", "Overdue"

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation property for invoice line items
        public ICollection<InvoiceItem>? InvoiceItems { get; set; } = new List<InvoiceItem>();

        // Navigation property for payments linked to this invoice
        public ICollection<Payment>? Payments { get; set; } = new List<Payment>();
    }
}
