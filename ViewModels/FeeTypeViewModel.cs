using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.ViewModels
{
    public class FeeTypeViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Fee Type Name is required.")]
        [StringLength(100, ErrorMessage = "Fee Type Name cannot exceed 100 characters.")]
        [Display(Name = "Fee Type Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Display(Name = "Is Recurring")]
        public bool IsRecurring { get; set; }
    }
}
