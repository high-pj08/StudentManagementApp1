using StudentManagementApp.Models;

namespace StudentManagementApp.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalUsers { get; set; } // Total registered ApplicationUsers
        // You can add more counts here as your application grows, e.g., TotalClasses, TotalSubjects

        public List<Notice> ActiveNotices { get; set; } = new List<Notice>();
        public List<Holiday> UpcomingHolidays { get; set; } = new List<Holiday>();
    }
}
