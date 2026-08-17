using dt191g_project.Models; using dt191g_project.Data;
using dt191g_project.Models;
using dt191g_project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace dt191g_project.Controllers
{
    [Authorize]
    public class CompanyUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserCompanyService _companyService;

        public CompanyUserController(ApplicationDbContext context, UserCompanyService companyService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _companyService = companyService;
            _userManager = userManager;
        }

        // Hjälpmetod för att hämta användarens företag och verifiera admin-roll
        private async Task<(int? companyId, bool isAdmin)> GetCurrentCompanyAndRoleAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return (null, false);

            var role = await _companyService.GetRoleAsync(user.Id);
            var companyId = await _companyService.GetCompanyIdForUserAsync(user.Id);

            return (companyId, role == "Admin");
        }

        // GET: CompanyUser
        public async Task<IActionResult> Index()
        {
            var (companyId, isAdmin) = await GetCurrentCompanyAndRoleAsync();
            if (!isAdmin || companyId == null) return Forbid();

            var companyUsers = await _context.CompanyUsers
                .Include(cu => cu.User)
                .Include(cu => cu.Company) // Lade till Company Include för att förhindra krasch i vyn
                .Where(cu => cu.CompanyId == companyId)
                .ToListAsync();

            return View(companyUsers);
        }

        // GET: CompanyUser/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var (companyId, isAdmin) = await GetCurrentCompanyAndRoleAsync();
            if (!isAdmin || companyId == null) return Forbid();

            // SÄKERHETSÅTGÄRD: Filtrera även på CompanyId
            var companyUser = await _context.CompanyUsers
                .Include(c => c.Company)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id && m.CompanyId == companyId);

            if (companyUser == null) return NotFound();

            return View(companyUser);
        }

        // GET: CompanyUser/Create
        public async Task<IActionResult> Create()
        {
            var (companyId, isAdmin) = await GetCurrentCompanyAndRoleAsync();
            if (!isAdmin || companyId == null) return Forbid();

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        // POST: CompanyUser/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,Role")] CompanyUser companyUser)
        {
            var (companyId, isAdmin) = await GetCurrentCompanyAndRoleAsync();
            if (!isAdmin || companyId == null) return Forbid();

            ModelState.Remove("Company");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                companyUser.CompanyId = companyId.Value;
                companyUser.CreatedAt = DateTime.UtcNow;

                _context.Add(companyUser);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", companyUser.UserId);
            return View(companyUser);
        }

        // GET: CompanyUser/Edit/5
        // GET: CompanyUser/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var (companyId, isAdmin) = await GetCurrentCompanyAndRoleAsync();
            if (!isAdmin || companyId == null) return Forbid();

            // KOLLA HÄR: .Include(cu => cu.User) är tillagt!
            var companyUser = await _context.CompanyUsers
                .Include(cu => cu.User)
                .FirstOrDefaultAsync(cu => cu.Id == id && cu.CompanyId == companyId);

            if (companyUser == null) return NotFound();

            return View(companyUser);
        }

        // POST: CompanyUser/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Role,UserId,CreatedAt")] CompanyUser companyUser)
        {
            if (id != companyUser.Id) return NotFound();

            var (companyId, isAdmin) = await GetCurrentCompanyAndRoleAsync();
            if (!isAdmin || companyId == null) return Forbid();

            
            ModelState.Remove("Company");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                try
                {
                    companyUser.CompanyId = companyId.Value;

                    _context.Update(companyUser);

                    
                    _context.Entry(companyUser).Property(x => x.CreatedAt).IsModified = false;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CompanyUserExists(companyUser.Id, companyId.Value)) return NotFound();
                    else throw;
                }

                return RedirectToAction(nameof(Index));
            }

            
            companyUser.User = await _context.Users.FindAsync(companyUser.UserId);

            return View(companyUser);
        }

        // GET: CompanyUser/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var (companyId, isAdmin) = await GetCurrentCompanyAndRoleAsync();
            if (!isAdmin || companyId == null) return Forbid();

            var companyUser = await _context.CompanyUsers
                .Include(c => c.User)
                .Include(c => c.Company)
                .FirstOrDefaultAsync(m => m.Id == id && m.CompanyId == companyId);

            if (companyUser == null) return NotFound();

            return View(companyUser);
        }

        // POST: CompanyUser/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (companyId, isAdmin) = await GetCurrentCompanyAndRoleAsync();
            if (!isAdmin || companyId == null) return Forbid();

            var companyUser = await _context.CompanyUsers
                .FirstOrDefaultAsync(cu => cu.Id == id && cu.CompanyId == companyId);

            if (companyUser != null)
            {
                _context.CompanyUsers.Remove(companyUser);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CompanyUserExists(int id, int companyId)
        {
            return _context.CompanyUsers.Any(e => e.Id == id && e.CompanyId == companyId);
        }
    }
}