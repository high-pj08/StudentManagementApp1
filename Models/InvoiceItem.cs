using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApp.Models
{
    public class InvoiceItem
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Invoice")]
        public int InvoiceId { get; set; } // Foreign Key to the Invoice this item belongs to

        [ForeignKey("InvoiceId")]
        public Invoice? Invoice { get; set; }

        [Required]
        [Display(Name = "Fee Type")]
        public int FeeTypeId { get; set; } // Foreign Key to the FeeType

        [ForeignKey("FeeTypeId")]
        public FeeType? FeeType { get; set; } // Navigation property to FeeType

        [Required]
        [StringLength(100)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty; // e.g., "Tuition Fee - Sep 2023"

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; } // Amount for this specific item
    }
}
