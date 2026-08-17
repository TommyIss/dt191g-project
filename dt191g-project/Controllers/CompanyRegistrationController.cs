using dt191g_project.Models; using dt191g_project.Data;
using dt191g_project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace dt191g_project.Controllers
{
    [Authorize]
    public class CompanyRegistrationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompanyRegistrationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }

        // POST: CompanyRegistration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CompanyRegistrationModel model)
        {
            if(!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            var company = new Company
            {
                Name = model.CompanyName,
                Description = model.Description,
                Address = model.Address,
                City = model.City,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                IsActive = true,
                Category = model.Category,
                OpeningsHours = model.OpeningsHours,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            var companyUser = new CompanyUser
            {
                UserId = user.Id,
                CompanyId = company.Id,
                Role = "Admin",
                CreatedAt = DateTime.Now
            };

            _context.CompanyUsers.Add(companyUser);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "CompanyDashboard", new { companyId =company.Id });
        }
    }
}
