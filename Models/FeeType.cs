using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApp.Models
{
    public class FeeType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Fee Type Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Is Recurring")]
        public bool IsRecurring { get; set; } // e.g., monthly, annually
    }
}
