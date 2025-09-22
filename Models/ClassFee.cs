using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApp.Models
{
    public class ClassFee
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Class")]
        public int ClassId { get; set; } // Foreign Key to Class

        [ForeignKey("ClassId")]
        public Class? Class { get; set; }

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

        [Display(Name = "Effective Date")]
        [DataType(DataType.Date)]
        public DateTime EffectiveDate { get; set; } = DateTime.Today;
    }
}
