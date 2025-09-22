using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList

namespace StudentManagementApp.ViewModels
{
    public class ParentViewModel
    {
        public int Id { get; set; } // For Edit operations

        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty; // This is the Parent's contact email

        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }


        // For linking to an existing ApplicationUser account (for login)
        [Display(Name = "Linked User Account (Optional)")]
        public string? ApplicationUserId { get; set; }
        public IEnumerable<SelectListItem>? ApplicationUsers { get; set; } // Dropdown for user accounts

        // NEW: Fields for creating a NEW ApplicationUser if one isn't selected above
        [EmailAddress]
        [Display(Name = "New User Email (if creating new login)")]
        public string? NewUserEmail { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New User Password")]
        public string? NewUserPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New User Password")]
        [Compare("NewUserPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmNewUserPassword { get; set; }


        // For linking to children (students)
        [Display(Name = "Linked Children (Students)")]
        public List<int>? SelectedStudentIds { get; set; } // Stores selected student IDs
        public IEnumerable<SelectListItem>? AllStudents { get; set; } // Dropdown for all students
    }
}
