using StudentManagementApp.Models;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.ViewModels
{
    public class StudentDashboardViewModel
    {
        public int StudentId { get; set; }

        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Admission Date")]
        [DataType(DataType.Date)]
        public DateTime AdmissionDate { get; set; }

        public string CurrentClassName { get; set; } = string.Empty;

        // Enrollment Information
        public List<Enrollment>? Enrollments { get; set; } = new List<Enrollment>();

        // Attendance Summary
        public int TotalAttendanceDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public double AttendancePercentage { get; set; }
        public List<Attendance>? RecentAttendance { get; set; } = new List<Attendance>(); // E.g., last 5 records

        // Marks Information
        public List<Mark>? RecentMarks { get; set; } = new List<Mark>(); // E.g., last 5 exam marks
        public Dictionary<string, double>? SubjectAverages { get; set; } = new Dictionary<string, double>(); // Avg marks per subject

        // Upcoming Exams
        public List<Exam>? UpcomingExams { get; set; } = new List<Exam>();
    }
}
