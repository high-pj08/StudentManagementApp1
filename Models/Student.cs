using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // Make sure this is included

namespace StudentManagementApp.Models
{
    public class Student
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

        [Display(Name = "Enrollment Date")]
        [DataType(DataType.Date)]
        public DateTime EnrollmentDate { get; set; } = DateTime.Today;

        [Display(Name = "Admission Date")]
        [DataType(DataType.Date)]
        public DateTime AdmissionDate { get; set; } = DateTime.Today;

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        // Foreign Key to ApplicationUser (for login)
        public string? ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; }

        // Navigation properties for relationships
        public ICollection<Parent>? Parents { get; set; } = new List<Parent>(); // Many-to-many with Parent

        // NEW: Collection for Enrollments
        public ICollection<Enrollment>? Enrollments { get; set; } = new List<Enrollment>();

        // NEW: Foreign Key to Class (assuming a student belongs to one main class)
        // This is crucial for the .ThenInclude(s => s.Class) to work
        public int? ClassId { get; set; } // Nullable if a student might not be assigned a class yet
        [ForeignKey("ClassId")]
        public Class? Class { get; set; } // Navigation property to the Class model

        // Helper property for display in dropdowns
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}
