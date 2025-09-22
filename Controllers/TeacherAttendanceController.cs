using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementApp.Data;
using StudentManagementApp.Models;
using StudentManagementApp.ViewModels;
using System.Linq; // For LINQ operations like Distinct()

namespace StudentManagementApp.Controllers
{
    // Only teachers can access this controller
    [Authorize(Roles = "Teacher")]
    public class TeacherAttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherAttendanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: TeacherAttendance (Landing page for selecting class/subject/date)
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Mark Attendance";
            var teacherId = _userManager.GetUserId(User);
            var teacherRecord = await _context.Teachers.FirstOrDefaultAsync(t => t.ApplicationUserId == teacherId);

            if (teacherRecord == null)
            {
                TempData["ErrorMessage"] = "Your teacher profile is not linked. Please contact an administrator.";
                return RedirectToAction("Index", "Home"); // Redirect if teacher record not found
            }

            // Get classes assigned to this teacher
            var assignedClasses = await _context.TeacherClassSubjects
                                                .Where(tcs => tcs.TeacherId == teacherRecord.Id)
                                                .Select(tcs => tcs.Class)
                                                .Distinct()
                                                .OrderBy(c => c!.Name)
                                                .ThenBy(c => c!.Section)
                                                .ToListAsync();

            // Get subjects assigned to this teacher (across all classes)
            var assignedSubjects = await _context.TeacherClassSubjects
                                                 .Where(tcs => tcs.TeacherId == teacherRecord.Id)
                                                 .Select(tcs => tcs.Subject)
                                                 .Distinct()
                                                 .OrderBy(s => s!.Name)
                                                 .ToListAsync();

            var viewModel = new MarkAttendanceViewModel
            {
                AttendanceDate = DateTime.Today,
                AssignedClasses = new SelectList(assignedClasses, "Id", "NameWithSection"),
                AssignedSubjects = new SelectList(assignedSubjects, "Id", "Name")
            };

