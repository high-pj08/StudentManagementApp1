// Models/Exam.cs
using System; // Ensure this is present for DateOnly
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApp.Models
{
    public class Exam
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Display(Name = "Exam Date")]
        [DataType(DataType.Date)]
        public DateOnly ExamDate { get; set; } // <--- ENSURE THIS IS DateOnly

        [Required]
        [Display(Name = "Class")]
        public int ClassId { get; set; } // Foreign Key to Class
        [ForeignKey("ClassId")]
        public Class? Class { get; set; }

        [Required]
        [Display(Name = "Subject")]
        public int SubjectId { get; set; } // Foreign Key to Subject
        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        [Required]
        [Display(Name = "Teacher")]
        public int TeacherId { get; set; } // Foreign Key to Teacher
        [ForeignKey("TeacherId")]
        public Teacher? Teacher { get; set; }

        [Display(Name = "Maximum Marks")]
        [Range(0, 1000, ErrorMessage = "Max Marks must be between 0 and 1000.")]
        public int? MaxMarks { get; set; }
    }
}