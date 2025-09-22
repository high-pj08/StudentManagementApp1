using StudentManagementApp.Models;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementApp.ViewModels
{
    public class ParentDashboardViewModel
    {
        public int ParentId { get; set; }

        [Display(Name = "Parent Name")]
        public string ParentFullName { get; set; } = string.Empty;

        [Display(Name = "Parent Email")]
        public string ParentEmail { get; set; } = string.Empty;

        // List of children, each with their relevant dashboard data
        public List<ChildDashboardViewModel> Children { get; set; } = new List<ChildDashboardViewModel>();
    }

    public class ChildDashboardViewModel
    {
        public int StudentId { get; set; }

        [Display(Name = "Student Name")]
        public string StudentFullName { get; set; } = string.Empty;

        [Display(Name = "Student Email")]
        public string StudentEmail { get; set; } = string.Empty;

        [Display(Name = "Class")]
        public string ClassNameWithSection { get; set; } = "Not Enrolled";

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
