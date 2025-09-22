using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApp.Models
{
    public class StudentFee
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; } // Foreign Key to Student

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        [Required]
        [Display(Name = "Fee Type")]
        public int FeeTypeId { get; set; } // Foreign Key to FeeType

        [ForeignKey("FeeTypeId")]
        public FeeType? FeeType { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, 1000000.00, ErrorMessage = "Amount must be greater than 0.")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.Today.AddMonths(1); // Default to 1 month from now

        [StringLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Outstanding"; // e.g., "Outstanding", "Paid", "Waived"

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
