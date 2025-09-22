using Microsoft.EntityFrameworkCore;
using StudentManagementApp.Data;
using StudentManagementApp.Models;
using StudentManagementApp.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList
using System.Collections.Generic; // For List

namespace StudentManagementApp.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;

        public InvoiceService(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- Invoice Operations ---

        public async Task<IEnumerable<InvoiceListViewModel>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.Student)
                .Include(i => i.Parent)
                .OrderByDescending(i => i.IssueDate)
                .Select(i => new InvoiceListViewModel
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    IssueDate = i.IssueDate,
                    DueDate = i.DueDate,
                    StudentName = i.Student != null ? $"{i.Student.FirstName} {i.Student.LastName}" : "N/A",
                    ParentName = i.Parent != null ? $"{i.Parent.FirstName} {i.Parent.LastName}" : "N/A",
                    TotalAmount = i.TotalAmount,
                    AmountPaid = i.AmountPaid, // Use the stored TotalAmountPaid
                    BalanceDue = i.TotalAmount - i.AmountPaid, // Calculate from stored
                    Status = i.Status
                })
                .ToListAsync();
        }

        public async Task<InvoiceDetailsViewModel?> GetInvoiceDetailsAsync(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Student)
                .Include(i => i.Parent)
                .Include(i => i.InvoiceItems!) // Use ! for non-nullable if confident
                    .ThenInclude(ii => ii.FeeType)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null)
            {
                return null;
            }

            // BalanceDue is a calculated property in ViewModel, no direct assignment needed here.
            // Payments collection is directly passed.

            return new InvoiceDetailsViewModel
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                StudentId = invoice.StudentId, // Pass StudentId
                StudentFullName = invoice.Student != null ? $"{invoice.Student.FirstName} {invoice.Student.LastName}" : "N/A",
                ParentId = invoice.ParentId, // Handle nullable ParentId
                ParentFullName = invoice.Parent != null ? $"{invoice.Parent.FirstName} {invoice.Parent.LastName}" : "N/A",
                TotalAmount = invoice.TotalAmount,
                Notes = invoice.Notes,
                Status = invoice.Status,
                AmountPaid = invoice.AmountPaid, // Use the stored TotalAmountPaid
                InvoiceItems = invoice.InvoiceItems!.Select(ii => new InvoiceItemViewModel
                {
                    Id = ii.Id,
                    FeeTypeId = ii.FeeTypeId,
                    FeeTypeName = ii.FeeType != null ? ii.FeeType.Name : "N/A",
                    Description = ii.Description,
                    Amount = ii.Amount
                }).ToList(),
                Payments = invoice.Payments!.ToList() // Pass the actual Payment models
            };
        }

        public async Task<bool> CreateInvoiceAsync(CreateInvoiceViewModel model)
        {
            if (model == null) return false;

            var invoice = new Invoice
            {
                InvoiceNumber = model.InvoiceNumber,
                IssueDate = model.IssueDate,
                DueDate = model.DueDate,
                StudentId = model.StudentId,
                ParentId = (int)model.ParentId, // ParentId is int?, so direct assignment is fine
                TotalAmount = model.InvoiceItems.Sum(item => item.Amount),
                AmountPaid = 0m, // New invoices start with 0 paid
                Notes = model.Notes,
                Status = "Outstanding", // Default status
                InvoiceItems = model.InvoiceItems.Select(item => new InvoiceItem
                {
                    FeeTypeId = item.FeeTypeId,
                    Description = item.Description,
                    Amount = item.Amount
                }).ToList()
            };

            _context.Add(invoice);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateInvoiceAsync(CreateInvoiceViewModel model)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments) // Include payments to recalculate status
                .FirstOrDefaultAsync(i => i.Id == model.Id);

            if (invoice == null)
            {
                return false;
            }

            invoice.InvoiceNumber = model.InvoiceNumber;
            invoice.IssueDate = model.IssueDate;
            invoice.DueDate = model.DueDate;
            invoice.StudentId = model.StudentId;
            invoice.ParentId = (int) model.ParentId; // ParentId is int?, direct assignment fine
            invoice.Notes = model.Notes;
            // Status is recalculated below, not taken directly from model unless explicitly overridden by admin
            invoice.TotalAmount = model.InvoiceItems.Sum(item => item.Amount); // Recalculate total

            // Update Invoice Items
            var existingInvoiceItems = invoice.InvoiceItems!.ToList();

            // Remove items not in the model
            existingInvoiceItems.RemoveAll(item => !model.InvoiceItems.Any(mItem => mItem.Id == item.Id));

            foreach (var itemModel in model.InvoiceItems)
            {
                var existingItem = existingInvoiceItems.FirstOrDefault(ii => ii.Id == itemModel.Id);
                if (existingItem != null)
                {
                    // Update existing item
                    existingItem.FeeTypeId = itemModel.FeeTypeId;
                    existingItem.Description = itemModel.Description;
                    existingItem.Amount = itemModel.Amount;
                }
                else
                {
                    // Add new item
                    existingInvoiceItems.Add(new InvoiceItem
                    {
                        FeeTypeId = itemModel.FeeTypeId,
                        Description = itemModel.Description,
                        Amount = itemModel.Amount,
                        InvoiceId = invoice.Id
                    });
                }
            }
            invoice.InvoiceItems = existingInvoiceItems; // Assign back to navigation property

            // Save changes to invoice items before recalculating TotalAmountPaid and Status
            await _context.SaveChangesAsync();

            // After updating items, recalculate TotalAmountPaid and update Status based on payments
            await UpdateInvoiceStatusAndAmountPaid(invoice.Id);
            return true;
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return false;
            }

            // EF Core should cascade delete InvoiceItems and Payments if configured correctly (OnDelete: Cascade)
            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CreateInvoiceViewModel?> GetInvoiceForEditAsync(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems!)
                    .ThenInclude(ii => ii.FeeType)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return null;

            var students = await _context.Students
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = $"{s.FirstName} {s.LastName}" })
                .ToListAsync();

            var parents = await _context.Parents
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = $"{p.FirstName} {p.LastName}" })
                .ToListAsync();

            var allFeeTypes = await _context.FeeTypes
                .Select(ft => new SelectListItem { Value = ft.Id.ToString(), Text = ft.Name })
                .ToListAsync();

            var model = new CreateInvoiceViewModel
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                StudentId = invoice.StudentId,
                ParentId = invoice.ParentId, // Direct assignment from int?
                TotalAmount = invoice.TotalAmount,
                Notes = invoice.Notes,
                Status = invoice.Status,
                Students = new SelectList(students, "Value", "Text", invoice.StudentId),
                Parents = new SelectList(parents, "Value", "Text", invoice.ParentId),
                InvoiceItems = invoice.InvoiceItems!.Select(ii => new InvoiceItemViewModel
                {
                    Id = ii.Id,
                    FeeTypeId = ii.FeeTypeId,
                    Description = ii.Description,
                    Amount = ii.Amount,
                    FeeTypes = new SelectList(allFeeTypes, "Value", "Text", ii.FeeTypeId)
                }).ToList()
            };

            if (!model.InvoiceItems.Any())
            {
                model.InvoiceItems.Add(new InvoiceItemViewModel { FeeTypes = new SelectList(allFeeTypes, "Value", "Text") });
            }

            return model;
        }


        // --- Payment Operations ---

        public async Task<bool> RecordPayment(int invoiceId, decimal amount, string paymentMethod, DateTime paymentDate, string? notes, string? transactionId = null, string status = "Completed")
        {
            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                return false;
            }

            if (invoice.Status == "Paid" || invoice.Status == "Waived")
            {
                return false;
            }

            var payment = new Payment
            {
                InvoiceId = invoiceId,
                PaymentDate = paymentDate,
                Amount = amount,
                PaymentMethod = paymentMethod,
                TransactionId = transactionId, // Now exists on Payment model
                Status = status,
                Notes = notes,
                StudentId = invoice.StudentId, // Populate StudentId for payment
                ParentId = invoice.ParentId, // Handle nullable ParentId for payment
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            await UpdateInvoiceStatusAndAmountPaid(invoiceId);

            return true;
        }

        public async Task<decimal> GetInvoiceBalanceAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Where(i => i.Id == invoiceId)
                .Select(i => new { i.TotalAmount, i.AmountPaid }) // Use TotalAmountPaid
                .FirstOrDefaultAsync();

            if (invoice == null) return 0;

            return invoice.TotalAmount - invoice.AmountPaid;
        }

        public async Task<IEnumerable<PaymentListViewModel>> GetPaymentsByInvoiceIdAsync(int invoiceId)
        {
            return await _context.Payments
                .Where(p => p.InvoiceId == invoiceId)
                .Select(p => new PaymentListViewModel
                {
                    Id = p.Id,
                    InvoiceId = (int)p.InvoiceId,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    Status = p.Status
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<ParentInvoiceSummaryViewModel>> GetInvoicesForParentAsync(string parentApplicationUserId)
        {
            return await _context.Invoices
                .Where(i => i.Parent != null && i.Parent.ApplicationUserId == parentApplicationUserId) // Corrected: Use ApplicationUserId
                .Include(i => i.Student)
                .OrderByDescending(i => i.IssueDate)
                .Select(i => new ParentInvoiceSummaryViewModel
                {
                    InvoiceId = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    StudentFullName = i.Student != null ? $"{i.Student.FirstName} {i.Student.LastName}" : "N/A",
                    IssueDate = i.IssueDate,
                    DueDate = i.DueDate,
                    TotalAmount = i.TotalAmount,
                    AmountPaid = i.AmountPaid, // Use stored TotalAmountPaid
                    BalanceDue = i.TotalAmount - i.AmountPaid, // Calculate from stored
                    Status = i.Status
                })
                .ToListAsync();
        }


        // --- Internal/Helper Methods ---

        /// <summary>
        /// Recalculates the TotalAmountPaid and updates the Status for a given invoice.
        /// This method is internal to the service and not exposed via the interface.
        /// </summary>
        private async Task UpdateInvoiceStatusAndAmountPaid(int invoiceId)
        {
            var invoice = await _context.Invoices
                                .Include(i => i.Payments)
                                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice != null)
            {
                decimal totalPaid = invoice.Payments?.Sum(p => p.Amount) ?? 0m;
                invoice.AmountPaid = totalPaid;

                decimal currentBalanceDue = invoice.TotalAmount - totalPaid;

                if (currentBalanceDue <= 0)
                {
                    invoice.Status = "Paid";
                }
                else if (totalPaid > 0) // If some amount paid, but not full
                {
                    invoice.Status = "Partially Paid";
                }
                else
                {
                    invoice.Status = "Outstanding"; // No payments or all payments reversed
                }

                // Overdue status check (only if not fully paid or waived)
                if (invoice.Status != "Paid" && invoice.Status != "Waived" && invoice.DueDate < DateTime.Today)
                {
                    invoice.Status = "Overdue";
                }

                _context.Update(invoice);
                await _context.SaveChangesAsync();
            }
        }
    }
}
