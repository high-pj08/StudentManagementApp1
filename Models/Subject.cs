using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.Models
{
    public class Subject
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Subject Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Subject Code")]
        public string? Code { get; set; } // e.g., "MATH101", "ENG201"

        [StringLength(500)]
        public string? Description { get; set; }
        public ICollection<TeacherClassSubject>? TeacherClassSubjects { get; set; } = new List<TeacherClassSubject>(); // NEW
    }
}
