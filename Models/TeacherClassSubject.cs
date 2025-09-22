using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApp.Models
{
    public class TeacherClassSubject
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Teacher")]
        public int TeacherId { get; set; } // Foreign Key to Teacher

        [ForeignKey("TeacherId")]
        public Teacher? Teacher { get; set; } // Navigation property to Teacher

        [Required]
        [Display(Name = "Class")]
        public int ClassId { get; set; } // Foreign Key to Class

        [ForeignKey("ClassId")]
        public Class? Class { get; set; } // Navigation property to Class

        [Required]
        [Display(Name = "Subject")]
        public int SubjectId { get; set; } // Foreign Key to Subject

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; } // Navigation property to Subject

        [Display(Name = "Assignment Date")]
        [DataType(DataType.Date)]
        public DateTime AssignmentDate { get; set; } = DateTime.Today;

        // You could add properties like "IsActive", "RoleInClass" (e.g., "Lead Teacher", "Assistant")
    }
}
