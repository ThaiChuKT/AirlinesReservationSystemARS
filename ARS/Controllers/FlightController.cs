using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ARS.Data;
using ARS.Models;
using ARS.ViewModels;

namespace ARS.Controllers
{
    public class FlightController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FlightController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Flight
        public async Task<IActionResult> Index()
        {
            var model = new FlightSearchViewModel();
            await PopulateCityDropdowns();
            return View(model);
        }

        // POST: Flight/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(FlightSearchViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCityDropdowns();
                return View("Index", model);
            }

            // Get all flights matching the route
            var flights = await _context.Flights
                .Include(f => f.OriginCity)
                .Include(f => f.DestinationCity)
                .Include(f => f.Schedules)
                .Include(f => f.Reservations)
                .Where(f => f.OriginCityID == model.OriginCityID 
                         && f.DestinationCityID == model.DestinationCityID)
                .ToListAsync();

            var results = new FlightSearchResultViewModel
            {
                SearchCriteria = model,
                Flights = flights.Select(f => {
                    // Find schedule for the travel date
                    var schedule = f.Schedules.FirstOrDefault(s => s.Date == model.TravelDate);
                    
                    // Calculate available seats
                    var bookedSeats = f.Reservations
                        .Where(r => r.TravelDate == model.TravelDate && r.Status != "Cancelled")
                        .Sum(r => r.NumAdults + r.NumChildren + r.NumSeniors);
                    var availableSeats = f.TotalSeats - bookedSeats;

                    // Calculate price based on class
                    var basePrice = f.BaseFare;
                    var classMultiplier = model.Class switch
                    {
                        "Business" => 2.0m,
                        "First" => 3.5m,
                        _ => 1.0m
                    };

                    // Calculate price based on booking time (days before departure)
                    var daysBeforeDeparture = (model.TravelDate.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days;
                    var timingMultiplier = daysBeforeDeparture switch
                    {
                        >= 30 => 0.80m,
                        >= 15 => 1.00m,
                        >= 7 => 1.20m,
                        _ => 1.50m
                    };

                    return new FlightResultItem
                    {
                        FlightID = f.FlightID,
                        FlightNumber = f.FlightNumber,
                        OriginCity = f.OriginCity?.CityName ?? "",
                        OriginAirportCode = f.OriginCity?.AirportCode ?? "",
                        DestinationCity = f.DestinationCity?.CityName ?? "",
                        DestinationAirportCode = f.DestinationCity?.AirportCode ?? "",
                        DepartureTime = f.DepartureTime,
                        ArrivalTime = f.ArrivalTime,
                        Duration = f.Duration,
                        AircraftType = f.AircraftType,
                        AvailableSeats = availableSeats,
                        BasePrice = basePrice,
                        FinalPrice = basePrice * classMultiplier * timingMultiplier * model.Passengers,
                        ScheduleID = schedule?.ScheduleID
                    };
                })
                .Where(f => f.AvailableSeats >= model.Passengers)
                .OrderBy(f => f.DepartureTime)
                .ToList()
            };

            return View("SearchResults", results);
        }

        // GET: Flight/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flight = await _context.Flights
                .Include(f => f.OriginCity)
                .Include(f => f.DestinationCity)
                .Include(f => f.PricingPolicy)
                .FirstOrDefaultAsync(m => m.FlightID == id);

            if (flight == null)
            {
                return NotFound();
            }

            return View(flight);
        }

        // GET: Flight/Create
        public async Task<IActionResult> Create()
        {
            await PopulateCityDropdowns();
            ViewData["PolicyID"] = new SelectList(_context.PricingPolicies, "PolicyID", "Description");
            return View();
        }

        // POST: Flight/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FlightNumber,OriginCityID,DestinationCityID,DepartureTime,ArrivalTime,Duration,AircraftType,TotalSeats,BaseFare,PolicyID")] Flight flight)
        {
            if (ModelState.IsValid)
            {
                _context.Add(flight);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateCityDropdowns();
            ViewData["PolicyID"] = new SelectList(_context.PricingPolicies, "PolicyID", "Description", flight.PolicyID);
            return View(flight);
        }

        // GET: Flight/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flight = await _context.Flights.FindAsync(id);
            if (flight == null)
            {
                return NotFound();
            }
            await PopulateCityDropdowns();
            ViewData["PolicyID"] = new SelectList(_context.PricingPolicies, "PolicyID", "Description", flight.PolicyID);
            return View(flight);
        }

        // POST: Flight/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FlightID,FlightNumber,OriginCityID,DestinationCityID,DepartureTime,ArrivalTime,Duration,AircraftType,TotalSeats,BaseFare,PolicyID")] Flight flight)
        {
            if (id != flight.FlightID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(flight);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FlightExists(flight.FlightID))
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
            await PopulateCityDropdowns();
            ViewData["PolicyID"] = new SelectList(_context.PricingPolicies, "PolicyID", "Description", flight.PolicyID);
            return View(flight);
        }

        // GET: Flight/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flight = await _context.Flights
                .Include(f => f.OriginCity)
                .Include(f => f.DestinationCity)
                .Include(f => f.PricingPolicy)
                .FirstOrDefaultAsync(m => m.FlightID == id);

            if (flight == null)
            {
                return NotFound();
            }

            return View(flight);
        }

        // POST: Flight/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight != null)
            {
                _context.Flights.Remove(flight);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool FlightExists(int id)
        {
            return _context.Flights.Any(e => e.FlightID == id);
        }

        private async Task PopulateCityDropdowns()
        {
            var cities = await _context.Cities.OrderBy(c => c.CityName).ToListAsync();
            ViewData["Cities"] = new SelectList(cities, "CityID", "CityName");
            ViewData["OriginCityID"] = new SelectList(cities, "CityID", "CityName");
            ViewData["DestinationCityID"] = new SelectList(cities, "CityID", "CityName");
        }
    }
}
