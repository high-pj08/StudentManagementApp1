using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApp.Data;
using StudentManagementApp.Models;
using System.Linq;

namespace StudentManagementApp.Controllers
{
    // Only students can access this controller
    [Authorize(Roles = "Student")]
    public class StudentAttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentAttendanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: StudentAttendance/ViewMyAttendance
        // Allows a student to view their own attendance records.
        public async Task<IActionResult> ViewMyAttendance()
        {
            ViewData["Title"] = "My Attendance";
            var currentUserId = _userManager.GetUserId(User);

            // Find the student record associated with the logged-in ApplicationUser
            var student = await _context.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == currentUserId);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Your student profile is not linked. Please contact an administrator.";
                return RedirectToAction("Index", "Home"); // Redirect if student record not found
            }

            // Retrieve attendance records for this specific student
            var attendanceRecords = await _context.Attendances
                                                  .Where(a => a.StudentId == student.Id)
                                                  .Include(a => a.Class)
                                                  .Include(a => a.Subject)
                                                  .OrderByDescending(a => a.AttendanceDate)
                                                  .ToListAsync();

            return View(attendanceRecords);
        }
    }
}
