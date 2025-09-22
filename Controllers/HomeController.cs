using Microsoft.AspNetCore.Mvc;
using StudentManagementApp.Models;
using System.Diagnostics;
using StudentManagementApp.Data; // Add this for ApplicationDbContext
using Microsoft.EntityFrameworkCore; // Add this for ToListAsync, CountAsync
using StudentManagementApp.ViewModels; // Add this for DashboardViewModel
using Microsoft.AspNetCore.Identity; // Add this for UserManager
using System.Threading.Tasks;
using System; // For DateTime

namespace StudentManagementApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // Inject ApplicationDbContext
        private readonly UserManager<ApplicationUser> _userManager; // Inject UserManager

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context; // Assign injected context
            _userManager = userManager; // Assign injected userManager
        }

        public async Task<IActionResult> Index()
        {
            // Fetch counts from the database
            var totalStudents = await _context.Students.CountAsync();
            var totalTeachers = await _context.Teachers.CountAsync();
            var totalUsers = await _userManager.Users.CountAsync(); // Count all registered users
            var activeNotices = await _context.Notices
                .Where(n => n.IsActive && (n.PublishDate <= DateTime.Today) && (n.ExpiryDate == null || n.ExpiryDate >= DateTime.Today))
                .OrderByDescending(n => n.PublishDate)
                .Take(5) // Limit to 5 most recent active notices
                .ToListAsync();

            var upcomingHolidays = await _context.Holidays
                .Where(h => h.HolidayDate >= DateTime.Today)
                .OrderBy(h => h.HolidayDate)
                .Take(5) // Limit to 5 upcoming holidays
                .ToListAsync();

            // Create an instance of the ViewModel and populate it
            var viewModel = new DashboardViewModel
            {
                TotalStudents = totalStudents,
                TotalTeachers = totalTeachers,
                TotalUsers = totalUsers,
                ActiveNotices = activeNotices,
                UpcomingHolidays = upcomingHolidays
            };

            // Pass the ViewModel to the view
            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
