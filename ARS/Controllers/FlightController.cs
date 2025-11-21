using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ARS.Data;
using ARS.Models;
using ARS.ViewModels;
using Microsoft.AspNetCore.Authorization;

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
        // Consolidated flights page at /Flight (supports filter query parameters)
        public async Task<IActionResult> Index([FromQuery] FlightSearchViewModel? search)
        {
            await PopulateCityDropdowns();

            // Provide defaults when values aren't supplied
            if (search == null)
            {
                search = new FlightSearchViewModel();
            }

            if (search.Passengers < 1)
                search.Passengers = 1;

            if (string.IsNullOrEmpty(search.Class))
                search.Class = "Economy";

            if (search.TravelDate == default)
                search.TravelDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

            // Load flights with related data
            var flightsQuery = _context.Flights
                .Include(f => f.OriginCity)
                .Include(f => f.DestinationCity)
                .Include(f => f.Schedules)
                .Include(f => f.Reservations)
                .AsQueryable();

            // Apply origin/destination filters if provided
            if (search.OriginCityID != 0)
                flightsQuery = flightsQuery.Where(f => f.OriginCityID == search.OriginCityID);
            if (search.DestinationCityID != 0)
                flightsQuery = flightsQuery.Where(f => f.DestinationCityID == search.DestinationCityID);

            // If a travel date is provided, only include flights that have a schedule on that date
            if (search.TravelDate != default)
                flightsQuery = flightsQuery.Where(f => f.Schedules.Any(s => s.Date == search.TravelDate));

            var flights = await flightsQuery.ToListAsync();

            var results = new FlightSearchResultViewModel
            {
                SearchCriteria = search,
                Flights = flights.Select(f =>
                {
                    // Prefer the first schedule on or after the requested travel date, otherwise the earliest
                    var schedule = f.Schedules
                        .Where(s => s.Date >= search.TravelDate)
                        .OrderBy(s => s.Date)
                        .FirstOrDefault()
                        ?? f.Schedules.OrderBy(s => s.Date).FirstOrDefault();

                    // Use schedule date to compute departure/arrival datetimes when available
                    var travelDateForCalc = schedule?.Date ?? search.TravelDate;
                    var departure = schedule != null
                        ? schedule.Date.ToDateTime(TimeOnly.FromDateTime(f.DepartureTime))
                        : f.DepartureTime;
                    var arrival = schedule != null
                        ? schedule.Date.ToDateTime(TimeOnly.FromDateTime(f.ArrivalTime))
                        : f.ArrivalTime;

                    // Calculate booked seats for chosen travel date
                    var bookedSeats = f.Reservations
                        .Where(r => r.TravelDate == travelDateForCalc && r.Status != "Cancelled")
                        .Sum(r => r.NumAdults + r.NumChildren + r.NumSeniors);
                    var availableSeats = f.TotalSeats - bookedSeats;

                    // Price multipliers
                    var basePrice = f.BaseFare;
                    var classMultiplier = search.Class switch
                    {
                        "Business" => 2.0m,
                        "First" => 3.5m,
                        _ => 1.0m
                    };

                    var daysBeforeDeparture = (travelDateForCalc.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days;
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
                        DepartureTime = departure,
                        ArrivalTime = arrival,
                        Duration = f.Duration,
                        AircraftType = f.AircraftType,
                        AvailableSeats = availableSeats,
                        BasePrice = basePrice,
                        FinalPrice = basePrice * classMultiplier * timingMultiplier * search.Passengers,
                        ScheduleID = schedule?.ScheduleID
                    };
                })
                .Where(f => f.AvailableSeats >= search.Passengers)
                .OrderBy(f => f.DepartureTime)
                .ToList()
            };

            return View("All", results);
        }

        // POST: Flight/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(FlightSearchViewModel model)
        {
            // For MultiCity searches we perform custom validation below, so skip the default ModelState blocking.
            if (model.TripType != "MultiCity" && !ModelState.IsValid)
            {
                await PopulateCityDropdowns();
                return View("Index", model);
            }
            var results = new FlightSearchResultViewModel { SearchCriteria = model };

            // Helper to perform a single-leg search and return result list
            async Task<List<FlightResultItem>> DoLegSearchAsync(int originCityId, int destinationCityId, DateOnly travelDate)
            {
                var flights = await _context.Flights
                    .Include(f => f.OriginCity)
                    .Include(f => f.DestinationCity)
                    .Include(f => f.Schedules)
                    .Include(f => f.Reservations)
                    .Where(f => f.OriginCityID == originCityId && f.DestinationCityID == destinationCityId)
                    .ToListAsync();

                return flights.Select(f =>
                {
                    var schedule = f.Schedules.FirstOrDefault(s => s.Date == travelDate);
                    var departure = schedule != null
                        ? schedule.Date.ToDateTime(TimeOnly.FromDateTime(f.DepartureTime))
                        : f.DepartureTime;
                    var arrival = schedule != null
                        ? schedule.Date.ToDateTime(TimeOnly.FromDateTime(f.ArrivalTime))
                        : f.ArrivalTime;

                    var bookedSeats = f.Reservations
                        .Where(r => r.TravelDate == travelDate && r.Status != "Cancelled")
                        .Sum(r => r.NumAdults + r.NumChildren + r.NumSeniors);
                    var availableSeats = f.TotalSeats - bookedSeats;

                    var basePrice = f.BaseFare;
                    var classMultiplier = model.Class switch
                    {
                        "Business" => 2.0m,
                        "First" => 3.5m,
                        _ => 1.0m
                    };
                    var daysBeforeDeparture = (travelDate.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days;
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
                        OriginCity = f.OriginCity?.CityName ?? string.Empty,
                        OriginAirportCode = f.OriginCity?.AirportCode ?? string.Empty,
                        DestinationCity = f.DestinationCity?.CityName ?? string.Empty,
                        DestinationAirportCode = f.DestinationCity?.AirportCode ?? string.Empty,
                        DepartureTime = departure,
                        ArrivalTime = arrival,
                        Duration = f.Duration,
                        AircraftType = f.AircraftType,
                        AvailableSeats = availableSeats,
                        BasePrice = basePrice,
                        FinalPrice = basePrice * classMultiplier * timingMultiplier * model.Passengers,
                        ScheduleID = schedule?.ScheduleID
                    };
                })
                .Where(fr => fr.AvailableSeats >= model.Passengers)
                .OrderBy(fr => fr.DepartureTime)
                .ToList();
            }

            if (model.TripType == "RoundTrip")
            {
                if (!model.ReturnDate.HasValue)
                {
                    ModelState.AddModelError("ReturnDate", "Please select a return date for round-trip searches.");
                    await PopulateCityDropdowns();
                    return View("Index", model);
                }

                results.OutboundFlights = await DoLegSearchAsync(model.OriginCityID, model.DestinationCityID, model.TravelDate);
                results.ReturnFlights = await DoLegSearchAsync(model.DestinationCityID, model.OriginCityID, model.ReturnDate.Value);
            }
            else if (model.TripType == "MultiCity" && model.Legs != null && model.Legs.Any())
            {
                foreach (var leg in model.Legs)
                {
                    var legResults = await DoLegSearchAsync(leg.OriginCityID, leg.DestinationCityID, leg.TravelDate);
                    results.LegsResults.Add(legResults);
                }
            }
            else // OneWay
            {
                results.Flights = await DoLegSearchAsync(model.OriginCityID, model.DestinationCityID, model.TravelDate);
            }

            return View("SearchResults", results);
        }

        // Preserve old /Flight/All links: redirect permanently to /Flight keeping the same query string
        [HttpGet]
        public IActionResult All()
        {
            var qs = Request?.QueryString.HasValue == true ? Request.QueryString.Value : string.Empty;
            return RedirectPermanent($"/Flight{qs}");
        }

        // GET: Flight/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await PopulateCityDropdowns();
            ViewData["PolicyID"] = new SelectList(_context.PricingPolicies, "PolicyID", "Description");
            return View();
        }

        // POST: Flight/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([Bind("FlightNumber,OriginCityID,DestinationCityID,DepartureTime,ArrivalTime,Duration,AircraftType,TotalSeats,BaseFare,PolicyID")] Flight flight)
        {
            if (ModelState.IsValid)
            {
                _context.Add(flight);
                await _context.SaveChangesAsync();
                // Automatically create a Schedule for the newly created flight so it appears in date-based searches.
                // Use the departure date portion of the provided DepartureTime as the schedule date.
                try
                {
                    var scheduleDate = DateOnly.FromDateTime(flight.DepartureTime);

                    var exists = await _context.Schedules
                        .AnyAsync(s => s.FlightID == flight.FlightID && s.Date == scheduleDate);

                    if (!exists)
                    {
                        var schedule = new Schedule
                        {
                            FlightID = flight.FlightID,
                            Date = scheduleDate,
                            Status = "Scheduled"
                        };
                        _context.Schedules.Add(schedule);
                        await _context.SaveChangesAsync();
                    }
                }
                catch
                {
                    // Swallow any schedule-creation errors so flight creation still succeeds.
                    // The admin can always add schedules manually via the Schedule management UI.
                }

                return RedirectToAction(nameof(Index));
            }
            await PopulateCityDropdowns();
            ViewData["PolicyID"] = new SelectList(_context.PricingPolicies, "PolicyID", "Description", flight.PolicyID);
            return View(flight);
        }

        // GET: Flight/Edit/5
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
