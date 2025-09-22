using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApp.Data;
using StudentManagementApp.Models;
using StudentManagementApp.ViewModels;
using System.Linq;

namespace StudentManagementApp.Controllers
{
    [Authorize(Roles = "Student")] // Only students can access this dashboard
    public class StudentDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "My Dashboard";
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students
                                        .Include(s => s.ApplicationUser)
                                        .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Your student profile is not linked. Please contact an administrator.";
                return RedirectToAction("Index", "Home");
            }

            // --- Fetch Enrollments ---
            var enrollments = await _context.Enrollments
                                            .Include(e => e.Class)
                                            .Include(e => e.Subject) // Include Subject for the Enrollment model
                                            .Where(e => e.StudentId == student.Id && e.Status == "Active")
                                            .ToListAsync();

            var currentClass = enrollments.FirstOrDefault()?.Class; // Assuming one primary class for simplicity
            if (currentClass == null)
            {
                TempData["WarningMessage"] = "You are not currently enrolled in any class.";
            }

            // --- Fetch Attendance Data ---
            var studentAttendance = await _context.Attendances
                                                  .Where(a => a.StudentId == student.Id)
                                                  .ToListAsync();
            // FIX: Use Status string comparison instead of non-existent IsPresent
            int presentDays = studentAttendance.Count(a => a.Status == "Present");
            int absentDays = studentAttendance.Count(a => a.Status == "Absent"); // Assuming "Absent" is the status for absent days
            int totalAttendanceDays = studentAttendance.Count;
            double attendancePercentage = totalAttendanceDays > 0 ? (double)presentDays / totalAttendanceDays * 100 : 0;

            // --- Fetch Recent Marks ---
            var recentMarks = await _context.Marks
                                            .Include(m => m.Exam)
                                                .ThenInclude(e => e!.Subject)
                                            .Include(m => m.Exam)
                                                .ThenInclude(e => e!.Class)
                                            .Where(m => m.StudentId == student.Id)
                                            .OrderByDescending(m => m.DateRecorded)
                                            .Take(5) // Get latest 5 marks
                                            .ToListAsync();

            // --- Calculate Subject Averages (for all marks, not just recent) ---
            var allMarksForStudent = await _context.Marks
                                                   .Include(m => m.Exam)
                                                      .ThenInclude(e => e!.Subject)
                                                   .Where(m => m.StudentId == student.Id && m.Exam != null && m.Exam.MaxMarks.HasValue)
                                                   .ToListAsync();

            var subjectAverages = allMarksForStudent
                .GroupBy(m => m.Exam!.Subject!.Name)
                .ToDictionary(
                    group => group.Key,
                    group => group.Average(m => (double)m.MarksObtained / m.Exam!.MaxMarks!.Value * 100)
                );


            // --- Fetch Upcoming Exams (for the student's current class) ---
            var upcomingExams = new List<Exam>();
            if (currentClass != null)
            {
                upcomingExams = await _context.Exams
                                              .Include(e => e.Class)
                                              .Include(e => e.Subject)
                                              // FIX: Compare DateTime.Date with DateTime.Today.Date
                                              .Where(e => e.ClassId == currentClass.Id && e.ExamDate >= DateOnly.FromDateTime(DateTime.Today))
                                              .OrderBy(e => e.ExamDate)
                                              .Take(5) // Get next 5 upcoming exams
                                              .ToListAsync();
            }

            var viewModel = new StudentDashboardViewModel
            {
                StudentId = student.Id,
                FullName = student.FullName,
                Email = student.ApplicationUser?.Email ?? "N/A",
                // FIX: Handle nullable DateOfBirth with null-coalescing operator
                DateOfBirth = student.DateOfBirth ?? DateTime.MinValue, // Provide a default non-nullable DateTime
                AdmissionDate = student.AdmissionDate, // Now exists in Student model
                CurrentClassName = currentClass?.NameWithSection ?? "Not Enrolled",
                Enrollments = enrollments,

                TotalAttendanceDays = totalAttendanceDays,
                PresentDays = presentDays,
                AbsentDays = absentDays,
                AttendancePercentage = attendancePercentage,
                RecentAttendance = studentAttendance.OrderByDescending(a => a.AttendanceDate).Take(5).ToList(), // Latest 5 attendance records

                RecentMarks = recentMarks,
                SubjectAverages = subjectAverages,
                UpcomingExams = upcomingExams
            };

            return View(viewModel);
        }
    }
}
