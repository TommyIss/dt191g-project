using dt191g_project.Data;
using dt191g_project.Models;
using dt191g_project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dt191g_project.Controllers
{
    [Authorize]
    public class CompanyDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserCompanyService _companyService;

        public CompanyDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, UserCompanyService companyService)
        {
            _context = context;
            _userManager = userManager;
            _companyService = companyService;
        }

        public async Task<IActionResult> Index(int companyId)
        {
            var user = await _userManager.GetUserAsync(User);
            var companyUser = await _companyService.GetCompanyUserAsync(user.Id);

            // Kund eller användare utan företag
            if (companyUser == null)
                return RedirectToAction("Index", "Home");

            // Användaren försöker komma åt ett företag de inte tillhör
            if (companyUser.CompanyId != companyId)
                return Forbid();

            var role = companyUser.Role;

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null)
                return NotFound();

            var servicesCount = await _context.Services
                .CountAsync(s => s.CompanyId == companyId);

            var timeSlotsCount = await _context.TimeSlots
                .CountAsync(t => t.CompanyId == companyId);

            var bookingsCount = await _context.Bookings
                .Include(b => b.TimeSlot)
                .CountAsync(b => b.TimeSlot.CompanyId == companyId);

            var model = new CompanyDashboardModel
            {
                Company = company,
                ServicesCount = servicesCount,
                TimeSlotsCount = timeSlotsCount,
                BookingsCount = bookingsCount,
                Role = role
            };

            return View(model);
        }
    }
}
