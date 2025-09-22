using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApp.Data;
using StudentManagementApp.Models;

namespace StudentManagementApp.Controllers
{
    // By default, only authenticated users can access this controller.
    [Authorize]
    public class SubjectsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Subjects
        // Admins, Teachers, and Students can view all subjects.
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Subjects List";
            return View(await _context.Subjects.ToListAsync());
        }

        // GET: Subjects/Details/5
        // Admins, Teachers, and Students can view details of a specific subject.
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(m => m.Id == id);
            if (subject == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Subject Details: {subject.Name}";
            return View(subject);
        }
    }
}
