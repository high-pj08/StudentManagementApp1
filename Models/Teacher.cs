using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApp.Models
{
    public class Teacher
    {
        public int Id { get; set; }

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
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [Display(Name = "Date of Joining")]
        [DataType(DataType.Date)]
        public DateTime DateOfJoining { get; set; } = DateTime.Today;

        [StringLength(100)]
        [Display(Name = "Subject Taught")]
        public string? SubjectTaught { get; set; } // This could become less relevant if assignments are granular

        // Foreign Key to ApplicationUser (for login)
        public string? ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; }

        // Navigation property for TeacherClassSubject assignments
        public ICollection<TeacherClassSubject>? TeacherClassSubjects { get; set; }

        // Helper property for display in dropdowns
        [NotMapped] // This property is not mapped to the database
        public string FullName => $"{FirstName} {LastName}";
    }
}
