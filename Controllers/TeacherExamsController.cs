using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementApp.Data;
using StudentManagementApp.Models;
using StudentManagementApp.ViewModels;
using System.Linq;

namespace StudentManagementApp.Controllers
{
    // Only teachers can access this controller
    [Authorize(Roles = "Teacher")]
    public class TeacherExamsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherExamsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Helper method to get the current teacher's ID from the Teacher model
        private async Task<int?> GetCurrentTeacherIdAsync()
        {
            var userId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.ApplicationUserId == userId);
            return teacher?.Id;
        }

        // Helper method to get assigned classes for the current teacher
        private async Task<List<Class>> GetAssignedClassesAsync(int teacherRecordId)
        {
            return await _context.TeacherClassSubjects
                                 .Where(tcs => tcs.TeacherId == teacherRecordId)
                                 .Select(tcs => tcs.Class)
                                 .Distinct()
                                 .OrderBy(c => c!.Name)
                                 .ThenBy(c => c!.Section)
                                 .ToListAsync();
        }

        // Helper method to get assigned subjects for the current teacher
        private async Task<List<Subject>> GetAssignedSubjectsAsync(int teacherRecordId)
        {
            return await _context.TeacherClassSubjects
                                 .Where(tcs => tcs.TeacherId == teacherRecordId)
                                 .Select(tcs => tcs.Subject)
                                 .Distinct()
                                 .OrderBy(s => s!.Name)
                                 .ToListAsync();
        }

        // GET: TeacherExams
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Manage Exams";
            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue)
            {
                TempData["ErrorMessage"] = "Your teacher profile is not linked. Please contact an administrator.";
                return RedirectToAction("Index", "Home");
            }

            // Only show exams created by or assigned to this teacher
            var exams = await _context.Exams
                                      .Include(e => e.Class)
                                      .Include(e => e.Subject)
                                      .Include(e => e.Teacher)
                                      .Where(e => e.TeacherId == teacherId.Value) // Filter by current teacher
                                      .OrderByDescending(e => e.ExamDate)
                                      .ToListAsync();
            return View(exams);
        }

        // GET: TeacherExams/Create
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Create New Exam";
            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue)
            {
                TempData["ErrorMessage"] = "Your teacher profile is not linked. Please contact an administrator.";
                return RedirectToAction("Index", "Home");
            }

            ViewData["ClassId"] = new SelectList(await GetAssignedClassesAsync(teacherId.Value), "Id", "NameWithSection");
            ViewData["SubjectId"] = new SelectList(await GetAssignedSubjectsAsync(teacherId.Value), "Id", "Name");
            return View();
        }

        // POST: TeacherExams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,ExamDate,ClassId,SubjectId,MaxMarks")] Exam exam)
        {
            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue)
            {
                TempData["ErrorMessage"] = "Your teacher profile is not linked. Please contact an administrator.";
                return RedirectToAction("Index", "Home");
            }

            // Automatically assign the current teacher as the creator/manager
            exam.TeacherId = teacherId.Value;

            // Verify the teacher is assigned to teach this class and subject combination
            var isAssigned = await _context.TeacherClassSubjects
                                           .AnyAsync(tcs => tcs.TeacherId == teacherId.Value &&
                                                            tcs.ClassId == exam.ClassId &&
                                                            tcs.SubjectId == exam.SubjectId);
            if (!isAssigned)
            {
                ModelState.AddModelError(string.Empty, "You are not authorized to create exams for this class and subject combination.");
                TempData["ErrorMessage"] = "Exam creation failed: Not authorized for the selected class/subject.";
                ViewData["ClassId"] = new SelectList(await GetAssignedClassesAsync(teacherId.Value), "Id", "NameWithSection", exam.ClassId);
                ViewData["SubjectId"] = new SelectList(await GetAssignedSubjectsAsync(teacherId.Value), "Id", "Name", exam.SubjectId);
                return View(exam);
            }

            if (ModelState.IsValid)
            {
                _context.Add(exam);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Exam '{exam.Name}' created successfully!";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Exam creation failed due to validation errors. Please check the form.";
            ViewData["ClassId"] = new SelectList(await GetAssignedClassesAsync(teacherId.Value), "Id", "NameWithSection", exam.ClassId);
            ViewData["SubjectId"] = new SelectList(await GetAssignedSubjectsAsync(teacherId.Value), "Id", "Name", exam.SubjectId);
            return View(exam);
        }

        // GET: TeacherExams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exam = await _context.Exams.FindAsync(id);
            if (exam == null)
            {
                return NotFound();
            }

            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue || exam.TeacherId != teacherId.Value) // Ensure only the creating teacher can edit
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this exam.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = $"Edit Exam: {exam.Name}";
            ViewData["ClassId"] = new SelectList(await GetAssignedClassesAsync(teacherId.Value), "Id", "NameWithSection", exam.ClassId);
            ViewData["SubjectId"] = new SelectList(await GetAssignedSubjectsAsync(teacherId.Value), "Id", "Name", exam.SubjectId);
            return View(exam);
        }

        // POST: TeacherExams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,ExamDate,ClassId,SubjectId,TeacherId,MaxMarks")] Exam exam)
        {
            if (id != exam.Id)
            {
                return NotFound();
            }

            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue || exam.TeacherId != teacherId.Value) // Re-verify authorization
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this exam.";
                return RedirectToAction(nameof(Index));
            }

            // Verify the teacher is still assigned to teach this class and subject combination
            var isAssigned = await _context.TeacherClassSubjects
                                           .AnyAsync(tcs => tcs.TeacherId == teacherId.Value &&
                                                            tcs.ClassId == exam.ClassId &&
                                                            tcs.SubjectId == exam.SubjectId);
            if (!isAssigned)
            {
                ModelState.AddModelError(string.Empty, "You are no longer authorized for the selected class and subject combination.");
                TempData["ErrorMessage"] = "Exam update failed: Not authorized for the selected class/subject.";
                ViewData["ClassId"] = new SelectList(await GetAssignedClassesAsync(teacherId.Value), "Id", "NameWithSection", exam.ClassId);
                ViewData["SubjectId"] = new SelectList(await GetAssignedSubjectsAsync(teacherId.Value), "Id", "Name", exam.SubjectId);
                return View(exam);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(exam);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Exam '{exam.Name}' updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExamExists(exam.Id))
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

            TempData["ErrorMessage"] = "Exam update failed due to validation errors. Please check the form.";
            ViewData["ClassId"] = new SelectList(await GetAssignedClassesAsync(teacherId.Value), "Id", "NameWithSection", exam.ClassId);
            ViewData["SubjectId"] = new SelectList(await GetAssignedSubjectsAsync(teacherId.Value), "Id", "Name", exam.SubjectId);
            return View(exam);
        }

        // GET: TeacherExams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exam = await _context.Exams
                .Include(e => e.Class)
                .Include(e => e.Subject)
                .Include(e => e.Teacher)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (exam == null)
            {
                return NotFound();
            }

            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue || exam.TeacherId != teacherId.Value) // Ensure only the creating teacher can view details
            {
                TempData["ErrorMessage"] = "You are not authorized to view details of this exam.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = $"Exam Details: {exam.Name}";
            return View(exam);
        }

        // GET: TeacherExams/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exam = await _context.Exams
                .Include(e => e.Class)
                .Include(e => e.Subject)
                .Include(e => e.Teacher)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (exam == null)
            {
                return NotFound();
            }

            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue || exam.TeacherId != teacherId.Value) // Ensure only the creating teacher can delete
            {
                TempData["ErrorMessage"] = "You are not authorized to delete this exam.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = $"Delete Exam: {exam.Name}";
            return View(exam);
        }

        // POST: TeacherExams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            var teacherId = await GetCurrentTeacherIdAsync();

            if (exam == null || !teacherId.HasValue || exam.TeacherId != teacherId.Value) // Re-verify authorization
            {
                TempData["ErrorMessage"] = "You are not authorized to delete this exam or the exam was not found.";
                return RedirectToAction(nameof(Index));
            }

            // Before deleting the exam, delete any associated marks
            var associatedMarks = await _context.Marks.Where(m => m.ExamId == id).ToListAsync();
            _context.Marks.RemoveRange(associatedMarks);

            _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Exam '{exam.Name}' and its associated marks deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool ExamExists(int id)
        {
            return _context.Exams.Any(e => e.Id == id);
        }

        // --- Marks Management Actions ---

        // GET: TeacherExams/EnterMarks/5 (examId)
        public async Task<IActionResult> EnterMarks(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exam = await _context.Exams
                                     .Include(e => e.Class)
                                     .Include(e => e.Subject)
                                     .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
            {
                return NotFound();
            }

            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue || exam.TeacherId != teacherId.Value) // Ensure only the creating teacher can enter marks
            {
                TempData["ErrorMessage"] = "You are not authorized to enter marks for this exam.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = $"Enter Marks for {exam.Name}";

            // Get all students enrolled in the exam's class
            var enrolledStudents = await _context.Enrollments
                                                 .Where(e => e.ClassId == exam.ClassId && e.Status == "Active")
                                                 .Select(e => e.Student!)
                                                 .OrderBy(s => s.FirstName)
                                                 .ThenBy(s => s.LastName)
                                                 .ToListAsync();

            // Get existing marks for this exam
            var existingMarks = await _context.Marks
                                              .Where(m => m.ExamId == exam.Id)
                                              .ToDictionaryAsync(m => m.StudentId, m => m.MarksObtained);

            var viewModel = new EnterMarksViewModel
            {
                ExamId = exam.Id,
                ExamName = exam.Name,
                ClassId = exam.ClassId,
                ClassName = exam.Class!.NameWithSection,
                SubjectId = exam.SubjectId,
                SubjectName = exam.Subject!.Name,
                MaxMarks = exam.MaxMarks,
                Students = enrolledStudents.Select(s => new StudentMarkEntryViewModel
                {
                    StudentId = s.Id,
                    StudentName = s.FullName,
                    MarksObtained = existingMarks.GetValueOrDefault(s.Id) // Populate with existing marks or null
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: TeacherExams/SaveMarks
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMarks(EnterMarksViewModel viewModel)
        {
            var exam = await _context.Exams.FindAsync(viewModel.ExamId);
            if (exam == null)
            {
                return NotFound();
            }

            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue || exam.TeacherId != teacherId.Value) // Re-verify authorization
            {
                TempData["ErrorMessage"] = "You are not authorized to save marks for this exam.";
                return RedirectToAction(nameof(Index));
            }

            // Custom validation for marks against MaxMarks
            foreach (var studentMark in viewModel.Students)
            {
                if (studentMark.MarksObtained.HasValue)
                {
                    if (studentMark.MarksObtained < 0)
                    {
                        ModelState.AddModelError($"Students[{viewModel.Students.IndexOf(studentMark)}].MarksObtained", "Marks cannot be negative.");
                    }
                    else if (exam.MaxMarks.HasValue && studentMark.MarksObtained > exam.MaxMarks.Value)
                    {
                        ModelState.AddModelError($"Students[{viewModel.Students.IndexOf(studentMark)}].MarksObtained", $"Marks cannot exceed {exam.MaxMarks.Value}.");
                    }
                }
            }


            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Failed to save marks due to validation errors. Please check the entries.";
                // Re-populate exam details for view
                viewModel.ExamName = exam.Name;
                viewModel.ClassName = exam.Class!.NameWithSection;
                viewModel.SubjectName = exam.Subject!.Name;
                viewModel.MaxMarks = exam.MaxMarks;
                return View("EnterMarks", viewModel);
            }

            foreach (var studentMarkVm in viewModel.Students)
            {
                // Only process if marks were entered (not null)
                if (studentMarkVm.MarksObtained.HasValue)
                {
                    var existingMark = await _context.Marks
                                                     .FirstOrDefaultAsync(m => m.ExamId == viewModel.ExamId &&
                                                                               m.StudentId == studentMarkVm.StudentId);

                    if (existingMark == null)
                    {
                        // Create new mark record
                        var newMark = new Mark
                        {
                            ExamId = viewModel.ExamId,
                            StudentId = studentMarkVm.StudentId,
                            MarksObtained = studentMarkVm.MarksObtained.Value,
                            DateRecorded = DateTime.Today
                        };
                        _context.Marks.Add(newMark);
                    }
                    else
                    {
                        // Update existing mark record
                        existingMark.MarksObtained = studentMarkVm.MarksObtained.Value;
                        existingMark.DateRecorded = DateTime.Today; // Update date recorded on edit
                        _context.Marks.Update(existingMark);
                    }
                }
                else // If marks are cleared (set to null), delete existing mark if any
                {
                    var existingMark = await _context.Marks
                                                     .FirstOrDefaultAsync(m => m.ExamId == viewModel.ExamId &&
                                                                               m.StudentId == studentMarkVm.StudentId);
                    if (existingMark != null)
                    {
                        _context.Marks.Remove(existingMark);
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Marks saved successfully!";
            return RedirectToAction(nameof(ViewMarks), new { id = viewModel.ExamId }); // Redirect to view marks for this exam
        }

        // GET: TeacherExams/ViewMarks/5 (examId)
        public async Task<IActionResult> ViewMarks(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exam = await _context.Exams
                                     .Include(e => e.Class)
                                     .Include(e => e.Subject)
                                     .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
            {
                return NotFound();
            }

            var teacherId = await GetCurrentTeacherIdAsync();
            if (!teacherId.HasValue || exam.TeacherId != teacherId.Value) // Ensure only the creating teacher can view marks
            {
                TempData["ErrorMessage"] = "You are not authorized to view marks for this exam.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = $"Marks for {exam.Name}";

            var marks = await _context.Marks
                                      .Include(m => m.Student)
                                      .Where(m => m.ExamId == exam.Id)
                                      .OrderBy(m => m.Student!.LastName)
                                      .ThenBy(m => m.Student!.FirstName)
                                      .ToListAsync();

            // Create a simple ViewModel to pass exam details and marks
            var viewModel = new EnterMarksViewModel // Reusing EnterMarksViewModel structure for display
            {
                ExamId = exam.Id,
                ExamName = exam.Name,
                ClassId = exam.ClassId,
                ClassName = exam.Class!.NameWithSection,
                SubjectId = exam.SubjectId,
                SubjectName = exam.Subject!.Name,
                MaxMarks = exam.MaxMarks,
                Students = marks.Select(m => new StudentMarkEntryViewModel
                {
                    StudentId = m.StudentId,
                    StudentName = m.Student!.FullName,
                    MarksObtained = m.MarksObtained
                }).ToList()
            };

            return View(viewModel);
        }
    }
}
