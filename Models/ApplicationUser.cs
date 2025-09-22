using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.Models
{
    // Extend IdentityUser to add custom properties for your users
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string? FirstName { get; set; } // Nullable to allow for initial setup without requiring it

        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; } // Nullable

        // You can add more common properties for all users here if needed
        // For example, a profile picture URL, or a registration date
    }
}
