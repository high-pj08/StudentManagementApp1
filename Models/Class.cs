using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApp.Models
{
    public class Class
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Class Name")]
        public string Name { get; set; } = string.Empty; // e.g., "Grade 10", "Class 5A"

        [StringLength(50)]
        public string? Section { get; set; } // e.g., "A", "B", "Morning"

        [Display(Name = "Year/Grade Level")]
        public int? YearLevel { get; set; } // e.g., 10 for Grade 10

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public ICollection<Enrollment>? Enrollments { get; set; } = new List<Enrollment>(); // NEW
        public ICollection<TeacherClassSubject>? TeacherClassSubjects { get; set; } = new List<TeacherClassSubject>();

        // Helper property for display in dropdowns
        [NotMapped] // This property is not mapped to the database
        public string NameWithSection => $"{Name} - {Section}";
    }
}
