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
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Booking
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Service)
                .Include(b => b.TimeSlot);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Booking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Service)
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // GET: Booking/Create
        [Authorize]
        public IActionResult Create()
        {
            // Endast tjänster laddas vid start – tidsluckor hämtas dynamiskt via AJAX
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Title");
            return View();
        }

        // POST: Booking/Create
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceId,TimeSlotId,CustomerName,CustomerEmail,CustomerPhone,Notes")] Booking booking)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Sätt system- och användarfält
            booking.CustomerId = userId;
            booking.UserId = userId;
            booking.Status = "Bekräftad";
            booking.CreatedAt = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;

            // 2. Rensa ALLA fält i ModelState som inte skickas direkt från HTML-formuläret
            ModelState.Remove("CustomerId");
            ModelState.Remove("UserId");
            ModelState.Remove("Status");
            ModelState.Remove("Customer");
            ModelState.Remove("Service");
            ModelState.Remove("TimeSlot");
            ModelState.Remove("User");

            // 3. Om valideringen fortfarande misslyckas, logga felet i konsolen för felsökning
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    System.Diagnostics.Debug.WriteLine($"ModelState Fel: {error}");
                }
            }

            if (ModelState.IsValid)
            {
                // Markera tidsluckan som bokad
                var timeSlot = await _context.TimeSlots.FindAsync(booking.TimeSlotId);
                if (timeSlot != null)
                {
                    timeSlot.IsBooked = true;
                }

                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Om valideringen misslyckades: Ladda om dropdowns så att sidan inte kraschar
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Title", booking.ServiceId);
            return View(booking);
        }

        // GET: Booking/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Title", booking.ServiceId);

            return View(booking);
        }

        // AJAX ENDPOINT: Anropas av skriptet i Edit-vyn
        [HttpGet]
        public async Task<IActionResult> GetTimeSlotsByService(int serviceId, int? currentSlotId)
        {
            var timeSlots = await _context.TimeSlots
                .Where(t => t.ServiceId == serviceId && (!t.IsBooked || t.Id == currentSlotId))
                .OrderBy(t => t.StartTime)
                .ToListAsync();

            var result = timeSlots.Select(t => new
            {
                id = t.Id,
                displayText = $"{t.StartTime:yyyy-MM-dd HH:mm} - {t.EndTime:HH:mm}"
            });

            return Json(result);
        }

        // POST: Booking/Edit/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerId,UserId,ServiceId,TimeSlotId,CustomerName,CustomerEmail,CustomerPhone,Notes,Status,CreatedAt")] Booking booking)
        {
            if (id != booking.Id) return NotFound();

            // Ta bort validering på navigeringsegenskaper som inte finns i formuläret
            ModelState.Remove("Customer");
            ModelState.Remove("Service");
            ModelState.Remove("TimeSlot");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                try
                {
                    // Hämta den ursprungliga bokningen (utan tracking) för att kontrollera om tidsluckan ändrats
                    var originalBooking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);

                    if (originalBooking != null && originalBooking.TimeSlotId != booking.TimeSlotId)
                    {
                        // 1. Friställ den gamla tidsluckan
                        var oldSlot = await _context.TimeSlots.FindAsync(originalBooking.TimeSlotId);
                        if (oldSlot != null)
                        {
                            oldSlot.IsBooked = false;
                        }

                        // 2. Markera den nya tidsluckan som bokad
                        var newSlot = await _context.TimeSlots.FindAsync(booking.TimeSlotId);
                        if (newSlot != null)
                        {
                            newSlot.IsBooked = true;
                        }
                    }

                    booking.UpdatedAt = DateTime.Now;
                    _context.Update(booking);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.Id)) return NotFound();
                    else throw;
                }
            }

            // Om valideringen misslyckades: Återställ Service-dropdownen
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Title", booking.ServiceId);

            return View(booking);
        }

        // GET: Booking/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Service)
                .Include(b => b.TimeSlot)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // POST: Booking/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                // Friställ tidsluckan när bokningen tas bort
                var slot = await _context.TimeSlots.FindAsync(booking.TimeSlotId);
                if (slot != null)
                {
                    slot.IsBooked = false;
                }

                _context.Bookings.Remove(booking);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }
    }
}