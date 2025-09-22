using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApp.Models
{
    public class Enrollment
    {
        public int Id { get; set; }

        [Display(Name = "Enrollment Date")]
        [DataType(DataType.Date)]
        public DateTime EnrollmentDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; } // Foreign Key to Student

        [ForeignKey("StudentId")]
        public Student? Student { get; set; } // Navigation property to Student

        [Required]
        [Display(Name = "Class")]
        public int ClassId { get; set; } // Foreign Key to Class

        [ForeignKey("ClassId")]
        public Class? Class { get; set; } // Navigation property to Class

        [Required] // An enrollment is for a specific subject within a class
        [Display(Name = "Subject")]
        public int SubjectId { get; set; } // Foreign Key to Subject

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; } // Navigation property to Subject

        // You could add properties like Grade, Status (e.g., "Active", "Completed", "Withdrawn") here
        [StringLength(500)]
        public string? Status { get; set; } = "Active"; // Default status
    }
}
