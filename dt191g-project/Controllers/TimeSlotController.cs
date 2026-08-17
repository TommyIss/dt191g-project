using dt191g_project.Data;
using dt191g_project.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dt191g_project.Controllers
{
    public class TimeSlotController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TimeSlotController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TimeSlot?companyId=1
        public async Task<IActionResult> Index(int? companyId)
        {
            if (companyId == null)
            {
                return NotFound();
            }

            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
            {
                return NotFound();
            }

            var timeSlots = await _context.TimeSlots
                .Include(t => t.Company)
                .Include(t => t.Service)
                .Where(t => t.CompanyId == companyId)
                .OrderBy(t => t.StartTime)
                .ToListAsync();

            ViewBag.Company = company;
            ViewBag.CompanyId = companyId;

            return View(timeSlots);
        }

        // GET: TimeSlot/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeSlot = await _context.TimeSlots
                .Include(t => t.Company)
                .Include(t => t.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (timeSlot == null)
            {
                return NotFound();
            }

            return View(timeSlot);
        }

        // GET: TimeSlot/Generate?companyId=1
        public async Task<IActionResult> Generate(int companyId)
        {
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
            {
                return NotFound();
            }

            var services = await _context.Services
                .Where(s => s.CompanyId == companyId && s.IsActive)
                .ToListAsync();

            if (!services.Any())
            {
                TempData["ErrorMessage"] = "Skapa minst en aktiv tjänst för företaget innan du genererar tidsluckor.";
                return RedirectToAction("Index", new { companyId });
            }

            var model = new TimeSlotGenerator
            {
                CompanyId = companyId,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7),
                OpeningTime = new TimeSpan(8, 0, 0),
                ClosingTime = new TimeSpan(17, 0, 0),
                IntervalMinutes = 30
            };

            ViewBag.Services = services;
            return View(model);
        }

        // POST: TimeSlot/Generate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(TimeSlotGenerator model)
        {
            var service = await _context.Services.FindAsync(model.ServiceId);

            // Om tjänsten inte finns eller inte tillhör angivet företag
            if (service == null)
            {
                ModelState.AddModelError("ServiceId", "Du måste välja en giltig tjänst.");
            }
            else
            {
                // Sätt CompanyId automatiskt från tjänsten om den saknas i model
                if (model.CompanyId == 0)
                {
                    model.CompanyId = service.CompanyId;
                }
            }

            if (model.ClosingTime <= model.OpeningTime)
            {
                ModelState.AddModelError("ClosingTime", "Stängningstid måste vara efter öppningstid.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Services = await _context.Services
                    .Where(s => s.CompanyId == model.CompanyId && s.IsActive)
                    .ToListAsync();

                return View(model);
            }

            if (service == null || service.CompanyId != model.CompanyId)
            {
                return BadRequest("Ogiltig tjänst för det valda företaget.");
            }

            int intervalMinutes = model.IntervalMinutes > 0 ? model.IntervalMinutes : service.DurationMinutes;

            var slots = new List<TimeSlot>();
            DateTime currentDate = model.StartDate.Date;

            while (currentDate <= model.EndDate.Date)
            {
                var start = currentDate.Add(model.OpeningTime);
                var end = currentDate.Add(model.ClosingTime);

                while (start.AddMinutes(intervalMinutes) <= end)
                {
                    slots.Add(new TimeSlot
                    {
                        CompanyId = model.CompanyId,
                        ServiceId = model.ServiceId,
                        StartTime = start,
                        EndTime = start.AddMinutes(intervalMinutes),
                        IsBooked = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });

                    start = start.AddMinutes(intervalMinutes);
                }

                currentDate = currentDate.AddDays(1);
            }

            if (slots.Any())
            {
                _context.TimeSlots.AddRange(slots);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"{slots.Count} tidsluckor skapades för tjänsten \"{service.Title}\".";
            }
            else
            {
                TempData["ErrorMessage"] = "Inga tidsluckor kunde skapas med de angivna tidsintervallen.";
            }

            return RedirectToAction("Index", new { companyId = model.CompanyId });
        }

        // GET: TimeSlot/Create?companyId=1
        public async Task<IActionResult> Create(int companyId)
        {
            var services = await _context.Services
                .Where(s => s.CompanyId == companyId && s.IsActive)
                .ToListAsync();

            var model = new TimeSlot
            {
                CompanyId = companyId,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1)
            };

            // VIKTIGT: Sätt SelectList här
            ViewBag.Services = new SelectList(services, "Id", "Title");
            return View(model);
        }

        // POST: TimeSlot/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CompanyId,ServiceId,StartTime,EndTime,IsBooked")] TimeSlot timeSlot)
        {
            ModelState.Remove("Service");
            ModelState.Remove("Company");
            ModelState.Remove("Bookings");

            if (ModelState.IsValid)
            {
                timeSlot.CreatedAt = DateTime.UtcNow;
                timeSlot.UpdatedAt = DateTime.UtcNow;

                _context.Add(timeSlot);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", new { companyId = timeSlot.CompanyId });
            }

            // VIKTIGT: Ladda om ViewBag.Services om valideringen misslyckades!
            var services = await _context.Services
                .Where(s => s.CompanyId == timeSlot.CompanyId && s.IsActive)
                .ToListAsync();

            ViewBag.Services = new SelectList(services, "Id", "Title", timeSlot.ServiceId);

            return View(timeSlot);
        }

        // GET: TimeSlot/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeSlot = await _context.TimeSlots
                .Include(t => t.Service)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (timeSlot == null)
            {
                return NotFound();
            }

            ViewBag.Services = new SelectList(
                await _context.Services.Where(s => s.CompanyId == timeSlot.CompanyId && s.IsActive).ToListAsync(),
                "Id",
                "Title",
                timeSlot.ServiceId
            );

            return View(timeSlot);
        }

        // POST: TimeSlot/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CompanyId,ServiceId,StartTime,EndTime,IsBooked")] TimeSlot timeSlot)
        {
            if (id != timeSlot.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Service");
            ModelState.Remove("Company");
            ModelState.Remove("Bookings");

            if (ModelState.IsValid)
            {
                try
                {
                    timeSlot.UpdatedAt = DateTime.UtcNow;

                    _context.Update(timeSlot);
                    _context.Entry(timeSlot).Property(x => x.CreatedAt).IsModified = false;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TimeSlotExists(timeSlot.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index), new { companyId = timeSlot.CompanyId });
            }

            ViewBag.Services = new SelectList(
                await _context.Services.Where(s => s.CompanyId == timeSlot.CompanyId && s.IsActive).ToListAsync(),
                "Id",
                "Title",
                timeSlot.ServiceId
            );

            return View(timeSlot);
        }

        // GET: TimeSlot/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeSlot = await _context.TimeSlots
                .Include(t => t.Company)
                .Include(t => t.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (timeSlot == null)
            {
                return NotFound();
            }

            return View(timeSlot);
        }

        // POST: TimeSlot/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var timeSlot = await _context.TimeSlots.FindAsync(id);
            int companyId = 0;

            if (timeSlot != null)
            {
                companyId = timeSlot.CompanyId;
                _context.TimeSlots.Remove(timeSlot);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { companyId });
        }

        private bool TimeSlotExists(int id)
        {
            return _context.TimeSlots.Any(e => e.Id == id);
        }
    }
}