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
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; // Ensure this is injected

        public StudentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager; // Assign RoleManager
        }

        // GET: Students
        // Admins and Teachers can view all students.
        // Students can only view their own details (handled by logic within the view).
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Students List";

            // The view itself will filter what's displayed for 'Student' role.
            // The controller here provides all data, and the view renders selectively.
            // This design allows Teachers/Admins to see all, while Students only see their own.
            return View(await _context.Students.ToListAsync());
        }

        // GET: Students/Details/5
        // Admins and Teachers can view any student's details.
        // Students can only view their *own* details.
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.ApplicationUser) // Include associated user for display
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            // If the current user is a Student, ensure they are only viewing their own record.
            // This is a server-side check to prevent direct URL access to other students' details.
            if (User.IsInRole("Student"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (student.ApplicationUserId != currentUserId)
                {
                    // If a student tries to view someone else's details, deny access.
                    return Forbid();
                }
            }

            ViewData["Title"] = $"Student Details: {student.FirstName} {student.LastName}";
            return View(student);
        }

        // GET: Students/Create
        // Only Admins can create new students.
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["Title"] = "Create New Student";
            return View();
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,PhoneNumber,Address,EnrollmentDate,DateOfBirth,Gender")] Student student, string password)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrWhiteSpace(password))
                {
                    var user = new ApplicationUser
                    {
                        UserName = student.Email,
                        Email = student.Email,
                        EmailConfirmed = true,
                        FirstName = student.FirstName,
                        LastName = student.LastName
                    };

                    var result = await _userManager.CreateAsync(user, password);
                    if (result.Succeeded)
                    {
                        // Ensure "Student" role exists before adding user to it
                        if (!await _roleManager.RoleExistsAsync("Student"))
                        {
                            var roleCreationResult = await _roleManager.CreateAsync(new IdentityRole("Student"));
                            if (!roleCreationResult.Succeeded)
                            {
                                ModelState.AddModelError(string.Empty, "Failed to create 'Student' role. " + string.Join("; ", roleCreationResult.Errors.Select(e => e.Description)));
                                return View(student);
                            }
                        }
                        await _userManager.AddToRoleAsync(user, "Student");

                        student.ApplicationUserId = user.Id;
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, $"User creation error: {error.Description}");
                        }
                        return View(student);
                    }
                }
                else
                {
                    if (await _context.Students.AnyAsync(s => s.Email == student.Email))
                    {
                        ModelState.AddModelError("Email", "A student with this email already exists.");
                        return View(student);
                    }
                }

                _context.Add(student);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Student '{student.FirstName} {student.LastName}' added successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }

        // GET: Students/Edit/5
        // Admins and Teachers can edit any student.
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            // If the current user is a Student, ensure they are only editing their own record.
            if (User.IsInRole("Student"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (student.ApplicationUserId != currentUserId)
                {
                    return Forbid();
                }
            }

            ViewData["Title"] = $"Edit Student: {student.FirstName} {student.LastName}";
            return View(student);
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Email,PhoneNumber,Address,EnrollmentDate,DateOfBirth,Gender,ApplicationUserId")] Student student)
        {
            if (id != student.Id)
            {
                return NotFound();
            }

            // If the current user is a Student, ensure they are only editing their own record.
            if (User.IsInRole("Student"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (student.ApplicationUserId != currentUserId)
                {
                    return Forbid();
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Student '{student.FirstName} {student.LastName}' updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id))
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
            ViewData["Title"] = $"Edit Student: {student.FirstName} {student.LastName}";
            return View(student);
        }

        // GET: Students/Delete/5
        // Only Admins can delete students.
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }
            ViewData["Title"] = $"Delete Student: {student.FirstName} {student.LastName}";
            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);

                if (!string.IsNullOrEmpty(student.ApplicationUserId))
                {
                    var user = await _userManager.FindByIdAsync(student.ApplicationUserId);
                    if (user != null)
                    {
                        await _userManager.DeleteAsync(user);
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Student deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}
