using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApp.Models
{
    public class Mark
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Exam")]
        public int ExamId { get; set; } // Foreign Key to Exam

        [ForeignKey("ExamId")]
        public Exam? Exam { get; set; } // Navigation property to Exam

        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; } // Foreign Key to Student

        [ForeignKey("StudentId")]
        public Student? Student { get; set; } // Navigation property to Student

        // Make SubjectId nullable to allow migration with existing data
        public int? SubjectId { get; set; } // CHANGED to nullable (int?)
        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        // Make ClassId nullable to allow migration with existing data
        public int? ClassId { get; set; } // CHANGED to nullable (int?)
        [ForeignKey("ClassId")]
        public Class? Class { get; set; }

        [Required]
        [Display(Name = "Marks Obtained")]
        [Range(0, 1000, ErrorMessage = "Marks must be between 0 and Max Marks of the exam.")]
        public int MarksObtained { get; set; }

        [Display(Name = "Date Recorded")]
        [DataType(DataType.Date)]
        public DateTime DateRecorded { get; set; } = DateTime.Today;
    }
}
