using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementApp.Data; // Inject ApplicationDbContext
using StudentManagementApp.Models;
using StudentManagementApp.Services;
using StudentManagementApp.ViewModels;
using StudentManagementApp.Services;

namespace StudentManagementApp.Controllers
{
    // Authorize only users with the "Admin" role to access this controller
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context; // Inject ApplicationDbContext
        private readonly IInvoiceService _invoiceService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context, IInvoiceService invoiceService) // Add ApplicationDbContext to constructor
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _context = context;
            _invoiceService = invoiceService;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Admin Panel";
            return View();
        }

        // --- User Management Actions ---
        // GET: Admin/ManageUsers
        public async Task<IActionResult> ManageUsers()
        {
            ViewData["Title"] = "Manage Users";
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // GET: Admin/EditUserRoles/userId
        public async Task<IActionResult> EditUserRoles(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AllRoles = allRoles,
                UserRoles = userRoles.ToList()
            };

            ViewData["Title"] = $"Edit Roles for {user.UserName}";
            return View(model);
        }

        // POST: Admin/EditUserRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserRoles(EditUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = model.UserRoles.Except(userRoles);
            var rolesToRemove = userRoles.Except(model.UserRoles);

            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                foreach (var error in addResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                model.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                foreach (var error in removeResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                model.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            if (User.Identity?.IsAuthenticated == true && User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == user.Id)
            {
                await _signInManager.RefreshSignInAsync(user);
            }

            TempData["SuccessMessage"] = $"Roles for {user.UserName} updated successfully!";
            return RedirectToAction(nameof(ManageUsers));
        }

        // GET: Admin/CreateRole
        public IActionResult CreateRole()
        {
            ViewData["Title"] = "Create New Role";
            return View();
        }

        // POST: Admin/CreateRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                ModelState.AddModelError(string.Empty, "Role name cannot be empty.");
                return View();
            }

            if (await _roleManager.RoleExistsAsync(roleName))
            {
                ModelState.AddModelError(string.Empty, "Role with this name already exists.");
                return View();
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Role '{roleName}' created successfully!";
                return RedirectToAction(nameof(ManageUsers));
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View();
            }
        }

        // GET: Admin/DeleteRole/roleId
        public async Task<IActionResult> DeleteRole(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Delete Role: {role.Name}";
            return View(role);
        }

        // POST: Admin/DeleteRole/roleId
        [HttpPost, ActionName("DeleteRole")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoleConfirmed(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
            if (usersInRole.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete role '{role.Name}' because there are users assigned to it. Please remove users from this role first.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var result = await _roleManager.DeleteAsync(role);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Role '{role.Name}' deleted successfully!";
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] = $"Error deleting role: {error.Description}";
                }
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        // --- Teacher Management Actions ---

        // GET: Admin/ManageTeachers
        public async Task<IActionResult> ManageTeachers()
        {
            ViewData["Title"] = "Manage Teachers";
            // Include ApplicationUser if you want to display associated user details
            var teachers = await _context.Teachers.Include(t => t.ApplicationUser).ToListAsync();
            return View(teachers);
        }

        // GET: Admin/CreateTeacher
        public IActionResult CreateTeacher()
        {
            ViewData["Title"] = "Create New Teacher";
            return View();
        }

        // POST: Admin/CreateTeacher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher([Bind("FirstName,LastName,Email,PhoneNumber,Address,DateOfJoining,SubjectTaught")] Teacher teacher, string password)
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
                        if (!await _roleManager.RoleExistsAsync("Teacher"))
                        {
                            var roleCreationResult = await _roleManager.CreateAsync(new IdentityRole("Teacher"));
                            if (!roleCreationResult.Succeeded)
                            {
                                TempData["ErrorMessage"] = "Failed to create 'Teacher' role. " + string.Join("; ", roleCreationResult.Errors.Select(e => e.Description));
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
                        TempData["ErrorMessage"] = "Failed to create login account for teacher. Please check errors.";
                        return View(teacher);
                    }
                }
                else
                {
                    if (await _context.Teachers.AnyAsync(t => t.Email == teacher.Email))
                    {
                        ModelState.AddModelError("Email", "A teacher with this email already exists.");
                        TempData["ErrorMessage"] = "Teacher creation failed: A teacher with this email already exists.";
                        return View(teacher);
                    }
                }

                try
                {
                    _context.Add(teacher);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Teacher '{teacher.FirstName} {teacher.LastName}' added successfully!";
                    return RedirectToAction(nameof(ManageTeachers));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Database save error: {ex.Message}");
                    TempData["ErrorMessage"] = $"Failed to save teacher record: {ex.Message}";
                    return View(teacher);
                }
            }
            TempData["ErrorMessage"] = "Teacher creation failed due to validation errors. Please check the form.";
            return View(teacher);
        }

        // GET: Admin/EditTeacher/id
        public async Task<IActionResult> EditTeacher(int? id)
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
            ViewData["Title"] = $"Edit Teacher: {teacher.FirstName} {teacher.LastName}";
            return View(teacher);
        }

        // POST: Admin/EditTeacher/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher(int id, [Bind("Id,FirstName,LastName,Email,PhoneNumber,Address,DateOfJoining,SubjectTaught,ApplicationUserId")] Teacher teacher)
        {
            if (id != teacher.Id)
            {
                return NotFound();
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
                return RedirectToAction(nameof(ManageTeachers));
            }
            ViewData["Title"] = $"Edit Teacher: {teacher.FirstName} {teacher.LastName}";
            return View(teacher);
        }

        // GET: Admin/DetailsTeacher/id
        public async Task<IActionResult> DetailsTeacher(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .Include(t => t.ApplicationUser) // Include associated user if any
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }
            ViewData["Title"] = $"Teacher Details: {teacher.FirstName} {teacher.LastName}";
            return View(teacher);
        }

        // GET: Admin/DeleteTeacher/id
        public async Task<IActionResult> DeleteTeacher(int? id)
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

        // POST: Admin/DeleteTeacher/id
        [HttpPost, ActionName("DeleteTeacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeacherConfirmed(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher != null)
            {
                _context.Teachers.Remove(teacher);

                // Optional: Also delete the associated ApplicationUser if it exists
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
            return RedirectToAction(nameof(ManageTeachers));
        }

        // --- Student Management Actions ---

        // GET: Admin/ManageStudents
        public async Task<IActionResult> ManageStudents()
        {
            ViewData["Title"] = "Manage Students";
            // Include ApplicationUser if you want to display associated user details
            var students = await _context.Students.Include(s => s.ApplicationUser).ToListAsync();
            return View(students);
        }

        // GET: Admin/CreateStudent
        public IActionResult CreateStudent()
        {
            ViewData["Title"] = "Create New Student";
            return View();
        }

        // POST: Admin/CreateStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent([Bind("FirstName,LastName,Email,PhoneNumber,Address,EnrollmentDate,DateOfBirth,Gender")] Student student, string password)
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
                        if (!await _roleManager.RoleExistsAsync("Student"))
                        {
                            var roleCreationResult = await _roleManager.CreateAsync(new IdentityRole("Student"));
                            if (!roleCreationResult.Succeeded)
                            {
                                TempData["ErrorMessage"] = "Failed to create 'Student' role. " + string.Join("; ", roleCreationResult.Errors.Select(e => e.Description));
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
                        TempData["ErrorMessage"] = "Failed to create login account for student. Please check errors.";
                        return View(student);
                    }
                }
                else
                {
                    if (await _context.Students.AnyAsync(s => s.Email == student.Email))
                    {
                        ModelState.AddModelError("Email", "A student with this email already exists.");
                        TempData["ErrorMessage"] = "Student creation failed: A student with this email already exists.";
                        return View(student);
                    }
                }

                try
                {
                    _context.Add(student);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Student '{student.FirstName} {student.LastName}' added successfully!";
                    return RedirectToAction(nameof(ManageStudents));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Database save error: {ex.Message}");
                    TempData["ErrorMessage"] = $"Failed to save student record: {ex.Message}";
                    return View(student);
                }
            }
            TempData["ErrorMessage"] = "Student creation failed due to validation errors. Please check the form.";
            return View(student);
        }

        // GET: Admin/EditStudent/id
        public async Task<IActionResult> EditStudent(int? id)
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
            ViewData["Title"] = $"Edit Student: {student.FirstName} {student.LastName}";
            return View(student);
        }

        // POST: Admin/EditStudent/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(int id, [Bind("Id,FirstName,LastName,Email,PhoneNumber,Address,EnrollmentDate,DateOfBirth,Gender,ApplicationUserId")] Student student)
        {
            if (id != student.Id)
            {
                return NotFound();
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
                return RedirectToAction(nameof(ManageStudents));
            }
            ViewData["Title"] = $"Edit Student: {student.FirstName} {student.LastName}";
            return View(student);
        }

        // GET: Admin/DetailsStudent/id
        public async Task<IActionResult> DetailsStudent(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.ApplicationUser) // Include associated user if any
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }
            ViewData["Title"] = $"Student Details: {student.FirstName} {student.LastName}";
            return View(student);
        }

        // GET: Admin/DeleteStudent/id
        public async Task<IActionResult> DeleteStudent(int? id)
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

        // POST: Admin/DeleteStudent/id
        [HttpPost, ActionName("DeleteStudent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudentConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);

                // Optional: Also delete the associated ApplicationUser if it exists
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
            return RedirectToAction(nameof(ManageStudents));
        }

        // GET: Admin/ManageSubjects
        public async Task<IActionResult> ManageSubjects()
        {
            ViewData["Title"] = "Manage Subjects";
            return View(await _context.Subjects.ToListAsync());
        }

        // GET: Admin/CreateSubject
        public IActionResult CreateSubject()
        {
            ViewData["Title"] = "Create New Subject";
            return View();
        }

        // POST: Admin/CreateSubject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubject([Bind("Id,Name,Code,Description")] Subject subject)
        {
            if (ModelState.IsValid)
            {
                // Optional: Check for duplicate subject name or code before adding
                if (await _context.Subjects.AnyAsync(s => s.Name == subject.Name || (s.Code != null && s.Code == subject.Code)))
                {
                    ModelState.AddModelError(string.Empty, "A subject with this name or code already exists.");
                    TempData["ErrorMessage"] = "Subject creation failed: A subject with this name or code already exists.";
                    return View(subject);
                }

                _context.Add(subject);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Subject '{subject.Name}' added successfully!";
                return RedirectToAction(nameof(ManageSubjects));
            }
            TempData["ErrorMessage"] = "Subject creation failed due to validation errors. Please check the form.";
            return View(subject);
        }

        // GET: Admin/EditSubject/5
        public async Task<IActionResult> EditSubject(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
            {
                return NotFound();
            }
            ViewData["Title"] = $"Edit Subject: {subject.Name}";
            return View(subject);
        }

        // POST: Admin/EditSubject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubject(int id, [Bind("Id,Name,Code,Description")] Subject subject)
        {
            if (id != subject.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Optional: Check for duplicate subject name or code (excluding current subject)
                    if (await _context.Subjects.AnyAsync(s => (s.Name == subject.Name || (s.Code != null && s.Code == subject.Code)) && s.Id != subject.Id))
                    {
                        ModelState.AddModelError(string.Empty, "Another subject with this name or code already exists.");
                        TempData["ErrorMessage"] = "Subject update failed: Another subject with this name or code already exists.";
                        return View(subject);
                    }

                    _context.Update(subject);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Subject '{subject.Name}' updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubjectExists(subject.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageSubjects));
            }
            ViewData["Title"] = $"Edit Subject: {subject.Name}";
            return View(subject);
        }

        // GET: Admin/DetailsSubject/5
        public async Task<IActionResult> DetailsSubject(int? id)
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

        // GET: Admin/DeleteSubject/5
        public async Task<IActionResult> DeleteSubject(int? id)
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
            ViewData["Title"] = $"Delete Subject: {subject.Name}";
            return View(subject);
        }

        // POST: Admin/DeleteSubject/5
        [HttpPost, ActionName("DeleteSubject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubjectConfirmed(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject != null)
            {
                _context.Subjects.Remove(subject);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Subject deleted successfully!";
            return RedirectToAction(nameof(ManageSubjects));
        }

        // GET: Admin/ManageClasses
        public async Task<IActionResult> ManageClasses()
        {
            ViewData["Title"] = "Manage Classes";
            return View(await _context.Classes.ToListAsync());
        }

        // GET: Admin/CreateClass
        public IActionResult CreateClass()
        {
            ViewData["Title"] = "Create New Class";
            return View();
        }

        // POST: Admin/CreateClass
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass([Bind("Id,Name,Section,YearLevel,Description")] Class @class)
        {
            if (ModelState.IsValid)
            {
                // Optional: Check for duplicate class name and section before adding
                if (await _context.Classes.AnyAsync(c => c.Name == @class.Name && c.Section == @class.Section))
                {
                    ModelState.AddModelError(string.Empty, "A class with this name and section already exists.");
                    TempData["ErrorMessage"] = "Class creation failed: A class with this name and section already exists.";
                    return View(@class);
                }

                _context.Add(@class);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Class '{@class.Name} ({@class.Section})' added successfully!";
                return RedirectToAction(nameof(ManageClasses));
            }
            TempData["ErrorMessage"] = "Class creation failed due to validation errors. Please check the form.";
            return View(@class);
        }

        // GET: Admin/EditClass/5
        public async Task<IActionResult> EditClass(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @class = await _context.Classes.FindAsync(id);
            if (@class == null)
            {
                return NotFound();
            }
            ViewData["Title"] = $"Edit Class: {@class.Name} ({@class.Section})";
            return View(@class);
        }

        // POST: Admin/EditClass/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClass(int id, [Bind("Id,Name,Section,YearLevel,Description")] Class @class)
        {
            if (id != @class.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Optional: Check for duplicate class name and section (excluding current class)
                    if (await _context.Classes.AnyAsync(c => c.Name == @class.Name && c.Section == @class.Section && c.Id != @class.Id))
                    {
                        ModelState.AddModelError(string.Empty, "Another class with this name and section already exists.");
                        TempData["ErrorMessage"] = "Class update failed: Another class with this name and section already exists.";
                        return View(@class);
                    }

                    _context.Update(@class);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Class '{@class.Name} ({@class.Section})' updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassExists(@class.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageClasses));
            }
            ViewData["Title"] = $"Edit Class: {@class.Name} ({@class.Section})";
            return View(@class);
        }

        // GET: Admin/DetailsClass/5
        public async Task<IActionResult> DetailsClass(int? id)
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

        // GET: Admin/DeleteClass/5
        public async Task<IActionResult> DeleteClass(int? id)
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
            ViewData["Title"] = $"Delete Class: {@class.Name} ({@class.Section})";
            return View(@class);
        }

        // POST: Admin/DeleteClass/5
        [HttpPost, ActionName("DeleteClass")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClassConfirmed(int id)
        {
            var @class = await _context.Classes.FindAsync(id);
            if (@class != null)
            {
                _context.Classes.Remove(@class);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Class deleted successfully!";
            return RedirectToAction(nameof(ManageClasses));
        }

        // GET: Admin/ManageEnrollments
        public async Task<IActionResult> ManageEnrollments()
        {
            ViewData["Title"] = "Manage Enrollments";
            var enrollments = await _context.Enrollments
                                            .Include(e => e.Student)
                                            .Include(e => e.Class)
                                            .ToListAsync();
            return View(enrollments);
        }

        // GET: Admin/CreateEnrollment
        public async Task<IActionResult> CreateEnrollment()
        {
            ViewData["Title"] = "Create New Enrollment";
            ViewData["StudentId"] = new SelectList(await _context.Students.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync(), "Id", "FullName"); // Assuming FullName property in Student
            ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection"); // Assuming NameWithSection property in Class
            ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Admin/CreateEnrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEnrollment([Bind("Id,EnrollmentDate,StudentId,ClassId,SubjectId,Status")] Enrollment enrollment)

        {
            if (ModelState.IsValid)
            {
                // Prevent duplicate enrollments for the same student in the same class
                if (await _context.Enrollments.AnyAsync(e => e.StudentId == enrollment.StudentId && e.ClassId == enrollment.ClassId))
                {
                    ModelState.AddModelError(string.Empty, "This student is already enrolled in this class.");
                    TempData["ErrorMessage"] = "Enrollment failed: Student is already enrolled in this class.";
                    ViewData["StudentId"] = new SelectList(await _context.Students.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync(), "Id", "FullName", enrollment.StudentId);
                    ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", enrollment.ClassId);
                    ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", enrollment.SubjectId);
                    return View(enrollment);
                }

                _context.Add(enrollment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Enrollment created successfully!";
                return RedirectToAction(nameof(ManageEnrollments));
            }

            TempData["ErrorMessage"] = "Enrollment creation failed due to validation errors. Please check the form.";
            ViewData["StudentId"] = new SelectList(await _context.Students.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync(), "Id", "FullName", enrollment.StudentId);
            ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", enrollment.ClassId);
            ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", enrollment.SubjectId);
            return View(enrollment);
        }

        // GET: Admin/EditEnrollment/5
        public async Task<IActionResult> EditEnrollment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Edit Enrollment";
            ViewData["StudentId"] = new SelectList(await _context.Students.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync(), "Id", "FullName", enrollment.StudentId);
            ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", enrollment.ClassId);
            return View(enrollment);
        }

        // POST: Admin/EditEnrollment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEnrollment(int id, [Bind("Id,EnrollmentDate,StudentId,ClassId,Status")] Enrollment enrollment)
        {
            if (id != enrollment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Prevent duplicate enrollments for the same student in the same class (excluding current enrollment)
                if (await _context.Enrollments.AnyAsync(e => e.StudentId == enrollment.StudentId && e.ClassId == enrollment.ClassId && e.Id != enrollment.Id))
                {
                    ModelState.AddModelError(string.Empty, "This student is already enrolled in this class.");
                    TempData["ErrorMessage"] = "Enrollment update failed: Student is already enrolled in this class.";
                    ViewData["StudentId"] = new SelectList(await _context.Students.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync(), "Id", "FullName", enrollment.StudentId);
                    ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", enrollment.ClassId);
                    return View(enrollment);
                }

                try
                {
                    _context.Update(enrollment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Enrollment updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EnrollmentExists(enrollment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageEnrollments));
            }

            TempData["ErrorMessage"] = "Enrollment update failed due to validation errors. Please check the form.";
            ViewData["StudentId"] = new SelectList(await _context.Students.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync(), "Id", "FullName", enrollment.StudentId);
            ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", enrollment.ClassId);
            return View(enrollment);
        }

        // GET: Admin/DetailsEnrollment/5
        public async Task<IActionResult> DetailsEnrollment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (enrollment == null)
            {
                return NotFound();
            }
            ViewData["Title"] = "Enrollment Details";
            return View(enrollment);
        }

        // GET: Admin/DeleteEnrollment/5
        public async Task<IActionResult> DeleteEnrollment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (enrollment == null)
            {
                return NotFound();
            }
            ViewData["Title"] = "Delete Enrollment";
            return View(enrollment);
        }

        // POST: Admin/DeleteEnrollment/5
        [HttpPost, ActionName("DeleteEnrollment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEnrollmentConfirmed(int id)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment != null)
            {
                _context.Enrollments.Remove(enrollment);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Enrollment deleted successfully!";
            return RedirectToAction(nameof(ManageEnrollments));
        }

        // GET: Admin/ManageTeacherAssignments
        public async Task<IActionResult> ManageTeacherAssignments()
        {
            ViewData["Title"] = "Manage Teacher Assignments";
            var assignments = await _context.TeacherClassSubjects
                                            .Include(tcs => tcs.Teacher)
                                            .Include(tcs => tcs.Class)
                                            .Include(tcs => tcs.Subject)
                                            .ToListAsync();
            return View(assignments);
        }

        // GET: Admin/CreateTeacherAssignment
        public async Task<IActionResult> CreateTeacherAssignment()
        {
            ViewData["Title"] = "Create New Teacher Assignment";
            ViewData["TeacherId"] = new SelectList(await _context.Teachers.OrderBy(t => t.FirstName).ThenBy(t => t.LastName).ToListAsync(), "Id", "FullName");
            ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection");
            ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Admin/CreateTeacherAssignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacherAssignment([Bind("Id,TeacherId,ClassId,SubjectId,AssignmentDate")] TeacherClassSubject assignment)
        {
            if (ModelState.IsValid)
            {
                // Prevent duplicate assignments for the same teacher, class, and subject
                if (await _context.TeacherClassSubjects.AnyAsync(tcs => tcs.TeacherId == assignment.TeacherId && tcs.ClassId == assignment.ClassId && tcs.SubjectId == assignment.SubjectId))
                {
                    ModelState.AddModelError(string.Empty, "This teacher is already assigned to teach this subject in this class.");
                    TempData["ErrorMessage"] = "Assignment failed: Duplicate assignment found.";
                    ViewData["TeacherId"] = new SelectList(await _context.Teachers.OrderBy(t => t.FirstName).ThenBy(t => t.LastName).ToListAsync(), "Id", "FullName", assignment.TeacherId);
                    ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", assignment.ClassId);
                    ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", assignment.SubjectId);
                    return View(assignment);
                }

                _context.Add(assignment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Teacher assignment created successfully!";
                return RedirectToAction(nameof(ManageTeacherAssignments));
            }

            TempData["ErrorMessage"] = "Teacher assignment creation failed due to validation errors. Please check the form.";
            ViewData["TeacherId"] = new SelectList(await _context.Teachers.OrderBy(t => t.FirstName).ThenBy(t => t.LastName).ToListAsync(), "Id", "FullName", assignment.TeacherId);
            ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", assignment.ClassId);
            ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", assignment.SubjectId);
            return View(assignment);
        }

        // GET: Admin/EditTeacherAssignment/5
        public async Task<IActionResult> EditTeacherAssignment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignment = await _context.TeacherClassSubjects.FindAsync(id);
            if (assignment == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Edit Teacher Assignment";
            ViewData["TeacherId"] = new SelectList(await _context.Teachers.OrderBy(t => t.FirstName).ThenBy(t => t.LastName).ToListAsync(), "Id", "FullName", assignment.TeacherId);
            ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", assignment.ClassId);
            ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", assignment.SubjectId);
            return View(assignment);
        }

        // POST: Admin/EditTeacherAssignment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacherAssignment(int id, [Bind("Id,TeacherId,ClassId,SubjectId,AssignmentDate")] TeacherClassSubject assignment)
        {
            if (id != assignment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Prevent duplicate assignments (excluding current assignment being edited)
                if (await _context.TeacherClassSubjects.AnyAsync(tcs => tcs.TeacherId == assignment.TeacherId && tcs.ClassId == assignment.ClassId && tcs.SubjectId == assignment.SubjectId && tcs.Id != assignment.Id))
                {
                    ModelState.AddModelError(string.Empty, "Another assignment with this teacher, class, and subject already exists.");
                    TempData["ErrorMessage"] = "Assignment update failed: Duplicate assignment found.";
                    ViewData["TeacherId"] = new SelectList(await _context.Teachers.OrderBy(t => t.FirstName).ThenBy(t => t.LastName).ToListAsync(), "Id", "FullName", assignment.TeacherId);
                    ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", assignment.ClassId);
                    ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", assignment.SubjectId);
                    return View(assignment);
                }

                try
                {
                    _context.Update(assignment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Teacher assignment updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeacherClassSubjectExists(assignment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageTeacherAssignments));
            }

            TempData["ErrorMessage"] = "Teacher assignment update failed due to validation errors. Please check the form.";
            ViewData["TeacherId"] = new SelectList(await _context.Teachers.OrderBy(t => t.FirstName).ThenBy(t => t.LastName).ToListAsync(), "Id", "FullName", assignment.TeacherId);
            ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", assignment.ClassId);
            ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", assignment.SubjectId);
            return View(assignment);
        }

        // GET: Admin/DetailsTeacherAssignment/5
        public async Task<IActionResult> DetailsTeacherAssignment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignment = await _context.TeacherClassSubjects
                .Include(tcs => tcs.Teacher)
                .Include(tcs => tcs.Class)
                .Include(tcs => tcs.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (assignment == null)
            {
                return NotFound();
            }
            ViewData["Title"] = "Teacher Assignment Details";
            return View(assignment);
        }

        // GET: Admin/DeleteTeacherAssignment/5
        public async Task<IActionResult> DeleteTeacherAssignment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignment = await _context.TeacherClassSubjects
                .Include(tcs => tcs.Teacher)
                .Include(tcs => tcs.Class)
                .Include(tcs => tcs.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (assignment == null)
            {
                return NotFound();
            }
            ViewData["Title"] = "Delete Teacher Assignment";
            return View(assignment);
        }

        // POST: Admin/DeleteTeacherAssignment/5
        [HttpPost, ActionName("DeleteTeacherAssignment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeacherAssignmentConfirmed(int id)
        {
            var assignment = await _context.TeacherClassSubjects.FindAsync(id);
            if (assignment != null)
            {
                _context.TeacherClassSubjects.Remove(assignment);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Teacher assignment deleted successfully!";
            return RedirectToAction(nameof(ManageTeacherAssignments));
        }

        // GET: Admin/ManageAttendance
        public async Task<IActionResult> ManageAttendance()
        {
            ViewData["Title"] = "Manage Attendance";
            var attendances = await _context.Attendances
                                            .Include(a => a.Student)
                                            .Include(a => a.Class)
                                            .Include(a => a.Subject)
                                            .ToListAsync();
            return View(attendances);
        }

        // GET: Admin/CreateAttendance
        public async Task<IActionResult> CreateAttendance()
        {
            ViewData["Title"] = "Create New Attendance Record";
            ViewData["StudentId"] = new SelectList(await _context.Students.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync(), "Id", "FullName");
            ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection");
            ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Admin/CreateAttendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAttendance([Bind("Id,AttendanceDate,StudentId,ClassId,SubjectId,Status")] Attendance attendance)
        {
            if (ModelState.IsValid)
            {
                // Optional: Prevent duplicate attendance for the same student, class, subject, and date
                if (await _context.Attendances.AnyAsync(a => a.StudentId == attendance.StudentId && a.ClassId == attendance.ClassId && a.SubjectId == attendance.SubjectId && a.AttendanceDate.Date == attendance.AttendanceDate.Date))
                {
                    ModelState.AddModelError(string.Empty, "Attendance record for this student, class, subject, and date already exists.");
                    TempData["ErrorMessage"] = "Attendance creation failed: Duplicate record found.";
                    ViewData["StudentId"] = new SelectList(await _context.Students.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync(), "Id", "FullName", attendance.StudentId);
                    ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", attendance.ClassId);
                    ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", attendance.SubjectId);
                    return View(attendance);
                }

                _context.Add(attendance);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Attendance record created successfully!";
                return RedirectToAction(nameof(ManageAttendance));
            }

            TempData["ErrorMessage"] = "Attendance creation failed due to validation errors. Please check the form.";
            ViewData["StudentId"] = new SelectList(await _context.Students.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync(), "Id", "FullName", attendance.StudentId);
            ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", attendance.ClassId);
            ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", attendance.SubjectId);
            return View(attendance);
        }

        // GET: Admin/EditAttendance/5
        public async Task<IActionResult> EditAttendance(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Edit Attendance Record";
            ViewData["StudentId"] = new SelectList(await _context.Students.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync(), "Id", "FullName", attendance.StudentId);
            ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", attendance.ClassId);
            ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", attendance.SubjectId);
            return View(attendance);
        }

        // POST: Admin/EditAttendance/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAttendance(int id, [Bind("Id,AttendanceDate,StudentId,ClassId,SubjectId,Status")] Attendance attendance)
        {
            if (id != attendance.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Optional: Prevent duplicate attendance (excluding current record being edited)
                if (await _context.Attendances.AnyAsync(a => a.StudentId == attendance.StudentId && a.ClassId == attendance.ClassId && a.SubjectId == attendance.SubjectId && a.AttendanceDate.Date == attendance.AttendanceDate.Date && a.Id != attendance.Id))
                {
                    ModelState.AddModelError(string.Empty, "Another attendance record for this student, class, subject, and date already exists.");
                    TempData["ErrorMessage"] = "Attendance update failed: Duplicate record found.";
                    ViewData["StudentId"] = new SelectList(await _context.Students.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync(), "Id", "FullName", attendance.StudentId);
                    ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", attendance.ClassId);
                    ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", attendance.SubjectId);
                    return View(attendance);
                }

                try
                {
                    _context.Update(attendance);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Attendance record updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AttendanceExists(attendance.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ManageAttendance));
            }

            TempData["ErrorMessage"] = "Attendance update failed due to validation errors. Please check the form.";
            ViewData["StudentId"] = new SelectList(await _context.Students.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync(), "Id", "FullName", attendance.StudentId);
            ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(), "Id", "NameWithSection", attendance.ClassId);
            ViewData["SubjectId"] = new SelectList(await _context.Subjects.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", attendance.SubjectId);
            return View(attendance);
        }

        // GET: Admin/DetailsAttendance/5
        public async Task<IActionResult> DetailsAttendance(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Class)
                .Include(a => a.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (attendance == null)
            {
                return NotFound();
            }
            ViewData["Title"] = "Attendance Details";
            return View(attendance);
        }

        // GET: Admin/DeleteAttendance/5
        public async Task<IActionResult> DeleteAttendance(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Class)
                .Include(a => a.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (attendance == null)
            {
                return NotFound();
            }
            ViewData["Title"] = "Delete Attendance Record";
            return View(attendance);
        }

        // POST: Admin/DeleteAttendance/5
        [HttpPost, ActionName("DeleteAttendance")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttendanceConfirmed(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance != null)
            {
                _context.Attendances.Remove(attendance);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Attendance record deleted successfully!";
            return RedirectToAction(nameof(ManageAttendance));
        }


        // --- Parents Management ---

        // GET: Admin/ManageParents
        public async Task<IActionResult> ManageParents()
        {
            ViewData["Title"] = "Manage Parents";
            var parents = await _context.Parents
                                        .Include(p => p.ApplicationUser)
                                        .Include(p => p.Children)
                                        .ToListAsync();
            return View(parents);
        }

        // GET: Admin/AddParent
        public IActionResult AddParent()
        {
            ViewData["Title"] = "Add New Parent";
            return View();
        }

        // POST: Admin/AddParent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddParent(ParentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.NewUserPassword);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Parent");

                    var parent = new Parent
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        ApplicationUserId = user.Id
                    };
                    _context.Add(parent);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Parent and user account created successfully!";
                    return RedirectToAction(nameof(ManageParents));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: Admin/EditParent/5
        public async Task<IActionResult> EditParent(int? id)
        {
            if (id == null) return NotFound();

            var parent = await _context.Parents.Include(p => p.ApplicationUser).FirstOrDefaultAsync(p => p.Id == id);
            if (parent == null) return NotFound();

            var model = new ParentViewModel
            {
                Id = parent.Id,
                FirstName = parent.FirstName,
                LastName = parent.LastName,
                Email = parent.Email,
                PhoneNumber = parent.PhoneNumber,
                ApplicationUserId = parent.ApplicationUserId
            };
            ViewData["Title"] = $"Edit Parent: {parent.FullName}";
            return View(model);
        }

        // POST: Admin/EditParent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditParent(int id, ParentViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var parentToUpdate = await _context.Parents.Include(p => p.ApplicationUser).FirstOrDefaultAsync(p => p.Id == id);
                    if (parentToUpdate == null) return NotFound();

                    parentToUpdate.FirstName = model.FirstName;
                    parentToUpdate.LastName = model.LastName;
                    parentToUpdate.PhoneNumber = model.PhoneNumber;

                    if (parentToUpdate.ApplicationUser != null && parentToUpdate.ApplicationUser.Email != model.Email)
                    {
                        var user = await _userManager.FindByIdAsync(parentToUpdate.ApplicationUserId);
                        if (user != null)
                        {
                            user.Email = model.Email;
                            user.UserName = model.Email;
                            var updateResult = await _userManager.UpdateAsync(user);
                            if (!updateResult.Succeeded)
                            {
                                foreach (var error in updateResult.Errors)
                                {
                                    ModelState.AddModelError(string.Empty, error.Description);
                                }
                                return View(model);
                            }
                        }
                    }
                    parentToUpdate.Email = model.Email;


                    _context.Update(parentToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Parent updated successfully!";
                    return RedirectToAction(nameof(ManageParents));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ParentExists(model.Id)) return NotFound();
                    else throw;
                }
            }
            return View(model);
        }

        // POST: Admin/DeleteParent/5
        [HttpPost, ActionName("DeleteParent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteParentConfirmed(int id)
        {
            var parent = await _context.Parents.Include(p => p.ApplicationUser).FirstOrDefaultAsync(p => p.Id == id);
            if (parent == null) return NotFound();

            if (parent.ApplicationUser != null)
            {
                var result = await _userManager.DeleteAsync(parent.ApplicationUser);
                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = "Error deleting parent's user account.";
                    return RedirectToAction(nameof(ManageParents));
                }
            }

            _context.Parents.Remove(parent);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Parent deleted successfully!";
            return RedirectToAction(nameof(ManageParents));
        }

        private bool ParentExists(int id)
        {
            return _context.Parents.Any(e => e.Id == id);
        }

        // --- Link Student to Parent ---

        // GET: Admin/LinkStudentToParent
        public IActionResult LinkStudentToParent()
        {
            ViewData["Title"] = "Link Student to Parent";
            ViewData["Students"] = new SelectList(_context.Students, "Id", "FullName");
            ViewData["Parents"] = new SelectList(_context.Parents, "Id", "FullName");
            return View();
        }

        // POST: Admin/LinkStudentToParent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkStudentToParent(int studentId, int parentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            var parent = await _context.Parents.Include(p => p.Children).FirstOrDefaultAsync(p => p.Id == parentId);

            if (student == null || parent == null)
            {
                TempData["ErrorMessage"] = "Student or Parent not found.";
                return RedirectToAction(nameof(LinkStudentToParent));
            }

            if (parent.Children == null)
            {
                parent.Children = new List<Student>();
            }

            if (!parent.Children.Any(s => s.Id == studentId))
            {
                parent.Children.Add(student);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Student '{student.FullName}' linked to Parent '{parent.FullName}' successfully!";
            }
            else
            {
                TempData["WarningMessage"] = "Student is already linked to this parent.";
            }

            return RedirectToAction(nameof(ManageParents));
        }

        // GET: Admin/GetParentsForStudent/5 (for AJAX)
        [HttpGet]
        public async Task<IActionResult> GetParentsForStudent(int studentId)
        {
            var student = await _context.Students
                                        .Include(s => s.Parents) // Use the correct navigation property for many-to-many
                                        .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                return NotFound();
            }

            var parents = student.Parents?
                                 .Select(p => new { Id = p.Id, FullName = p.FullName })
                                 .ToList();

            return Json(parents);
        }

        // --- Manage Payments (Admin View) ---
        // GET: Admin/ManagePayments
        public async Task<IActionResult> ManagePayments()
        {
            ViewData["Title"] = "Manage Payments";
            var payments = await _context.Payments
                .Include(p => p.Invoice) // Include Invoice
                    .ThenInclude(i => i!.Student) // Then include Student
                .Include(p => p.Invoice) // Include Invoice again for Parent
                    .ThenInclude(i => i!.Parent) // Then include Parent
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new PaymentListViewModel
                {
                    Id = p.Id,
                    InvoiceId = p.InvoiceId ?? 0,
                    InvoiceNumber = p.Invoice != null ? p.Invoice.InvoiceNumber : "N/A",
                    // REVISED LINES BELOW
                    StudentName = p.Invoice == null || p.Invoice.Student == null
                                  ? "N/A"
                                  : $"{p.Invoice.Student.FirstName} {p.Invoice.Student.LastName}",
                    ParentName = p.Invoice == null || p.Invoice.Parent == null
                                 ? "N/A"
                                 : $"{p.Invoice.Parent.FirstName} {p.Invoice.Parent.LastName}",
                    // END REVISED LINES
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    Status = p.Status
                })
                .ToListAsync();

            return View(payments);
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePayment(int? invoiceId)
        {
            var model = new CreatePaymentViewModel();

            var invoices = await _context.Invoices
                .Where(i => i.Status != "Paid" && i.Status != "Waived")
                .Include(i => i.Student)
                .Include(i => i.Parent)
                .OrderBy(i => i.Status == "Overdue" ? 0 : 1)
                .ThenBy(i => i.DueDate)
                .Select(i => new
                {
                    i.Id,
                    DisplayText = $"INV-{i.InvoiceNumber} - {i.Student!.FirstName} {i.Student.LastName} (Due: {i.DueDate.ToShortDateString()}, Bal: {i.TotalAmount - i.AmountPaid:C})"
                })
                .ToListAsync();

            model.Invoices = new SelectList(invoices, "Id", "DisplayText");

            if (invoiceId.HasValue && invoices.Any(i => i.Id == invoiceId.Value))
            {
                model.InvoiceId = invoiceId.Value;
                // Pre-fill amount if invoiceId is provided
                var selectedInvoice = await _context.Invoices.FindAsync(invoiceId.Value);
                if (selectedInvoice != null)
                {
                    model.Amount = selectedInvoice.TotalAmount - selectedInvoice.AmountPaid;
                }
            }

            model.PaymentDate = DateTime.Today;
            model.PaymentMethod = "Cash";
            model.Status = "Completed";

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePayment(CreatePaymentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var invoice = await _context.Invoices.FindAsync(model.InvoiceId);
                if (invoice == null)
                {
                    ModelState.AddModelError("", "Selected Invoice not found.");
                }
                else if (model.Amount <= 0)
                {
                    ModelState.AddModelError("Amount", "Payment amount must be positive.");
                }
                else
                {
                    // Use the service method to record the payment
                    var success = await _invoiceService.RecordPayment(
                        model.InvoiceId,
                        model.Amount,
                        model.PaymentMethod,
                        model.PaymentDate,
                        model.Notes,
                        model.TransactionId,
                        model.Status
                    );

                    if (success)
                    {
                        TempData["SuccessMessage"] = "Payment recorded successfully!";
                        return RedirectToAction(nameof(ManagePayments));
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to record payment. Check invoice balance or other issues.");
                    }
                }
            }

            var invoices = await _context.Invoices
                .Where(i => i.Status != "Paid" && i.Status != "Waived")
                .Include(i => i.Student)
                .Include(i => i.Parent)
                .OrderBy(i => i.Status == "Overdue" ? 0 : 1)
                .ThenBy(i => i.DueDate)
                .Select(i => new
                {
                    i.Id,
                    DisplayText = $"INV-{i.InvoiceNumber} - {i.Student!.FirstName} {i.Student.LastName} (Due: {i.DueDate.ToShortDateString()}, Bal: {i.TotalAmount - i.AmountPaid:C})"
                })
                .ToListAsync();
            model.Invoices = new SelectList(invoices, "Id", "DisplayText");

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PaymentDetails(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i!.Student)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i!.Parent)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            var model = new PaymentDetailsViewModel
            {
                Id = payment.Id,
                InvoiceId = (int)payment.InvoiceId,
                InvoiceNumber = payment.Invoice != null ? payment.Invoice.InvoiceNumber : "N/A",
                StudentFullName = payment.Invoice != null && payment.Invoice.Student != null ? $"{payment.Invoice.Student.FirstName} {payment.Invoice.Student.LastName}" : "N/A",
                ParentFullName = payment.Invoice != null && payment.Invoice.Parent != null ? $"{payment.Invoice.Parent.FirstName} {payment.Invoice.Parent.LastName}" : "N/A",
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                TransactionId = payment.TransactionId,
                Notes = payment.Notes
            };

            return View(model);
        }


        // --- Fee Management ---

        // GET: Admin/ManageFeeTypes
        public async Task<IActionResult> ManageFeeTypes()
        {
            ViewData["Title"] = "Manage Fee Types";
            return View(await _context.FeeTypes.ToListAsync());
        }

        // GET: Admin/CreateFeeType
        public IActionResult CreateFeeType()
        {
            ViewData["Title"] = "Create Fee Type";
            return View();
        }

        // POST: Admin/CreateFeeType
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFeeType([Bind("Name,Description,DefaultAmount")] FeeType feeType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(feeType);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Fee Type created successfully.";
                return RedirectToAction(nameof(ManageFeeTypes));
            }
            return View(feeType);
        }

        // GET: Admin/EditFeeType/5
        public async Task<IActionResult> EditFeeType(int? id)
        {
            if (id == null) return NotFound();
            var feeType = await _context.FeeTypes.FindAsync(id);
            if (feeType == null) return NotFound();
            ViewData["Title"] = "Edit Fee Type";
            return View(feeType);
        }

        // POST: Admin/EditFeeType/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFeeType(int id, [Bind("Id,Name,Description,DefaultAmount")] FeeType feeType)
        {
            if (id != feeType.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(feeType);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Fee Type updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FeeTypeExists(feeType.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(ManageFeeTypes));
            }
            return View(feeType);
        }

        // POST: Admin/DeleteFeeType/5
        [HttpPost, ActionName("DeleteFeeType")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFeeTypeConfirmed(int id)
        {
            var feeType = await _context.FeeTypes.FindAsync(id);
            if (feeType != null)
            {
                _context.FeeTypes.Remove(feeType);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Fee Type deleted successfully.";
            }
            return RedirectToAction(nameof(ManageFeeTypes));
        }

        private bool FeeTypeExists(int id)
        {
            return _context.FeeTypes.Any(e => e.Id == id);
        }


        // GET: Admin/ManageClassFees
        public async Task<IActionResult> ManageClassFees()
        {
            ViewData["Title"] = "Manage Class Fees";
            var classFees = await _context.ClassFees
                                          .Include(cf => cf.Class)
                                          .Include(cf => cf.FeeType)
                                          .ToListAsync();
            return View(classFees);
        }

        // GET: Admin/CreateClassFee
        public IActionResult CreateClassFee()
        {
            ViewData["Title"] = "Create Class Fee";
            ViewData["ClassId"] = new SelectList(_context.Classes, "Id", "NameWithSection");
            ViewData["FeeTypeId"] = new SelectList(_context.FeeTypes, "Id", "Name");
            return View();
        }

        // POST: Admin/CreateClassFee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClassFee([Bind("ClassId,FeeTypeId,Amount")] ClassFee classFee)
        {
            if (ModelState.IsValid)
            {
                _context.Add(classFee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Class Fee created successfully.";
                return RedirectToAction(nameof(ManageClassFees));
            }
            ViewData["ClassId"] = new SelectList(_context.Classes, "Id", "NameWithSection", classFee.ClassId);
            ViewData["FeeTypeId"] = new SelectList(_context.FeeTypes, "Id", "Name", classFee.FeeTypeId);
            return View(classFee);
        }

        // GET: Admin/EditClassFee/5
        public async Task<IActionResult> EditClassFee(int? classId, int? feeTypeId)
        {
            if (classId == null || feeTypeId == null) return NotFound();

            var classFee = await _context.ClassFees
                                         .FirstOrDefaultAsync(cf => cf.ClassId == classId && cf.FeeTypeId == feeTypeId);
            if (classFee == null) return NotFound();

            ViewData["Title"] = "Edit Class Fee";
            ViewData["ClassId"] = new SelectList(_context.Classes, "Id", "NameWithSection", classFee.ClassId);
            ViewData["FeeTypeId"] = new SelectList(_context.FeeTypes, "Id", "Name", classFee.FeeTypeId);
            return View(classFee);
        }

        // POST: Admin/EditClassFee/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClassFee(int classId, int feeTypeId, [Bind("ClassId,FeeTypeId,Amount")] ClassFee classFee)
        {
            if (classId != classFee.ClassId || feeTypeId != classFee.FeeTypeId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(classFee);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Class Fee updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassFeeExists(classFee.ClassId, classFee.FeeTypeId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(ManageClassFees));
            }
            ViewData["ClassId"] = new SelectList(_context.Classes, "Id", "NameWithSection", classFee.ClassId);
            ViewData["FeeTypeId"] = new SelectList(_context.FeeTypes, "Id", "Name", classFee.FeeTypeId);
            return View(classFee);
        }

        // POST: Admin/DeleteClassFee/5
        [HttpPost, ActionName("DeleteClassFee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClassFeeConfirmed(int classId, int feeTypeId)
        {
            var classFee = await _context.ClassFees
                                         .FirstOrDefaultAsync(cf => cf.ClassId == classId && cf.FeeTypeId == feeTypeId);
            if (classFee != null)
            {
                _context.ClassFees.Remove(classFee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Class Fee deleted successfully.";
            }
            return RedirectToAction(nameof(ManageClassFees));
        }

        private bool ClassFeeExists(int classId, int feeTypeId)
        {
            return _context.ClassFees.Any(e => e.ClassId == classId && e.FeeTypeId == feeTypeId);
        }

        // GET: Admin/ManageStudentFees
        public async Task<IActionResult> ManageStudentFees()
        {
            ViewData["Title"] = "Manage Student Fees";
            var studentFees = await _context.StudentFees
                                            .Include(sf => sf.Student)
                                            .Include(sf => sf.FeeType)
                                            .ToListAsync();
            return View(studentFees);
        }

        // GET: Admin/CreateStudentFee
        public IActionResult CreateStudentFee()
        {
            ViewData["Title"] = "Create Student Fee";
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "FullName");
            ViewData["FeeTypeId"] = new SelectList(_context.FeeTypes, "Id", "Name");
            return View();
        }

        // POST: Admin/CreateStudentFee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudentFee([Bind("StudentId,FeeTypeId,Amount")] StudentFee studentFee)
        {
            if (ModelState.IsValid)
            {
                _context.Add(studentFee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Student Fee created successfully.";
                return RedirectToAction(nameof(ManageStudentFees));
            }
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "FullName", studentFee.StudentId);
            ViewData["FeeTypeId"] = new SelectList(_context.FeeTypes, "Id", "Name", studentFee.FeeTypeId);
            return View(studentFee);
        }

        // GET: Admin/EditStudentFee/5
        public async Task<IActionResult> EditStudentFee(int? studentId, int? feeTypeId)
        {
            if (studentId == null || feeTypeId == null) return NotFound();

            var studentFee = await _context.StudentFees
                                           .FirstOrDefaultAsync(sf => sf.StudentId == studentId && sf.FeeTypeId == feeTypeId);
            if (studentFee == null) return NotFound();

            ViewData["Title"] = "Edit Student Fee";
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "FullName", studentFee.StudentId);
            ViewData["FeeTypeId"] = new SelectList(_context.FeeTypes, "Id", "Name", studentFee.FeeTypeId);
            return View(studentFee);
        }

        // POST: Admin/EditStudentFee/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudentFee(int studentId, int feeTypeId, [Bind("StudentId,FeeTypeId,Amount")] StudentFee studentFee)
        {
            if (studentId != studentFee.StudentId || feeTypeId != studentFee.FeeTypeId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(studentFee);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Student Fee updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentFeeExists(studentFee.StudentId, studentFee.FeeTypeId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(ManageStudentFees));
            }
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "FullName", studentFee.StudentId);
            ViewData["FeeTypeId"] = new SelectList(_context.FeeTypes, "Id", "Name", studentFee.FeeTypeId);
            return View(studentFee);
        }

        // POST: Admin/DeleteStudentFee/5
        [HttpPost, ActionName("DeleteStudentFee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudentFeeConfirmed(int studentId, int feeTypeId)
        {
            var studentFee = await _context.StudentFees
                                           .FirstOrDefaultAsync(sf => sf.StudentId == studentId && sf.FeeTypeId == feeTypeId);
            if (studentFee != null)
            {
                _context.StudentFees.Remove(studentFee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Student Fee deleted successfully.";
            }
            return RedirectToAction(nameof(ManageStudentFees));
        }

        private bool StudentFeeExists(int studentId, int feeTypeId)
        {
            return _context.StudentFees.Any(e => e.StudentId == studentId && e.FeeTypeId == feeTypeId);
        }

        // --- Invoice Management ---

        // GET: Admin/ManageInvoices
        public async Task<IActionResult> ManageInvoices()
        {
            ViewData["Title"] = "Manage Invoices";
            var invoices = await _context.Invoices
                                         .Include(i => i.Student)
                                         .Include(i => i.Parent)
                                         .Include(i => i.Payments)
                                         .OrderByDescending(i => i.IssueDate)
                                         .ToListAsync();

            var viewModel = invoices.Select(i => {
                decimal amountPaid = i.Payments?.Sum(p => p.Amount) ?? 0m;
                decimal balanceDue = i.TotalAmount - amountPaid;
                string status;

                if (balanceDue <= 0)
                {
                    status = "Paid";
                }
                else if (amountPaid > 0 && balanceDue > 0)
                {
                    status = "Partially Paid";
                }
                else if (balanceDue > 0 && i.DueDate < DateTime.Today)
                {
                    status = "Overdue";
                }
                else
                {
                    status = "Outstanding";
                }

                return new InvoiceListViewModel
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    IssueDate = i.IssueDate,
                    DueDate = i.DueDate,
                    StudentName = i.Student?.FullName,
                    ParentName = i.Parent?.FullName,
                    TotalAmount = i.TotalAmount,
                    AmountPaid = amountPaid,
                    BalanceDue = balanceDue,
                    Status = status
                };
            }).ToList();

            return View(viewModel);
        }

        // GET: Admin/CreateInvoice
        public async Task<IActionResult> CreateInvoice()
        {
            ViewData["Title"] = "Create New Invoice";
            var model = new CreateInvoiceViewModel
            {
                IssueDate = DateTime.Today,
                DueDate = DateTime.Today.AddMonths(1),
                InvoiceItems = new List<InvoiceItemViewModel> { new InvoiceItemViewModel { Amount = 0 } }
            };

            // Generate a simple invoice number (e.g., INV-YYYYMMDD-XXXX)
            model.InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{(_context.Invoices.Count() + 1).ToString("D4")}";

            await PopulateCreateEditInvoiceDropdowns(model); // Use the new helper

            return View(model);
        }

        // POST: Admin/CreateInvoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInvoice(CreateInvoiceViewModel model)
        {
            ViewData["Title"] = "Create New Invoice";

            model.TotalAmount = model.InvoiceItems.Sum(item => item.Amount); // Recalculate total

            // Custom validation: Ensure ParentId is provided if Student has a linked Parent
            if (model.StudentId != 0)
            {
                var student = await _context.Students.Include(s => s.Parents).FirstOrDefaultAsync(s => s.Id == model.StudentId);
                if (student != null && student.Parents != null && student.Parents.Any())
                {
                    if (!model.ParentId.HasValue || !student.Parents.Any(p => p.Id == model.ParentId.Value))
                    {
                        ModelState.AddModelError(nameof(model.ParentId), "Please select a valid parent responsible for this student's invoice.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                var invoice = new Invoice
                {
                    InvoiceNumber = model.InvoiceNumber,
                    IssueDate = model.IssueDate,
                    DueDate = model.DueDate,
                    StudentId = model.StudentId,
                    ParentId = model.ParentId ?? 0,
                    TotalAmount = model.TotalAmount,
                    AmountPaid = model.AmountPaid,
                    Status = model.Status,
                    Notes = model.Notes
                };

                foreach (var itemModel in model.InvoiceItems)
                {
                    invoice.InvoiceItems!.Add(new InvoiceItem
                    {
                        FeeTypeId = itemModel.FeeTypeId,
                        Description = itemModel.Description,
                        Amount = itemModel.Amount
                    });
                }

                _context.Add(invoice);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Invoice {invoice.InvoiceNumber} created successfully.";
                return RedirectToAction(nameof(ManageInvoices));
            }

            await PopulateCreateEditInvoiceDropdowns(model); // Re-populate dropdowns on error
            TempData["ErrorMessage"] = "Invoice creation failed due to validation errors.";
            return View(model);
        }

        // GET: Admin/DetailsInvoice/5
        public async Task<IActionResult> DetailsInvoice(int? id)
        {
            ViewData["Title"] = "Invoice Details";
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                                        .Include(i => i.Student)
                                        .Include(i => i.Parent)
                                        .Include(i => i.InvoiceItems!)
                                            .ThenInclude(ii => ii.FeeType)
                                        .Include(i => i.Payments)
                                            .ThenInclude(p => p.Student)
                                        .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null) return NotFound();

            var viewModel = new InvoiceDetailsViewModel
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                StudentId = invoice.StudentId,
                StudentFullName = invoice.Student?.FullName ?? "N/A",
                ParentId = (int)invoice.ParentId,
                ParentFullName = invoice.Parent?.FullName ?? "N/A",
                TotalAmount = invoice.TotalAmount,
                AmountPaid = invoice.Payments?.Sum(p => p.Amount) ?? 0m,
                Status = invoice.Status,
                Notes = invoice.Notes,
                InvoiceItems = invoice.InvoiceItems!.Select(ii => new InvoiceItemViewModel
                {
                    Id = ii.Id,
                    FeeTypeId = ii.FeeTypeId,
                    FeeTypeName = ii.FeeType?.Name,
                    Description = ii.Description,
                    Amount = ii.Amount
                }).ToList(),
                Payments = invoice.Payments!.ToList()
            };

            // Dynamically update status for display
            if (viewModel.BalanceDue <= 0)
            {
                viewModel.Status = "Paid";
            }
            else if (viewModel.AmountPaid > 0 && viewModel.BalanceDue > 0)
            {
                viewModel.Status = "Partially Paid";
            }
            else if (viewModel.BalanceDue > 0 && viewModel.DueDate < DateTime.Today)
            {
                viewModel.Status = "Overdue";
            }
            // else, keep as whatever came from DB (Outstanding)

            return View(viewModel);
        }

        // GET: Admin/EditInvoice/5
        public async Task<IActionResult> EditInvoice(int? id)
        {
            ViewData["Title"] = "Edit Invoice";
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                                        .Include(i => i.InvoiceItems!)
                                            .ThenInclude(ii => ii.FeeType)
                                        .FirstOrDefaultAsync(m => m.Id == id);
            if (invoice == null) return NotFound();

            var model = new CreateInvoiceViewModel // Use CreateInvoiceViewModel for editing
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                StudentId = invoice.StudentId,
                ParentId = invoice.ParentId,
                TotalAmount = invoice.TotalAmount,
                AmountPaid = invoice.AmountPaid,
                Status = invoice.Status,
                Notes = invoice.Notes,
                InvoiceItems = invoice.InvoiceItems!.Select(ii => new InvoiceItemViewModel
                {
                    Id = ii.Id,
                    FeeTypeId = ii.FeeTypeId,
                    Description = ii.Description,
                    Amount = ii.Amount
                }).ToList()
            };

            await PopulateCreateEditInvoiceDropdowns(model); // Use the new helper

            return View(model);
        }

        // POST: Admin/EditInvoice/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInvoice(int id, CreateInvoiceViewModel model) // Use CreateInvoiceViewModel for editing
        {
            ViewData["Title"] = "Edit Invoice";
            if (id != model.Id) return NotFound();

            model.TotalAmount = model.InvoiceItems.Sum(item => item.Amount); // Recalculate total

            // Custom validation: Ensure ParentId is provided if Student has a linked Parent
            if (model.StudentId != 0)
            {
                var student = await _context.Students.Include(s => s.Parents).FirstOrDefaultAsync(s => s.Id == model.StudentId);
                if (student != null && student.Parents != null && student.Parents.Any())
                {
                    if (!model.ParentId.HasValue || !student.Parents.Any(p => p.Id == model.ParentId.Value))
                    {
                        ModelState.AddModelError(nameof(model.ParentId), "Please select a valid parent responsible for this student's invoice.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var invoiceToUpdate = await _context.Invoices
                                                        .Include(i => i.InvoiceItems)
                                                        .Include(i => i.Payments)
                                                        .FirstOrDefaultAsync(i => i.Id == id);
                    if (invoiceToUpdate == null) return NotFound();

                    invoiceToUpdate.InvoiceNumber = model.InvoiceNumber;
                    invoiceToUpdate.IssueDate = model.IssueDate;
                    invoiceToUpdate.DueDate = model.DueDate;
                    invoiceToUpdate.StudentId = model.StudentId;
                    invoiceToUpdate.ParentId = model.ParentId ?? 0;
                    invoiceToUpdate.TotalAmount = model.TotalAmount;
                    invoiceToUpdate.AmountPaid = model.AmountPaid;
                    invoiceToUpdate.Notes = model.Notes;

                    // Update Invoice Items
                    var existingInvoiceItems = invoiceToUpdate.InvoiceItems!.ToList(); // Convert to List for RemoveAll

                    // Remove items not in the new list
                    existingInvoiceItems.RemoveAll(item => !model.InvoiceItems.Any(m => m.Id == item.Id));

                    foreach (var itemModel in model.InvoiceItems)
                    {
                        var existingItem = existingInvoiceItems.FirstOrDefault(ii => ii.Id == itemModel.Id);
                        if (existingItem == null)
                        {
                            // Add new item
                            existingInvoiceItems.Add(new InvoiceItem
                            {
                                FeeTypeId = itemModel.FeeTypeId,
                                Description = itemModel.Description,
                                Amount = itemModel.Amount
                            });
                        }
                        else
                        {
                            // Update existing item
                            existingItem.FeeTypeId = itemModel.FeeTypeId;
                            existingItem.Description = itemModel.Description;
                            existingItem.Amount = itemModel.Amount;
                        }
                    }
                    invoiceToUpdate.InvoiceItems = existingInvoiceItems; // Assign back to navigation property

                    // Recalculate Status based on new total and existing payments
                    decimal currentAmountPaid = invoiceToUpdate.Payments?.Sum(p => p.Amount) ?? 0m;
                    decimal currentBalanceDue = invoiceToUpdate.TotalAmount - currentAmountPaid;

                    if (currentBalanceDue <= 0)
                    {
                        invoiceToUpdate.Status = "Paid";
                    }
                    else if (currentAmountPaid > 0 && currentBalanceDue > 0)
                    {
                        invoiceToUpdate.Status = "Partially Paid";
                    }
                    else if (currentBalanceDue > 0 && invoiceToUpdate.DueDate < DateTime.Today)
                    {
                        invoiceToUpdate.Status = "Overdue";
                    }
                    else
                    {
                        invoiceToUpdate.Status = "Outstanding";
                    }

                    _context.Update(invoiceToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Invoice {invoiceToUpdate.InvoiceNumber} updated successfully.";
                    return RedirectToAction(nameof(ManageInvoices));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InvoiceExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await PopulateCreateEditInvoiceDropdowns(model); // Re-populate dropdowns on error
            TempData["ErrorMessage"] = "Invoice update failed due to validation errors.";
            return View(model);
        }

        // GET: Admin/DeleteInvoice/5
        public async Task<IActionResult> DeleteInvoice(int? id)
        {
            ViewData["Title"] = "Delete Invoice";
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                                        .Include(i => i.Student)
                                        .Include(i => i.Parent)
                                        .Include(i => i.Payments) // Include payments
                                        .FirstOrDefaultAsync(m => m.Id == id);
            if (invoice == null) return NotFound();

            // Calculate current status for display on delete confirmation
            decimal amountPaid = invoice.Payments?.Sum(p => p.Amount) ?? 0m;
            decimal balanceDue = invoice.TotalAmount - amountPaid;
            string status;

            if (balanceDue <= 0) status = "Paid";
            else if (amountPaid > 0 && balanceDue > 0) status = "Partially Paid";
            else if (balanceDue > 0 && invoice.DueDate < DateTime.Today) status = "Overdue";
            else status = "Outstanding";

            var viewModel = new InvoiceListViewModel // Using InvoiceListViewModel for display
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                StudentName = invoice.Student?.FullName,
                ParentName = invoice.Parent?.FullName,
                TotalAmount = invoice.TotalAmount,
                AmountPaid = amountPaid,
                BalanceDue = balanceDue,
                Status = status
            };
            return View(viewModel);
        }

        // POST: Admin/DeleteInvoice/5
        [HttpPost, ActionName("DeleteInvoice")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInvoiceConfirmed(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id); // FindAsync doesn't include related data
            if (invoice != null)
            {
                // Ensure InvoiceItems and Payments are loaded before removal if cascade delete is not configured
                // or if you want to manually manage. Given previous cascade issues, manual is safer.
                await _context.Entry(invoice).Collection(i => i.InvoiceItems!).LoadAsync();
                await _context.Entry(invoice).Collection(i => i.Payments!).LoadAsync();

                if (invoice.InvoiceItems != null) _context.InvoiceItems.RemoveRange(invoice.InvoiceItems);
                if (invoice.Payments != null) _context.Payments.RemoveRange(invoice.Payments);

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Invoice deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Invoice not found for deletion.";
            }
            return RedirectToAction(nameof(ManageInvoices));
        }

        private bool InvoiceExists(int id)
        {
            return _context.Invoices.Any(e => e.Id == id);
        }

        // Helper method to populate dropdowns for Create and Edit Invoice ViewModels
        private async Task PopulateCreateEditInvoiceDropdowns(CreateInvoiceViewModel model)
        {
            model.Students = new SelectList(await _context.Students.ToListAsync(), "Id", "FullName", model.StudentId);
            model.Parents = new SelectList(await _context.Parents.ToListAsync(), "Id", "FullName", model.ParentId);

            var feeTypes = await _context.FeeTypes.ToListAsync();
            // Populate FeeTypes for each InvoiceItemViewModel
            foreach (var item in model.InvoiceItems)
            {
                item.FeeTypes = new SelectList(feeTypes, "Id", "Name", item.FeeTypeId);
            }
            // Also add FeeTypes to ViewBag for dynamic JS additions in the view
            ViewBag.AllFeeTypesForJs = new SelectList(feeTypes, "Id", "Name");
        }

        #region Notice Management

        // GET: Admin/ManageNotices
        public async Task<IActionResult> ManageNotices()
        {
            ViewData["Title"] = "Manage Notices";
            var notices = await _context.Notices.OrderByDescending(n => n.PublishDate).ToListAsync();
            return View(notices);
        }

        // GET: Admin/CreateNotice
        public IActionResult CreateNotice()
        {
            ViewData["Title"] = "Create New Notice";
            return View(new NoticeViewModel());
        }

        // POST: Admin/CreateNotice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNotice(NoticeViewModel model)
        {
            if (ModelState.IsValid)
            {
                var notice = new Notice
                {
                    Title = model.Title,
                    Content = model.Content,
                    PublishDate = model.PublishDate,
                    ExpiryDate = model.ExpiryDate,
                    IsActive = model.IsActive
                };
                _context.Add(notice);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Notice created successfully!";
                return RedirectToAction(nameof(ManageNotices));
            }
            ViewData["Title"] = "Create New Notice";
            return View(model);
        }

        // GET: Admin/EditNotice/5
        public async Task<IActionResult> EditNotice(int? id)
        {
            if (id == null) return NotFound();
            var notice = await _context.Notices.FindAsync(id);
            if (notice == null) return NotFound();

            var model = new NoticeViewModel
            {
                Id = notice.Id,
                Title = notice.Title,
                Content = notice.Content,
                PublishDate = notice.PublishDate,
                ExpiryDate = notice.ExpiryDate,
                IsActive = notice.IsActive
            };
            ViewData["Title"] = $"Edit Notice: {notice.Title}";
            return View(model);
        }

        // POST: Admin/EditNotice/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNotice(int id, NoticeViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var noticeToUpdate = await _context.Notices.FindAsync(id);
                    if (noticeToUpdate == null) return NotFound();

                    noticeToUpdate.Title = model.Title;
                    noticeToUpdate.Content = model.Content;
                    noticeToUpdate.PublishDate = model.PublishDate;
                    noticeToUpdate.ExpiryDate = model.ExpiryDate;
                    noticeToUpdate.IsActive = model.IsActive;

                    _context.Update(noticeToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Notice updated successfully!";
                    return RedirectToAction(nameof(ManageNotices));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NoticeExists(model.Id)) return NotFound();
                    else throw;
                }
            }
            ViewData["Title"] = $"Edit Notice: {model.Title}";
            return View(model);
        }

        // GET: Admin/DeleteNotice/5
        public async Task<IActionResult> DeleteNotice(int? id)
        {
            if (id == null) return NotFound();
            var notice = await _context.Notices.FirstOrDefaultAsync(m => m.Id == id);
            if (notice == null) return NotFound();
            ViewData["Title"] = $"Delete Notice: {notice.Title}";
            return View(notice); // Pass the model directly for confirmation
        }

        // POST: Admin/DeleteNotice/5
        [HttpPost, ActionName("DeleteNotice")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNoticeConfirmed(int id)
        {
            var notice = await _context.Notices.FindAsync(id);
            if (notice != null)
            {
                _context.Notices.Remove(notice);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Notice deleted successfully!";
            }
            return RedirectToAction(nameof(ManageNotices));
        }

        private bool NoticeExists(int id)
        {
            return _context.Notices.Any(e => e.Id == id);
        }

        #endregion

        #region Holiday Management

        // GET: Admin/ManageHolidays
        public async Task<IActionResult> ManageHolidays()
        {
            ViewData["Title"] = "Manage Holidays";
            var holidays = await _context.Holidays.OrderBy(h => h.HolidayDate).ToListAsync();
            return View(holidays);
        }

        // GET: Admin/CreateHoliday
        public IActionResult CreateHoliday()
        {
            ViewData["Title"] = "Create New Holiday";
            return View(new HolidayViewModel { HolidayDate = DateTime.Today });
        }

        // POST: Admin/CreateHoliday
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHoliday(HolidayViewModel model)
        {
            if (ModelState.IsValid)
            {
                var holiday = new Holiday
                {
                    Title = model.Title,
                    HolidayDate = model.HolidayDate
                };
                _context.Add(holiday);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Holiday created successfully!";
                return RedirectToAction(nameof(ManageHolidays));
            }
            ViewData["Title"] = "Create New Holiday";
            return View(model);
        }

        // GET: Admin/EditHoliday/5
        public async Task<IActionResult> EditHoliday(int? id)
        {
            if (id == null) return NotFound();
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound();

            var model = new HolidayViewModel
            {
                Id = holiday.Id,
                Title = holiday.Title,
                HolidayDate = holiday.HolidayDate
            };
            ViewData["Title"] = $"Edit Holiday: {holiday.Title}";
            return View(model);
        }

        // POST: Admin/EditHoliday/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHoliday(int id, HolidayViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var holidayToUpdate = await _context.Holidays.FindAsync(id);
                    if (holidayToUpdate == null) return NotFound();

                    holidayToUpdate.Title = model.Title;
                    holidayToUpdate.HolidayDate = model.HolidayDate;

                    _context.Update(holidayToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Holiday updated successfully!";
                    return RedirectToAction(nameof(ManageHolidays));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HolidayExists(model.Id)) return NotFound();
                    else throw;
                }
            }
            ViewData["Title"] = $"Edit Holiday: {model.Title}";
            return View(model);
        }

        // GET: Admin/DeleteHoliday/5
        public async Task<IActionResult> DeleteHoliday(int? id)
        {
            if (id == null) return NotFound();
            var holiday = await _context.Holidays.FirstOrDefaultAsync(m => m.Id == id);
            if (holiday == null) return NotFound();
            ViewData["Title"] = $"Delete Holiday: {holiday.Title}";
            return View(holiday); // Pass the model directly for confirmation
        }

        // POST: Admin/DeleteHoliday/5
        [HttpPost, ActionName("DeleteHoliday")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHolidayConfirmed(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday != null)
            {
                _context.Holidays.Remove(holiday);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Holiday deleted successfully!";
            }
            return RedirectToAction(nameof(ManageHolidays));
        }

        private bool HolidayExists(int id)
        {
            return _context.Holidays.Any(e => e.Id == id);
        }
        #endregion

        private bool StudentFeeExists(int id)
        {
            return _context.StudentFees.Any(e => e.Id == id);
        }

        private bool AttendanceExists(int id)
        {
            return _context.Attendances.Any(e => e.Id == id);
        }

        private bool TeacherClassSubjectExists(int id)
        {
            return _context.TeacherClassSubjects.Any(e => e.Id == id);
        }


        private bool EnrollmentExists(int id)
        {
            return _context.Enrollments.Any(e => e.Id == id);
        }

        private bool ClassExists(int id)
        {
            return _context.Classes.Any(e => e.Id == id);
        }

        private bool SubjectExists(int id)
        {
            return _context.Subjects.Any(e => e.Id == id);
        }
        // Helper method to check if a Teacher exists
        private bool TeacherExists(int id)
        {
            return _context.Teachers.Any(e => e.Id == id);
        }

        // Helper method to check if a Student exists
        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}