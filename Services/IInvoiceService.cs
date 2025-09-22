using StudentManagementApp.Models;
using StudentManagementApp.ViewModels;
using System.Collections.Generic; // Ensure this is present

namespace StudentManagementApp.Services
{
    public interface IInvoiceService
    {
        // Invoice Operations
        Task<IEnumerable<InvoiceListViewModel>> GetAllInvoicesAsync();
        Task<InvoiceDetailsViewModel?> GetInvoiceDetailsAsync(int id);
        Task<bool> CreateInvoiceAsync(CreateInvoiceViewModel model);
        Task<bool> UpdateInvoiceAsync(CreateInvoiceViewModel model);
        Task<bool> DeleteInvoiceAsync(int id);
        Task<CreateInvoiceViewModel?> GetInvoiceForEditAsync(int id);

        // Payment Operations
        Task<bool> RecordPayment(int invoiceId, decimal amount, string paymentMethod, DateTime paymentDate, string? notes, string? transactionId = null, string status = "Completed");
        Task<decimal> GetInvoiceBalanceAsync(int invoiceId);
        Task<IEnumerable<PaymentListViewModel>> GetPaymentsByInvoiceIdAsync(int invoiceId);
        Task<IEnumerable<ParentInvoiceSummaryViewModel>> GetInvoicesForParentAsync(string parentApplicationUserId); // Changed parameter name for clarity
    }
}
