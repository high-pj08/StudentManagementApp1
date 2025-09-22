using System;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.ViewModels
{
    public class NoticeViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required.")]
        [DataType(DataType.MultilineText)] // Suggests a larger text area
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Published Date")]
        [DataType(DataType.Date)]
        public DateTime PublishDate { get; set; } = DateTime.Today;

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }
}