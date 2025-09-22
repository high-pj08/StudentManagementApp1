using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApp.Models
{
    public class Attendance
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Attendance Date")]
        [DataType(DataType.Date)]
        public DateTime AttendanceDate { get; set; } = DateTime.Today;

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

        [Required]
        [Display(Name = "Subject")]
        public int SubjectId { get; set; } // Foreign Key to Subject (for specific subject attendance)

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; } // Navigation property to Subject

        [Required]
        [StringLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Present"; // e.g., "Present", "Absent", "Late", "Excused"

        public bool IsPresent { get; set; }
    }
}
