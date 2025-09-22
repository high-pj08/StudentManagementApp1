using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectListItem

namespace StudentManagementApp.ViewModels
{
    public class ClassFeeViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Class is required.")]
        [Display(Name = "Class")]
        public int ClassId { get; set; }
        public IEnumerable<SelectListItem>? Classes { get; set; } // Dropdown for available classes

        [Required(ErrorMessage = "Fee Type is required.")]
        [Display(Name = "Fee Type")]
        public int FeeTypeId { get; set; }
        public IEnumerable<SelectListItem>? FeeTypes { get; set; } // Dropdown for available fee types

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 1000000.00, ErrorMessage = "Amount must be greater than 0.")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Effective Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Effective Date")]
        public DateTime EffectiveDate { get; set; } = DateTime.Today;

        // For display purposes in list views
        public string? ClassName { get; set; }
        public string? FeeTypeName { get; set; }
    }
}
