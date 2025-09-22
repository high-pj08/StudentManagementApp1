using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementApp.Data;
using StudentManagementApp.Models;
using StudentManagementApp.ViewModels;
using StudentManagementApp.Services; // Ensure this is present
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace StudentManagementApp.Controllers
{
    [Authorize(Roles = "Parent")]
    public class ParentPaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IInvoiceService _invoiceService; // Inject the service

        public ParentPaymentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IInvoiceService invoiceService)
        {
            _context = context;
            _userManager = userManager;
            _invoiceService = invoiceService; // Initialize the service
        }

        // Helper to get the current Parent's ApplicationUser ID
        private string? GetCurrentApplicationUserId()
        {
            return _userManager.GetUserId(User);
        }

        // Helper to get the current Parent's database ID
        private async Task<int?> GetCurrentParentDbIdAsync()
        {
            var userId = GetCurrentApplicationUserId();
            if (userId == null) return null;
            var parent = await _context.Parents.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            return parent?.Id;
        }

        // GET: ParentPayments/MakePayment
        public async Task<IActionResult> MakePayment(int? invoiceId)
        {
            ViewData["Title"] = "Make a Payment";
            var parentDbId = await GetCurrentParentDbIdAsync();
            var parentApplicationUserId = GetCurrentApplicationUserId();

            if (!parentDbId.HasValue || string.IsNullOrEmpty(parentApplicationUserId))
            {
                TempData["ErrorMessage"] = "Your parent profile is not linked. Please contact an administrator.";
                return RedirectToAction("Dashboard", "Parent");
            }

            var parent = await _context.Parents.Include(p => p.Children).FirstOrDefaultAsync(p => p.Id == parentDbId.Value);
            if (parent == null)
            {
                TempData["ErrorMessage"] = "Parent profile not found.";
                return RedirectToAction("Dashboard", "Parent");
            }

            var viewModel = new MakePaymentViewModel
            {
                ParentName = parent.FullName,
                Students = parent.Children?.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.FullName
                }) ?? new List<SelectListItem>()
            };

            // Populate outstanding invoices for the parent using the service
            var outstandingInvoices = await _context.Invoices
                                                    .Include(i => i.Student)
                                                    .Where(i => i.ParentId == parentDbId.Value)
                                                    .Select(i => new {
                                                        Invoice = i,
                                                        Balance = i.TotalAmount - i.AmountPaid // Use TotalAmountPaid
                                                    })
                                                    .Where(x => x.Balance > 0 && x.Invoice.Status != "Paid" && x.Invoice.Status != "Waived")
                                                    .OrderBy(x => x.Invoice.Status == "Overdue" ? 0 : 1)
                                                    .ThenBy(x => x.Invoice.DueDate)
                                                    .Select(x => new SelectListItem
                                                    {
                                                        Value = x.Invoice.Id.ToString(),
                                                        Text = $"{x.Invoice.InvoiceNumber} - {x.Invoice.Student!.FullName} (Due: {x.Invoice.DueDate:d}) - Balance: {x.Balance:C}"
                                                    })
                                                    .ToListAsync();
            viewModel.OutstandingInvoices = outstandingInvoices;

            if (invoiceId.HasValue)
            {
                var invoiceToPay = await _context.Invoices
                                                .FirstOrDefaultAsync(i => i.Id == invoiceId.Value && i.ParentId == parentDbId.Value);

                if (invoiceToPay != null)
                {
                    viewModel.SelectedInvoiceId = invoiceToPay.Id;
                    viewModel.StudentId = invoiceToPay.StudentId;
                    viewModel.Amount = invoiceToPay.TotalAmount - invoiceToPay.AmountPaid; // Use TotalAmountPaid
                    TempData["InfoMessage"] = $"Paying against Invoice {invoiceToPay.InvoiceNumber}. Amount pre-filled with outstanding balance.";
                }
            }

            if (!viewModel.Students.Any())
            {
                TempData["WarningMessage"] = "You have no children linked to your account to make a payment for. Please link students via the admin panel.";
            }

            return View(viewModel);
        }

        // POST: ParentPayments/MakePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakePayment(MakePaymentViewModel model)
        {
            ViewData["Title"] = "Make a Payment";
            var parentDbId = await GetCurrentParentDbIdAsync();
            var parentApplicationUserId = GetCurrentApplicationUserId();

            if (!parentDbId.HasValue || string.IsNullOrEmpty(parentApplicationUserId))
            {
                TempData["ErrorMessage"] = "Your parent profile is not linked. Please contact an administrator.";
                return RedirectToAction("Dashboard", "Parent");
            }

            var parent = await _context.Parents.Include(p => p.Children).FirstOrDefaultAsync(p => p.Id == parentDbId.Value);
            if (parent == null)
            {
                TempData["ErrorMessage"] = "Parent profile not found.";
                return RedirectToAction("Dashboard", "Parent");
            }

            model.Students = parent.Children?.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.FullName
            }) ?? new List<SelectListItem>();

            var outstandingInvoices = await _context.Invoices
                                                    .Include(i => i.Student)
                                                    .Where(i => i.ParentId == parentDbId.Value)
                                                    .Select(i => new {
                                                        Invoice = i,
                                                        Balance = i.TotalAmount - i.AmountPaid
                                                    })
                                                    .Where(x => x.Balance > 0 && x.Invoice.Status != "Paid" && x.Invoice.Status != "Waived")
                                                    .OrderBy(x => x.Invoice.Status == "Overdue" ? 0 : 1)
                                                    .ThenBy(x => x.Invoice.DueDate)
                                                    .Select(x => new SelectListItem
                                                    {
                                                        Value = x.Invoice.Id.ToString(),
                                                        Text = $"{x.Invoice.InvoiceNumber} - {x.Invoice.Student!.FullName} (Due: {x.Invoice.DueDate:d}) - Balance: {x.Balance:C}"
                                                    })
                                                    .ToListAsync();
            model.OutstandingInvoices = outstandingInvoices;


            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Payment failed due to validation errors. Please check the form.";
                return View(model);
            }

            if (!parent.Children!.Any(s => s.Id == model.StudentId))
            {
                ModelState.AddModelError(nameof(model.StudentId), "Selected student is not linked to your account.");
                TempData["ErrorMessage"] = "Payment failed: Invalid student selected.";
                return View(model);
            }

            Invoice? targetInvoice = null;
            if (model.SelectedInvoiceId.HasValue)
            {
                targetInvoice = await _context.Invoices
                                            .FirstOrDefaultAsync(i => i.Id == model.SelectedInvoiceId.Value && i.ParentId == parentDbId.Value);

                if (targetInvoice == null)
                {
                    ModelState.AddModelError(nameof(model.SelectedInvoiceId), "Selected invoice not found or not accessible.");
                    TempData["ErrorMessage"] = "Payment failed: Invalid invoice selected.";
                    return View(model);
                }

                decimal targetInvoiceBalanceDue = targetInvoice.TotalAmount - targetInvoice.AmountPaid;

                if (model.Amount > targetInvoiceBalanceDue && targetInvoiceBalanceDue > 0)
                {
                    ModelState.AddModelError(nameof(model.Amount), $"Amount exceeds remaining balance for Invoice {targetInvoice.InvoiceNumber}. Remaining: {targetInvoiceBalanceDue:C}");
                    TempData["ErrorMessage"] = "Payment failed: Amount exceeds invoice balance.";
                    return View(model);
                }
            }

            // Use the InvoiceService to record the payment
            bool success = await _invoiceService.RecordPayment(
                model.SelectedInvoiceId!.Value, // This will always have a value if targetInvoice is not null
                model.Amount,
                "Online (Parent UI)", // Specific method for parent-initiated payments
                model.PaymentDate,
                model.Notes,
                "Completed" // Status for parent-initiated payments
            );

            if (success)
            {
                TempData["SuccessMessage"] = $"Payment of {model.Amount:C} for {parent.Children.FirstOrDefault(s => s.Id == model.StudentId)?.FullName} recorded successfully!" +
                                             (targetInvoice != null ? $" Linked to Invoice {targetInvoice.InvoiceNumber}." : "");
                return RedirectToAction(nameof(PaymentHistory));
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to record payment. Please check details.";
                return View(model);
            }
        }

        // GET: ParentPayments/PaymentHistory
        public async Task<IActionResult> PaymentHistory()
        {
            ViewData["Title"] = "My Payment History";
            var parentDbId = await GetCurrentParentDbIdAsync();
            if (!parentDbId.HasValue)
            {
                TempData["ErrorMessage"] = "Your parent profile is not linked. Please contact an administrator.";
                return RedirectToAction("Dashboard", "Parent");
            }

            var payments = await _context.Payments
                                         .Include(p => p.Student)
                                         .Include(p => p.Parent)
                                         .Include(p => p.Invoice)
                                         .Where(p => p.ParentId == parentDbId.Value)
                                         .OrderByDescending(p => p.PaymentDate)
                                         .ToListAsync();

            return View(payments);
        }

        // GET: ParentPayments/GetInvoiceDetailsForPayment (AJAX endpoint)
        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetailsForPayment(int invoiceId)
        {
            var parentDbId = await GetCurrentParentDbIdAsync();
            if (!parentDbId.HasValue)
            {
                return Unauthorized();
            }

            var invoice = await _context.Invoices
                                        .FirstOrDefaultAsync(i => i.Id == invoiceId && i.ParentId == parentDbId.Value);

            if (invoice == null)
            {
                return NotFound();
            }

            // Use the stored TotalAmountPaid for balance calculation
            decimal balanceDue = invoice.TotalAmount - invoice.AmountPaid;

            return Json(new
            {
                invoice.Id,
                invoice.StudentId,
                BalanceDue = balanceDue
            });
        }
    }
}
