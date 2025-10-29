using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ARS.Data;
using ARS.Models;
using ARS.ViewModels;

namespace ARS.Controllers
{
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reservation/Create
        public async Task<IActionResult> Create(int flightId, int? scheduleId, DateOnly travelDate, int numAdults = 1, string classType = "Economy")
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Please login to book a flight.";
                return RedirectToAction("Login", "Account", new { returnUrl = $"/Reservation/Create?flightId={flightId}&scheduleId={scheduleId}&travelDate={travelDate}&numAdults={numAdults}&classType={classType}" });
            }

            var flight = await _context.Flights
                .Include(f => f.OriginCity)
                .Include(f => f.DestinationCity)
                .FirstOrDefaultAsync(f => f.FlightID == flightId);

            if (flight == null)
            {
                return NotFound();
            }

            // Get logged-in user information
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            // Calculate pricing
            var daysBeforeDeparture = (travelDate.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days;
            var timingMultiplier = daysBeforeDeparture switch
            {
                >= 30 => 0.80m,
                >= 15 => 1.00m,
                >= 7 => 1.20m,
                _ => 1.50m
            };

            var classMultiplier = classType switch
            {
                "Business" => 2.0m,
                "First" => 3.5m,
                _ => 1.0m
            };

            var basePrice = flight.BaseFare * classMultiplier * timingMultiplier;

            var model = new BookingViewModel
            {
                FlightID = flightId,
                ScheduleID = scheduleId,
                FlightNumber = flight.FlightNumber,
                Origin = flight.OriginCity?.CityName ?? "",
                Destination = flight.DestinationCity?.CityName ?? "",
                DepartureTime = flight.DepartureTime,
                ArrivalTime = flight.ArrivalTime,
                TravelDate = travelDate,
                NumAdults = numAdults,
                Class = classType,
                BasePrice = basePrice,
                TotalPrice = basePrice * numAdults,
                // Pre-fill with logged-in user information
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone
            };

            return View(model);
        }

        // POST: Reservation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel model)
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Please login to book a flight.";
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get the logged-in user
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            // Create or get schedule
            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.FlightID == model.FlightID && s.Date == model.TravelDate);

            if (schedule == null)
            {
                schedule = new Schedule
                {
                    FlightID = model.FlightID,
                    Date = model.TravelDate,
                    Status = "Scheduled"
                };
                _context.Schedules.Add(schedule);
                await _context.SaveChangesAsync();
            }

            // Create reservation
            var reservation = new Reservation
            {
                UserID = user.UserID,
                FlightID = model.FlightID,
                ScheduleID = schedule.ScheduleID,
                BookingDate = DateOnly.FromDateTime(DateTime.Now),
                TravelDate = model.TravelDate,
                Status = "Pending",
                NumAdults = model.NumAdults,
                NumChildren = model.NumChildren,
                NumSeniors = model.NumSeniors,
                Class = model.Class,
                ConfirmationNumber = GenerateConfirmationNumber(),
                BlockingNumber = GenerateBlockingNumber()
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Confirmation), new { id = reservation.ReservationID });
        }

        // GET: Reservation/Confirmation/5
        public async Task<IActionResult> Confirmation(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Flight)
                    .ThenInclude(f => f.OriginCity)
                .Include(r => r.Flight)
                    .ThenInclude(f => f.DestinationCity)
                .Include(r => r.Schedule)
                .FirstOrDefaultAsync(m => m.ReservationID == id);

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // GET: Reservation/MyReservations
        public async Task<IActionResult> MyReservations()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Please login to view your reservations.";
                return RedirectToAction("Login", "Account", new { returnUrl = "/Reservation/MyReservations" });
            }

            var reservations = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Flight)
                    .ThenInclude(f => f.OriginCity)
                .Include(r => r.Flight)
                    .ThenInclude(f => f.DestinationCity)
                .Include(r => r.Schedule)
                .Where(r => r.UserID == userId)
                .OrderByDescending(r => r.BookingDate)
                .ToListAsync();

            return View(reservations);
        }

        // GET: Reservation/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Please login to view reservation details.";
                return RedirectToAction("Login", "Account");
            }

            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Flight)
                    .ThenInclude(f => f.OriginCity)
                .Include(r => r.Flight)
                    .ThenInclude(f => f.DestinationCity)
                .Include(r => r.Schedule)
                .Include(r => r.Payments)
                .Include(r => r.Refunds)
                .FirstOrDefaultAsync(m => m.ReservationID == id);

            if (reservation == null)
            {
                return NotFound();
            }

            // Verify the reservation belongs to the logged-in user
            if (reservation.UserID != userId)
            {
                return Forbid();
            }

            return View(reservation);
        }

        private string GenerateConfirmationNumber()
        {
            return $"ARS{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }

        private string GenerateBlockingNumber()
        {
            return $"BLK{DateTime.Now:yyyyMMddHHmmss}";
        }
    }
}
