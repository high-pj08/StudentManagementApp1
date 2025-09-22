using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectListItem

namespace StudentManagementApp.ViewModels
{
    public class StudentFeeViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Student is required.")]
        [Display(Name = "Student")]
        public int StudentId { get; set; }
        public IEnumerable<SelectListItem>? Students { get; set; } // Dropdown for available students

        [Required(ErrorMessage = "Fee Type is required.")]
        [Display(Name = "Fee Type")]
        public int FeeTypeId { get; set; }
        public IEnumerable<SelectListItem>? FeeTypes { get; set; } // Dropdown for available fee types

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 1000000.00, ErrorMessage = "Amount must be greater than 0.")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Due Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; } = DateTime.Today.AddMonths(1);

        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Outstanding";

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }

        // For display purposes in list views
        public string? StudentName { get; set; }
        public string? FeeTypeName { get; set; }
    }
}