            return View(viewModel);
        }

        // POST: TeacherAttendance/MarkAttendance (Process selected class/subject/date and show students)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAttendance(MarkAttendanceViewModel viewModel)
        {
            ViewData["Title"] = "Mark Attendance";
            var teacherId = _userManager.GetUserId(User);
            var teacherRecord = await _context.Teachers.FirstOrDefaultAsync(t => t.ApplicationUserId == teacherId);

            if (teacherRecord == null)
            {
                TempData["ErrorMessage"] = "Your teacher profile is not linked. Please contact an administrator.";
                return RedirectToAction("Index", "Home");
            }

            // Repopulate dropdowns in case of validation error or initial load
            var assignedClasses = await _context.TeacherClassSubjects
                                                .Where(tcs => tcs.TeacherId == teacherRecord.Id)
                                                .Select(tcs => tcs.Class)
                                                .Distinct()
                                                .OrderBy(c => c!.Name)
                                                .ThenBy(c => c!.Section)
                                                .ToListAsync();

            var assignedSubjects = await _context.TeacherClassSubjects
                                                 .Where(tcs => tcs.TeacherId == teacherRecord.Id)
                                                 .Select(tcs => tcs.Subject)
                                                 .Distinct()
                                                 .OrderBy(s => s!.Name)
                                                 .ToListAsync();

            viewModel.AssignedClasses = new SelectList(assignedClasses, "Id", "NameWithSection", viewModel.ClassId);
            viewModel.AssignedSubjects = new SelectList(assignedSubjects, "Id", "Name", viewModel.SubjectId);


            if (!ModelState.IsValid)
            {
                return View("Index", viewModel); // Return to index view with validation errors
            }

            // Verify the teacher is actually assigned to this class and subject combination
            var isAssigned = await _context.TeacherClassSubjects
                                           .AnyAsync(tcs => tcs.TeacherId == teacherRecord.Id &&
                                                            tcs.ClassId == viewModel.ClassId &&
                                                            tcs.SubjectId == viewModel.SubjectId);
            if (!isAssigned)
            {
                TempData["ErrorMessage"] = "You are not authorized to mark attendance for this class and subject combination.";
                return RedirectToAction(nameof(Index));
            }

            // Get all students enrolled in the selected class
            var enrolledStudents = await _context.Enrollments
                                                 .Where(e => e.ClassId == viewModel.ClassId && e.Status == "Active")
                                                 .Select(e => e.Student!) // Ensure Student is not null
                                                 .OrderBy(s => s.FirstName)
                                                 .ThenBy(s => s.LastName)
                                                 .ToListAsync();

            // Get existing attendance records for this class, subject, and date
            var existingAttendance = await _context.Attendances
                                                   .Where(a => a.ClassId == viewModel.ClassId &&
                                                               a.SubjectId == viewModel.SubjectId &&
                                                               a.AttendanceDate.Date == viewModel.AttendanceDate.Date)
                                                   .ToDictionaryAsync(a => a.StudentId, a => a.Status);

            viewModel.Students = enrolledStudents.Select(s => new StudentAttendanceViewModel
            {
                StudentId = s.Id,
                StudentName = s.FullName,
                Status = existingAttendance.GetValueOrDefault(s.Id, "Absent"), // Default to Absent if no record
                IsEnrolledInClass = true // All these students are enrolled in the selected class
            }).ToList();

            return View(viewModel); // Pass the populated ViewModel to the MarkAttendance view
        }

        // POST: TeacherAttendance/SaveAttendance (Save the attendance records)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAttendance(MarkAttendanceViewModel viewModel)
        {
            // Declare teacherId and teacherRecord once at the top of the method
            var teacherId = _userManager.GetUserId(User);
            var teacherRecord = await _context.Teachers.FirstOrDefaultAsync(t => t.ApplicationUserId == teacherId);

            if (!ModelState.IsValid)
            {
                // If validation fails, return to the marking view with errors
                TempData["ErrorMessage"] = "Failed to save attendance due to validation errors.";
                // Re-populate dropdowns and student list before returning
                // Use the already declared teacherRecord
                var assignedClasses = await _context.TeacherClassSubjects
                                                    .Where(tcs => tcs.TeacherId == teacherRecord!.Id)
                                                    .Select(tcs => tcs.Class)
                                                    .Distinct()
                                                    .OrderBy(c => c!.Name)
                                                    .ThenBy(c => c!.Section)
                                                    .ToListAsync();
                var assignedSubjects = await _context.TeacherClassSubjects
                                                     .Where(tcs => tcs.TeacherId == teacherRecord.Id)
                                                     .Select(tcs => tcs.Subject)
                                                     .Distinct()
                                                     .OrderBy(s => s!.Name)
                                                     .ToListAsync();
                viewModel.AssignedClasses = new SelectList(assignedClasses, "Id", "NameWithSection", viewModel.ClassId);
                viewModel.AssignedSubjects = new SelectList(assignedSubjects, "Id", "Name", viewModel.SubjectId);
                return View("MarkAttendance", viewModel);
            }


            if (teacherRecord == null)
            {
                TempData["ErrorMessage"] = "Your teacher profile is not linked. Please contact an administrator.";
                return RedirectToAction("Index", "Home");
            }

            // Verify the teacher is authorized for this class and subject
            var isAssigned = await _context.TeacherClassSubjects
                                           .AnyAsync(tcs => tcs.TeacherId == teacherRecord.Id &&
                                                            tcs.ClassId == viewModel.ClassId &&
                                                            tcs.SubjectId == viewModel.SubjectId);
            if (!isAssigned)
            {
                TempData["ErrorMessage"] = "You are not authorized to mark attendance for this class and subject combination.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var studentVm in viewModel.Students)
            {
                var existingAttendance = await _context.Attendances
                                                       .FirstOrDefaultAsync(a => a.StudentId == studentVm.StudentId &&
                                                                                 a.ClassId == viewModel.ClassId &&
                                                                                 a.SubjectId == viewModel.SubjectId &&
                                                                                 a.AttendanceDate.Date == viewModel.AttendanceDate.Date);

                if (existingAttendance == null)
                {
                    // Create new attendance record
                    var newAttendance = new Attendance
                    {
                        StudentId = studentVm.StudentId,
                        ClassId = viewModel.ClassId,
                        SubjectId = viewModel.SubjectId,
                        AttendanceDate = viewModel.AttendanceDate,
                        Status = studentVm.Status
                    };
                    _context.Attendances.Add(newAttendance);
                }
                else
                {
                    // Update existing attendance record
                    existingAttendance.Status = studentVm.Status;
                    _context.Attendances.Update(existingAttendance);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Attendance saved successfully!";
            return RedirectToAction(nameof(ViewAttendance)); // Redirect to view attendance page
        }


        // GET: TeacherAttendance/ViewAttendance (View past attendance records for teacher's assignments)
        public async Task<IActionResult> ViewAttendance(int? classId, int? subjectId, DateTime? attendanceDate)
        {
            ViewData["Title"] = "View Attendance";
            var teacherId = _userManager.GetUserId(User);
            var teacherRecord = await _context.Teachers.FirstOrDefaultAsync(t => t.ApplicationUserId == teacherId);

            if (teacherRecord == null)
            {
                TempData["ErrorMessage"] = "Your teacher profile is not linked. Please contact an administrator.";
                return RedirectToAction("Index", "Home");
            }

            // Get classes assigned to this teacher
            var assignedClasses = await _context.TeacherClassSubjects
                                                .Where(tcs => tcs.TeacherId == teacherRecord.Id)
                                                .Select(tcs => tcs.Class)
                                                .Distinct()
                                                .OrderBy(c => c!.Name)
                                                .ThenBy(c => c!.Section)
                                                .ToListAsync();

            // Get subjects assigned to this teacher (across all classes)
            var assignedSubjects = await _context.TeacherClassSubjects
                                                 .Where(tcs => tcs.TeacherId == teacherRecord.Id)
                                                 .Select(tcs => tcs.Subject)
                                                 .Distinct()
                                                 .OrderBy(s => s!.Name)
                                                 .ToListAsync();

            ViewBag.ClassId = new SelectList(assignedClasses, "Id", "NameWithSection", classId);
            ViewBag.SubjectId = new SelectList(assignedSubjects, "Id", "Name", subjectId);
            ViewBag.AttendanceDate = attendanceDate ?? DateTime.Today;

            IQueryable<Attendance> query = _context.Attendances
                                                   .Include(a => a.Student)
                                                   .Include(a => a.Class)
                                                   .Include(a => a.Subject);

            // Filter by teacher's assigned classes/subjects
            // FIX: Use Contains on separate lists of IDs for translatability
            var teacherAssignedClassIds = await _context.TeacherClassSubjects
                                                                .Where(tcs => tcs.TeacherId == teacherRecord.Id)
                                                                .Select(tcs => tcs.ClassId)
                                                                .Distinct()
                                                                .ToListAsync();

            var teacherAssignedSubjectIds = await _context.TeacherClassSubjects
                                                                .Where(tcs => tcs.TeacherId == teacherRecord.Id)
                                                                .Select(tcs => tcs.SubjectId)
                                                                .Distinct()
                                                                .ToListAsync();

            // Filter attendance records to only show those related to the teacher's assignments
            query = query.Where(a => teacherAssignedClassIds.Contains(a.ClassId) && teacherAssignedSubjectIds.Contains(a.SubjectId));


            if (classId.HasValue)
            {
                query = query.Where(a => a.ClassId == classId.Value);
            }
            if (subjectId.HasValue)
            {
                query = query.Where(a => a.SubjectId == subjectId.Value);
            }
            if (attendanceDate.HasValue)
            {
                query = query.Where(a => a.AttendanceDate.Date == attendanceDate.Value.Date);
            }

            var attendances = await query.OrderByDescending(a => a.AttendanceDate)
                                         .ThenBy(a => a.Class!.Name)
                                         .ThenBy(a => a.Subject!.Name)
                                         .ThenBy(a => a.Student!.LastName)
                                         .ToListAsync();

            return View(attendances);
        }
    }
}
