using dt191g_project.Data;
using dt191g_project.Models;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private Exception ex;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hjälpmetod för att hämta CompanyId för den inloggade användaren
        private async Task<int?> GetUserCompanyIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;

            // 1. Sök i Companies-tabellen efter företag som ägs av användaren
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company != null)
            {
                return company.Id;
            }

            // 2. Om ingen träff finns där, kontrollera om användaren har en CompanyId-egenskap i ApplicationUser
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return user?.CompanyId;
        }

        // AJAX: Hämtar tidsluckor för den valda tjänsten
        [HttpGet]
        public async Task<IActionResult> GetTimeSlotsByService(int serviceId, int? currentSlotId)
        {
            // HÄMTA ALLA TIDER FÖR TJÄNSTEN UTAN NÅGRA VILLKOR ALLS
            var slots = await _context.TimeSlots
                .Where(t => t.ServiceId == serviceId)
                .Select(t => new
                {
                    id = t.Id,
                    displayText = $"{t.StartTime:yyyy-MM-dd HH:mm} - {t.EndTime:HH:mm}"
                })
                .ToListAsync();

            return Json(slots);
        }

        // GET: Booking/Index
        [HttpGet]
        public async Task<IActionResult> Index(int? companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Om inget companyId skickas med, hämta via hjälpmetoden
            int activeCompanyId = (companyId.HasValue && companyId.Value > 0)
                ? companyId.Value
                : (await GetUserCompanyIdAsync() ?? 0);

            // 2. Om vi fortfarande inte har ett företag, visa tom lista
            if (activeCompanyId == 0)
            {
                ViewBag.CompanyName = "Inget företag valt";
                ViewBag.CompanyId = 0;
                return View(new List<Booking>());
            }

            // 3. Hämta bokningar för det aktiva företaget
            var bookings = await _context.Bookings
                .Include(b => b.Service)
                .Include(b => b.TimeSlot)
                .ToListAsync();

            ViewBag.CompanyId = activeCompanyId;
            ViewBag.CompanyName = await _context.Companies
                .Where(c => c.Id == activeCompanyId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();

            return View(bookings);
        }

        // GET: Booking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Service)
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            return View(booking); // ViewBag.CompanyId behövs inte längre här
        }

        // GET: Booking/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? companyId)
        {
            // 1. Hämta företags-ID för den inloggade användaren
            var userCompanyId = await GetUserCompanyIdAsync();

            // 2. Om inkommande companyId är null eller 0, använd den inloggade användarens företags-ID
            int activeCompanyId = (companyId.HasValue && companyId.Value > 0)
                ? companyId.Value
                : (userCompanyId ?? 0);

            // Om vi fortfarande inte har ett giltigt ID, kan användaren/företaget inte identifieras
            if (activeCompanyId == 0)
            {
                ViewBag.ServiceId = new SelectList(Enumerable.Empty<SelectListItem>());
                ViewBag.CompanyId = 0;
                return View();
            }

            // 3. Hämta enbart det aktiva företagets tjänster
            var services = await _context.Services
                .Where(s => s.CompanyId == activeCompanyId)
                .OrderBy(s => s.Title)
                .ToListAsync();

            ViewBag.ServiceId = new SelectList(services, "Id", "Title");
            ViewBag.CompanyId = activeCompanyId;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    [Bind("ServiceId,TimeSlotId,CustomerName,CustomerEmail,CustomerPhone,Notes")] Booking booking, int companyId)
        {
            // Rensa bort egenskaper som inte finns i formuläret från validering
            ModelState.Remove(nameof(Booking.CustomerId));
            ModelState.Remove(nameof(Booking.CompanyId));
            ModelState.Remove(nameof(Booking.Customer));
            ModelState.Remove(nameof(Booking.Service));
            ModelState.Remove(nameof(Booking.TimeSlot));
            ModelState.Remove(nameof(Booking.UserId));
            ModelState.Remove(nameof(Booking.Status));

            if (ModelState.IsValid)
            {
                var slot = await _context.TimeSlots.FindAsync(booking.TimeSlotId);
                if (slot == null || slot.IsBooked)
                {
                    ModelState.AddModelError("TimeSlotId", "Den valda tiden är inte längre tillgänglig.");
                }
                else
                {
                    booking.CompanyId = companyId;
                    booking.Status = "Bekräftad";
                    booking.CreatedAt = DateTime.UtcNow;
                    booking.UpdatedAt = DateTime.UtcNow;

                    slot.IsBooked = true;
                    slot.UpdatedAt = DateTime.UtcNow;

                    _context.Add(booking);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index", new { companyId });
                }
            }

            // Vid fel: Ladda om tjänsterna för rätt företag
            var services = await _context.Services
                .Where(s => s.CompanyId == companyId)
                .OrderBy(s => s.Title)
                .ToListAsync();

            ViewBag.ServiceId = new SelectList(services, "Id", "Title", booking.ServiceId);
            ViewBag.CompanyId = companyId;

            return View(booking);
        }

        // GET: Booking/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            var services = await _context.Services
                .Where(s => s.CompanyId == booking.CompanyId)
                .OrderBy(s => s.Title)
                .ToListAsync();

            ViewBag.ServiceId = new SelectList(services, "Id", "Title", booking.ServiceId);
            // ViewBag.CompanyId behövs inte längre för "Tillbaka"-länken (den läser @Model.CompanyId),
            // men lämnas kvar ifall du använder den till annat i vyn (t.ex. dolt fält eller JS).
            ViewBag.CompanyId = booking.CompanyId;

            return View(booking);
        }

        // POST: Booking/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,ServiceId,TimeSlotId,CustomerName,CustomerEmail,CustomerPhone,Notes")] Booking booking)
        {
            if (id != booking.Id) return NotFound();

            ModelState.Remove(nameof(Booking.CustomerId));
            ModelState.Remove(nameof(Booking.CompanyId));
            ModelState.Remove(nameof(Booking.Customer));
            ModelState.Remove(nameof(Booking.Service));
            ModelState.Remove(nameof(Booking.TimeSlot));
            ModelState.Remove(nameof(Booking.UserId));
            ModelState.Remove(nameof(Booking.Status));

            // Hämta original från databasen - lita ALDRIG på inskickat CompanyId för behörighet
            var existingBooking = await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (existingBooking == null) return NotFound();

            var companyId = existingBooking.CompanyId; // äkta värde, inte formulärdata

            async Task<IActionResult> RedisplayAsync()
            {
                var services = await _context.Services
                    .Where(s => s.CompanyId == companyId)
                    .OrderBy(s => s.Title)
                    .ToListAsync();

                ViewBag.ServiceId = new SelectList(services, "Id", "Title", booking.ServiceId);
                ViewBag.CompanyId = companyId;

                return View(booking);
            }

            if (!ModelState.IsValid)
                return await RedisplayAsync();

            // Verifiera att vald tjänst faktiskt tillhör detta företag
            var serviceValid = await _context.Services
                .AnyAsync(s => s.Id == booking.ServiceId && s.CompanyId == companyId);

            if (!serviceValid)
            {
                ModelState.AddModelError("ServiceId", "Ogiltig tjänst vald.");
                return await RedisplayAsync();
            }

            TimeSlot? oldSlot = null;
            TimeSlot? newSlot = null;

            if (existingBooking.TimeSlotId != booking.TimeSlotId)
            {
                newSlot = await _context.TimeSlots.FindAsync(booking.TimeSlotId);

                // Verifiera att den nya tiden är ledig OCH hör till rätt företag/tjänst
                if (newSlot == null
                    || newSlot.IsBooked
                    || newSlot.CompanyId != companyId
                    || newSlot.ServiceId != booking.ServiceId)
                {
                    ModelState.AddModelError("TimeSlotId", "Den valda tiden är inte längre tillgänglig.");
                    return await RedisplayAsync();
                }

                oldSlot = await _context.TimeSlots.FindAsync(existingBooking.TimeSlotId);
            }

            try
            {
                if (oldSlot != null)
                {
                    oldSlot.IsBooked = false;
                    oldSlot.UpdatedAt = DateTime.UtcNow;
                }

                if (newSlot != null)
                {
                    newSlot.IsBooked = true;
                    newSlot.UpdatedAt = DateTime.UtcNow;
                }

                booking.CompanyId = companyId;
                booking.CustomerId = existingBooking.CustomerId;
                booking.UserId = existingBooking.UserId;
                booking.Status = existingBooking.Status;
                booking.CreatedAt = existingBooking.CreatedAt;
                booking.UpdatedAt = DateTime.UtcNow;

                _context.Update(booking);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookingExists(booking.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction("Index", new { companyId });
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }

        // GET: Booking/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Service)
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            return View(booking); // ViewBag.CompanyId behövs inte längre här
        }

        // POST: Booking/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                int companyId = booking.CompanyId;

                // Frigör tidsluckan när bokningen tas bort
                var timeSlot = await _context.TimeSlots.FindAsync(booking.TimeSlotId);
                if (timeSlot != null)
                {
                    timeSlot.IsBooked = false;
                    timeSlot.UpdatedAt = DateTime.UtcNow;
                }

                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { companyId });
            }

            return RedirectToAction("Index");
        }

        
    }
}