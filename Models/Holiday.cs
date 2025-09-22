using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.Models
{
    public class Holiday
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Holiday Date is required.")]
        [Display(Name = "Holiday Date")]
        [DataType(DataType.Date)]
        public DateTime HolidayDate { get; set; }
    }
}
