using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApp.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Payment Date")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Required]
        [Column(TypeName = "decimal(18, 2)")] // For currency, use decimal type
        [Range(0.01, 1000000.00, ErrorMessage = "Amount must be greater than 0.")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; } // Foreign Key to the Student the payment is for

        [ForeignKey("StudentId")]
        public Student? Student { get; set; } // Navigation property to Student

        [Required]
        [Display(Name = "Paid By Parent")]
        public int ParentId { get; set; } // Foreign Key to the Parent who made the payment

        [ForeignKey("ParentId")]
        public Parent? Parent { get; set; } // Navigation property to Parent

        [StringLength(50)]
        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; } = "Online (Simulated)"; // e.g., "Cash", "Bank Transfer", "Online"

        [StringLength(50)]
        [Display(Name = "Payment Status")]
        public string Status { get; set; } = "Paid"; // e.g., "Pending", "Paid", "Failed", "Refunded"

        [StringLength(255)]
        public string? TransactionId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; } // Any additional notes

        // NEW: Foreign Key to Invoice
        public int? InvoiceId { get; set; } // Nullable, as some payments might not be linked to a specific invoice initially
        [ForeignKey("InvoiceId")]
        public Invoice? Invoice { get; set; } // Navigation property to Invoice
    }
}
