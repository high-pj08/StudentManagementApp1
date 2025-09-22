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
    public class StudentMarksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentMarksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: StudentMarks/ViewMyMarks
        public async Task<IActionResult> ViewMyMarks()
        {
            ViewData["Title"] = "My Marks";
            var userId = _userManager.GetUserId(User);
            var studentRecord = await _context.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (studentRecord == null)
            {
                TempData["ErrorMessage"] = "Your student profile is not linked. Please contact an administrator.";
                return RedirectToAction("Index", "Home");
            }

            // Fetch all marks for the current student
            // Include Exam, and through Exam, include Class and Subject for display purposes
            var studentMarks = await _context.Marks
                                             .Include(m => m.Exam)
                                                .ThenInclude(e => e!.Class) // Use ! to assert non-null after Include
                                             .Include(m => m.Exam)
                                                .ThenInclude(e => e!.Subject) // Use ! to assert non-null after Include
                                             .Where(m => m.StudentId == studentRecord.Id)
                                             .OrderByDescending(m => m.Exam!.ExamDate) // Order by exam date
                                             .ThenBy(m => m.Exam!.Subject!.Name) // Then by subject name
                                             .ToListAsync();

            ViewBag.StudentName = studentRecord.FullName; // Pass student name to view

            return View(studentMarks);
        }
    }
}
