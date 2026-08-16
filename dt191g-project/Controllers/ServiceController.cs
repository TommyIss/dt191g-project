using dt191g_project.Data;
using dt191g_project.Models;
using dt191g_project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace dt191g_project.Controllers
{
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ServiceController(ApplicationDbContext context, EmailService emailService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
        }

        // GET: Service
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Services.Include(s => s.Company);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Service/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services
                .Include(s => s.Company)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        // GET: Service/Create
        public IActionResult Create(int? companyId)
        {
            if (companyId == null)
            {
                return NotFound();
            }

            ViewBag.CompanyId = companyId;
            return View();
        }

        // POST: Service/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CompanyId,Title,Description,DurationMinutes,Price,Category,IsActive")] Service service)
        {
            // Ta bort validering för relationen till Company
            ModelState.Remove("Company");
            ModelState.Remove("Bookings");

            if (ModelState.IsValid)
            {
                service.CreatedAt = DateTime.Now;
                service.UpdatedAt = DateTime.Now;

                _context.Add(service);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "CompanyDashboard", new { companyId = service.CompanyId });
            }

            ViewBag.CompanyId = service.CompanyId;
            return View(service);
        }

        // GET: Service/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Id", service.CompanyId);
            return View(service);
        }

        // POST: Service/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CompanyId,Title,Description,DurationMinutes,Price,Category,IsActive,CreatedAt,UpdatedAt")] Service service)
        {
            if (id != service.Id)
            {
                return NotFound();
            }

            // Förhindra att ModelState misslyckas på navigationsegenskaper
            ModelState.Remove("Company");
            ModelState.Remove("Bookings");

            if (ModelState.IsValid)
            {
                try
                {
                    // Sätt uppdateringstiden automatiskt i bakkoden
                    service.UpdatedAt = DateTime.Now;

                    _context.Update(service);

                    // Säkerställ att ursprungligt skapandedatum bevaras
                    _context.Entry(service).Property(x => x.CreatedAt).IsModified = false;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(service.Id))
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
            ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Id", service.CompanyId);
            return View(service);
        }

        // Visa Tidsluckor för en specifik tjänst
        public IActionResult TimeSlots(int serviceId)
        {
            var service = _context.Services
                .Include(s => s.Company)
                .FirstOrDefault(s => s.Id == serviceId);

            if (service == null)
                return NotFound();

            var slots = _context.TimeSlots
                .Where(t => t.ServiceId == serviceId && !t.IsBooked)
                .OrderBy(t => t.StartTime)
                .ToList();

            ViewBag.Service = service;

            return View(slots);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Book(int timeSlotId)
        {
            var timeSlot = await _context.TimeSlots
                .Include(t => t.Service)
                .Include(t => t.Company)
                .FirstOrDefaultAsync(t => t.Id == timeSlotId);

            if (timeSlot == null || timeSlot.IsBooked)
            {
                return NotFound();
            }

            
            return View(timeSlot);
        }

        // Bekräfta bokning av en Tidslucka
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> BookConfirmed(int timeSlotId, string? notes)
        {
            var slot = await _context.TimeSlots
                .Include(t => t.Service)
                .Include(t => t.Company)
                .FirstOrDefaultAsync(t => t.Id == timeSlotId);

            if (slot == null || slot.IsBooked)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var userId = user.Id;
            var userEmail = user.Email ?? "Ej angiven";
            var userName = user.UserName ?? "Ej angiven";

            var booking = new Booking
            {
                TimeSlotId = timeSlotId,
                ServiceId = slot.ServiceId,
                CustomerId = userId,
                CustomerName = userName,
                CustomerEmail = userEmail,
                CustomerPhone = "",
                Notes = notes ?? "",
                Status = "Bekräftad",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            slot.IsBooked = true;

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Skicka bekräftelsemail
            await _emailService.SendBookingConfirmation(
                booking,
                slot,
                slot.Service,
                slot.Company,
                userEmail
            );


            return RedirectToAction("MyBookings");
        }

        // Visa användarens bokningar
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);

            var bookings = await _context.Bookings
                .Include(b => b.TimeSlot)
                .Include(b => b.Service)
                    .ThenInclude(s => s.Company)
                .Where(b => (b.CustomerId == userId || b.UserId == userId)
                    && b.Status != "Avbokad")
                .ToListAsync();

            return View(bookings);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var userId = _userManager.GetUserId(User)
                         ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            var booking = await _context.Bookings
                .Include(b => b.TimeSlot)
                .ThenInclude(ts => ts.Company)
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == bookingId && (b.CustomerId == userId || b.UserId == userId));

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.TimeSlot != null)
            {
                booking.TimeSlot.IsBooked = false;
            }

            booking.Status = "Avbokad";
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var customerEmail = booking.CustomerEmail;

            await _emailService.SendCancellationConfirmation(
                booking,
                booking.TimeSlot,
                booking.Service,
                booking.TimeSlot.Company,
                booking.CustomerEmail
            );

            return RedirectToAction("MyBookings");
        }


        // GET: Service/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services
                .Include(s => s.Company)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        // POST: Service/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                _context.Services.Remove(service);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // Tjänstfilter
        public IActionResult Search(ServiceFilterModel filter)
        {
            var query = _context.Services
                .Include(s => s.Company)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.City))
                query = query.Where(s => s.Company.City.Contains(filter.City));

            if (!string.IsNullOrEmpty(filter.Category))
                query = query.Where(s => s.Company.Category == filter.Category);

            if (!string.IsNullOrEmpty(filter.CompanyName))
                query = query.Where(s => s.Company.Name.Contains(filter.CompanyName));

            if (filter.MinPrice.HasValue)
                query = query.Where(s => s.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(s => s.Price <= filter.MaxPrice.Value);

            if (filter.DurationMinutes.HasValue)
                query = query.Where(s => s.DurationMinutes == filter.DurationMinutes.Value);

            var results = query.ToList();

            ViewBag.Filter = filter;

            return View(results);
        }


        private bool ServiceExists(int id)
        {
            return _context.Services.Any(e => e.Id == id);
        }
    }
}
