using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.ViewModels
{
    public class EnterMarksViewModel
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int? MaxMarks { get; set; }

        public List<StudentMarkEntryViewModel> Students { get; set; } = new List<StudentMarkEntryViewModel>();
    }

    public class StudentMarkEntryViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;

        [Display(Name = "Marks")]
        [Range(0, 1000, ErrorMessage = "Marks must be a non-negative number.")] // Initial range, will be validated against MaxMarks in controller
        public int? MarksObtained { get; set; }
    }
}
