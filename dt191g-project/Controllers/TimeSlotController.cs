using dt191g_project.Data;
using dt191g_project.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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

        // GET: TimeSlot
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.TimeSlots.Include(t => t.Company);
            return View(await applicationDbContext.ToListAsync());
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
                .FirstOrDefaultAsync(m => m.Id == id);
            if (timeSlot == null)
            {
                return NotFound();
            }

            return View(timeSlot);
        }

        // Get: TimeSlot/GenrateTimeSlots/5
        public IActionResult Generate(int companyId)
        {
            var services = _context.Services
                .Where(s => s.CompanyId == companyId)
                .ToList();

            var model = new TimeSlotGenerator
            {
                CompanyId = companyId
            };

            ViewBag.Services = services;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(TimeSlotGenerator model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var slots = new List<TimeSlot>();

            DateTime currentDate = model.StartDate;

            while (currentDate <= model.EndDate)
            {
                var start = currentDate.Date + model.OpeningTime;
                var end = currentDate.Date + model.ClosingTime;

                while (start < end)
                {
                    slots.Add(new TimeSlot
                    {
                        CompanyId = model.CompanyId,
                        ServiceId = model.ServiceId,   
                        StartTime = start,
                        EndTime = start.AddMinutes(model.IntervalMinutes),
                        IsBooked = false,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });

                    start = start.AddMinutes(model.IntervalMinutes);
                }

                currentDate = currentDate.AddDays(1);
            }

            _context.TimeSlots.AddRange(slots);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"{slots.Count} tidsluckor skapades.";

            return RedirectToAction("Index", "CompanyDashboard", new { companyId = model.CompanyId });
        }




        // GET: TimeSlot/Create
        public IActionResult Create(int companyId)
        {
            var services = _context.Services
                .Where(s => s.CompanyId == companyId)
                .ToList();

            var model = new TimeSlot
            {
                CompanyId = companyId
            };

            ViewBag.Services = services;

            return View(model);
        }


        // POST: TimeSlot/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CompanyId,ServiceId,StartTime,EndTime,IsBooked")] TimeSlot timeSlot)
        {
            // Rensa bort valideringen för objekt-relationerna
            ModelState.Remove("Service");
            ModelState.Remove("Company");
            ModelState.Remove("Bookings");

            if (ModelState.IsValid)
            {
                timeSlot.CreatedAt = DateTime.Now;
                timeSlot.UpdatedAt = DateTime.Now;

                _context.Add(timeSlot);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", new { companyId = timeSlot.CompanyId });
            }

            ViewBag.Services = _context.Services
                .Where(s => s.CompanyId == timeSlot.CompanyId)
                .ToList();

            return View(timeSlot);
        }



        // GET: TimeSlot/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeSlot = await _context.TimeSlots.FindAsync(id);
            if (timeSlot == null)
            {
                return NotFound();
            }

            // Hämta tjänster som hör till samma företag för dropdownen
            ViewBag.Services = new SelectList(
                _context.Services.Where(s => s.CompanyId == timeSlot.CompanyId),
                "Id",
                "Title",
                timeSlot.ServiceId
            );

            return View(timeSlot);
        }

        // POST: TimeSlot/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CompanyId,ServiceId,StartTime,EndTime,IsBooked,CreatedAt")] TimeSlot timeSlot)
        {
            if (id != timeSlot.Id)
            {
                return NotFound();
            }

            // Ta bort valideringskrav för navigationsegenskaper
            ModelState.Remove("Service");
            ModelState.Remove("Company");
            ModelState.Remove("Bookings");

            if (ModelState.IsValid)
            {
                try
                {
                    // Sätt uppdateringstiden automatiskt
                    timeSlot.UpdatedAt = DateTime.Now;

                    _context.Update(timeSlot);

                    // Förhindra att ursprungligt skapandedatum ändras
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
                _context.Services.Where(s => s.CompanyId == timeSlot.CompanyId),
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
            if (timeSlot != null)
            {
                _context.TimeSlots.Remove(timeSlot);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TimeSlotExists(int id)
        {
            return _context.TimeSlots.Any(e => e.Id == id);
        }
    }
}
