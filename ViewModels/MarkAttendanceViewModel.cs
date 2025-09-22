using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList

namespace StudentManagementApp.ViewModels
{
    public class MarkAttendanceViewModel
    {
        [Display(Name = "Class")]
        [Required(ErrorMessage = "Please select a class.")]
        public int ClassId { get; set; }

        [Display(Name = "Subject")]
        [Required(ErrorMessage = "Please select a subject.")]
        public int SubjectId { get; set; }

        [Display(Name = "Attendance Date")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Please select an attendance date.")]
        public DateTime AttendanceDate { get; set; } = DateTime.Today;

        public List<StudentAttendanceViewModel> Students { get; set; } = new List<StudentAttendanceViewModel>();

        // SelectLists for dropdowns, filtered by teacher's assignments
        public SelectList? AssignedClasses { get; set; }
        public SelectList? AssignedSubjects { get; set; }
    }

    public class StudentAttendanceViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public string Status { get; set; } = "Absent"; // Default status for new entries

        public bool IsEnrolledInClass { get; set; } // Indicates if student is currently enrolled in the selected class
    }
}
