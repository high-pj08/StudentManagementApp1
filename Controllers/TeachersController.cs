using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApp.Data;
using StudentManagementApp.Models;

namespace StudentManagementApp.Controllers
{
    // By default, only authenticated users can access this controller.
    // Specific actions will have more restrictive role-based authorization.
    [Authorize]
    public class TeachersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; // Ensure this is injected

        public TeachersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager; // Assign RoleManager
        }

        // GET: Teachers
        // Admins can view all teachers.
        // Teachers can only view their own record (handled by logic within the view).
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Teachers List";
            // The view itself will filter what's displayed for 'Teacher' role.
            return View(await _context.Teachers.ToListAsync());
        }

        // GET: Teachers/Details/5
        // Admins can view any teacher's details.
        // Teachers can only view their *own* details.
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .Include(t => t.ApplicationUser) // Include associated user for display
                .FirstOrDefaultAsync(m => m.Id == id);

            if (teacher == null)
            {
                return NotFound();
            }

            // If the current user is a Teacher, ensure they are only viewing their own record.
            // This is a server-side check to prevent direct URL access to other teachers' details.
            if (User.IsInRole("Teacher"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (teacher.ApplicationUserId != currentUserId)
                {
                    // If a teacher tries to view someone else's details, deny access.
                    return Forbid();
                }
            }

            ViewData["Title"] = $"Teacher Details: {teacher.FirstName} {teacher.LastName}";
            return View(teacher);
        }

        // GET: Teachers/Create
        // Only Admins can create new teachers.
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["Title"] = "Create New Teacher";
            return View();
        }

        // POST: Teachers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,PhoneNumber,Address,DateOfJoining,SubjectTaught")] Teacher teacher, string password)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrWhiteSpace(password))
                {
                    var user = new ApplicationUser
                    {
                        UserName = teacher.Email,
                        Email = teacher.Email,
                        EmailConfirmed = true,
                        FirstName = teacher.FirstName,
                        LastName = teacher.LastName
                    };

                    var result = await _userManager.CreateAsync(user, password);
                    if (result.Succeeded)
                    {
                        // Ensure "Teacher" role exists before adding user to it
                        if (!await _roleManager.RoleExistsAsync("Teacher"))
                        {
                            var roleCreationResult = await _roleManager.CreateAsync(new IdentityRole("Teacher"));
                            if (!roleCreationResult.Succeeded)
                            {
                                ModelState.AddModelError(string.Empty, "Failed to create 'Teacher' role. " + string.Join("; ", roleCreationResult.Errors.Select(e => e.Description)));
                                return View(teacher);
                            }
                        }
                        await _userManager.AddToRoleAsync(user, "Teacher");

                        teacher.ApplicationUserId = user.Id;
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, $"User creation error: {error.Description}");
                        }
                        return View(teacher);
                    }
                }
                else
                {
                    if (await _context.Teachers.AnyAsync(t => t.Email == teacher.Email))
                    {
                        ModelState.AddModelError("Email", "A teacher with this email already exists.");
                        return View(teacher);
                    }
                }

                _context.Add(teacher);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Teacher '{teacher.FirstName} {teacher.LastName}' added successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(teacher);
        }

        // GET: Teachers/Edit/5
        // Admins can edit any teacher. Teachers can edit their own.
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }

            // If the current user is a Teacher, ensure they are only editing their own record.
            if (User.IsInRole("Teacher"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (teacher.ApplicationUserId != currentUserId)
                {
                    return Forbid();
                }
            }

            ViewData["Title"] = $"Edit Teacher: {teacher.FirstName} {teacher.LastName}";
            return View(teacher);
        }

        // POST: Teachers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Email,PhoneNumber,Address,DateOfJoining,SubjectTaught,ApplicationUserId")] Teacher teacher)
        {
            if (id != teacher.Id)
            {
                return NotFound();
            }

            // If the current user is a Teacher, ensure they are only editing their own record.
            if (User.IsInRole("Teacher"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (teacher.ApplicationUserId != currentUserId)
                {
                    return Forbid();
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(teacher);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Teacher '{teacher.FirstName} {teacher.LastName}' updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeacherExists(teacher.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["Title"] = $"Edit Teacher: {teacher.FirstName} {teacher.LastName}";
            return View(teacher);
        }

        // GET: Teachers/Delete/5
        // Only Admins can delete teachers.
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }
            ViewData["Title"] = $"Delete Teacher: {teacher.FirstName} {teacher.LastName}";
            return View(teacher);
        }

        // POST: Teachers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher != null)
            {
                _context.Teachers.Remove(teacher);

                if (!string.IsNullOrEmpty(teacher.ApplicationUserId))
                {
                    var user = await _userManager.FindByIdAsync(teacher.ApplicationUserId);
                    if (user != null)
                    {
                        await _userManager.DeleteAsync(user);
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Teacher deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool TeacherExists(int id)
        {
            return _context.Teachers.Any(e => e.Id == id);
        }
    }
}
