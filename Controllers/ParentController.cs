using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApp.Data;
using StudentManagementApp.Models;
using StudentManagementApp.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // For List

namespace StudentManagementApp.Controllers
{
    [Authorize(Roles = "Parent")]
    public class ParentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ParentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Helper to get the current Parent's ID
        private async Task<int?> GetCurrentParentIdAsync()
        {
            var userId = _userManager.GetUserId(User);
            var parent = await _context.Parents.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            return parent?.Id;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);

            var parent = await _context.Parents
                                       .Include(p => p.ApplicationUser)
                                       .Include(p => p.Children!)
                                           .ThenInclude(s => s.ApplicationUser)
                                       .Include(p => p.Children!)
                                           .ThenInclude(s => s.Class)
                                       .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

            if (parent == null)
            {
                TempData["ErrorMessage"] = "Your parent profile is not fully set up. Please contact administration.";
                return View("NoProfile");
            }

            var parentDashboardViewModel = new ParentDashboardViewModel
            {
                ParentId = parent.Id,
                ParentFullName = parent.FullName,
                ParentEmail = parent.Email,
                Children = new List<ChildDashboardViewModel>()
            };

            foreach (var child in parent.Children!)
            {
                var childAttendance = await _context.Attendances
                                                    .Where(a => a.StudentId == child.Id)
                                                    .ToListAsync();
                int presentDays = childAttendance.Count(a => a.Status == "Present");
                int absentDays = childAttendance.Count(a => a.Status == "Absent");
                int totalAttendanceDays = childAttendance.Count;
                double attendancePercentage = totalAttendanceDays > 0 ? (double)presentDays / totalAttendanceDays * 100 : 0;

                var recentMarks = await _context.Marks
                                                .Include(m => m.Exam)
                                                    .ThenInclude(e => e!.Subject)
                                                .Include(m => m.Exam)
                                                    .ThenInclude(e => e!.Class)
                                                .Where(m => m.StudentId == child.Id)
                                                .OrderByDescending(m => m.DateRecorded)
                                                .Take(5)
                                                .ToListAsync();

                var allMarksForChild = await _context.Marks
                                                     .Include(m => m.Exam)
                                                        .ThenInclude(e => e!.Subject)
                                                     .Where(m => m.StudentId == child.Id && m.Exam != null && m.Exam.MaxMarks.HasValue)
                                                     .ToListAsync();

                var subjectAverages = allMarksForChild
                    .GroupBy(m => m.Exam!.Subject!.Name)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Average(m => (double)m.MarksObtained / m.Exam!.MaxMarks!.Value * 100)
                    );

                var upcomingExams = new List<Exam>();
                if (child.ClassId.HasValue)
                {
                    upcomingExams = await _context.Exams
                                                  .Include(e => e.Class)
                                                  .Include(e => e.Subject)
                                                  .Where(e => e.ClassId == child.ClassId.Value && e.ExamDate >= DateOnly.FromDateTime(DateTime.Today))
                                                  .OrderBy(e => e.ExamDate)
                                                  .Take(5)
                                                  .ToListAsync();
                }

                parentDashboardViewModel.Children.Add(new ChildDashboardViewModel
                {
                    StudentId = child.Id,
                    StudentFullName = child.FullName,
                    StudentEmail = child.Email,
                    ClassNameWithSection = child.Class?.NameWithSection ?? "Not Enrolled",

                    TotalAttendanceDays = totalAttendanceDays,
                    PresentDays = presentDays,
                    AbsentDays = absentDays,
                    AttendancePercentage = attendancePercentage,
                    RecentAttendance = childAttendance.OrderByDescending(a => a.AttendanceDate).Take(5).ToList(),

                    RecentMarks = recentMarks,
                    SubjectAverages = subjectAverages,
                    UpcomingExams = upcomingExams
                });
            }

            ViewData["Title"] = $"{parent.FirstName}'s Dashboard";
            return View(parentDashboardViewModel);
        }

        // --- Parent Invoice Actions ---

        // GET: Parent/MyInvoices
        public async Task<IActionResult> MyInvoices()
        {
            ViewData["Title"] = "My Invoices";
            var parentId = await GetCurrentParentIdAsync();
            if (!parentId.HasValue)
            {
                TempData["ErrorMessage"] = "Your parent profile is not linked. Please contact an administrator.";
                return RedirectToAction("Dashboard");
            }

            var parent = await _context.Parents.FirstOrDefaultAsync(p => p.Id == parentId.Value);
            if (parent == null)
            {
                TempData["ErrorMessage"] = "Parent profile not found.";
                return RedirectToAction("Dashboard");
            }

            var invoices = await _context.Invoices
                                         .Include(i => i.Student)
                                         .Include(i => i.Payments)
                                         .Where(i => i.ParentId == parentId.Value)
                                         .OrderByDescending(i => i.IssueDate)
                                         .ToListAsync();

            var viewModel = new ParentInvoiceListViewModel
            {
                ParentId = parent.Id,
                ParentFullName = parent.FullName,
                Invoices = invoices.Select(i => {
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

                    return new ParentInvoiceSummaryViewModel
                    {
                        InvoiceId = i.Id,
                        InvoiceNumber = i.InvoiceNumber,
                        StudentFullName = i.Student?.FullName ?? "N/A",
                        IssueDate = i.IssueDate,
                        DueDate = i.DueDate,
                        TotalAmount = i.TotalAmount,
                        AmountPaid = amountPaid,
                        BalanceDue = balanceDue,
                        Status = status
                    };
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Parent/InvoiceDetails/5
        public async Task<IActionResult> InvoiceDetails(int? id)
        {
            ViewData["Title"] = "Invoice Details";
            if (id == null) return NotFound();

            var parentId = await GetCurrentParentIdAsync();
            if (!parentId.HasValue)
            {
                TempData["ErrorMessage"] = "Your parent profile is not linked. Please contact an administrator.";
                return RedirectToAction("Dashboard");
            }

            var invoice = await _context.Invoices
                                        .Include(i => i.Student)
                                        .Include(i => i.Parent)
                                        .Include(i => i.InvoiceItems!)
                                            .ThenInclude(ii => ii.FeeType)
                                        .Include(i => i.Payments)
                                            .ThenInclude(p => p.Student)
                                        .Where(i => i.Id == id && i.ParentId == parentId.Value)
                                        .FirstOrDefaultAsync();

            if (invoice == null)
            {
                return NotFound($"Invoice with ID {id} not found or you do not have access.");
            }

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
    }
}
