using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApp.Data;
using StudentManagementApp.Models; // Ensure this is included

namespace StudentManagementApp.Controllers // This is the correct namespace declaration
{
    // By default, only authenticated users can access this controller.
    [Authorize]
    public class ClassesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Classes
        // Admins, Teachers, and Students can view all classes.
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Classes List";
            return View(await _context.Classes.ToListAsync());
        }

        // GET: Classes/Details/5
        // Admins, Teachers, and Students can view details of a specific class.
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @class = await _context.Classes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@class == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Class Details: {@class.Name} ({@class.Section})";
            return View(@class);
        }
    }
}
