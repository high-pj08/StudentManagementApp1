using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.ViewModels
{
    public class EditUserRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;

        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        // All available roles in the system
        public List<string> AllRoles { get; set; } = new List<string>();

        // Roles currently assigned to the user (selected by checkboxes)
        public List<string> UserRoles { get; set; } = new List<string>();
    }
}
