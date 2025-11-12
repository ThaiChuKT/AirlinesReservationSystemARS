using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ARS.Data;
using ARS.Models;
using ARS.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ARS.Controllers
{
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ReservationController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reservation/Create
        public async Task<IActionResult> Create(int flightId, int? scheduleId, DateOnly travelDate, int numAdults = 1, string classType = "Economy")
        {
            // Check if user is logged in via Identity
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
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

            // Use currentUser from Identity
            var user = currentUser;

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
            // Check if user is logged in via Identity
            var currentUserPost = await _userManager.GetUserAsync(User);
            if (currentUserPost == null)
            {
                TempData["ErrorMessage"] = "Please login to book a flight.";
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Use the Identity user
            var user = currentUserPost;

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
                UserID = user.Id,
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
            // Ensure a legacy Users row exists for compatibility with existing FK in the database.
            // Some databases have both AspNetUsers (Identity) and a legacy Users table. The Reservations
            // table in some deployments enforces a FK to the legacy Users table as well, which will
            // cause inserts to fail unless a corresponding Users row exists. Insert a lightweight
            // legacy record if missing. Use an idempotent INSERT ... SELECT ... WHERE NOT EXISTS pattern.
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO `Users` (`UserID`, `FirstName`, `LastName`, `Email`, `Password`, `Phone`, `Address`, `Gender`, `Age`, `CreditCardNumber`, `SkyMiles`, `Role`)
SELECT {user.Id}, {user.FirstName}, {user.LastName}, {user.Email}, '', {user.Phone}, {user.Address}, {user.Gender.ToString()}, {user.Age}, {user.CreditCardNumber}, {user.SkyMiles}, 'Customer'
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM `Users` WHERE `UserID` = {user.Id});
" );
            }
            catch
            {
                // If this insert fails (e.g. legacy Users table not present) continue and rely on the
                // AspNetUsers FK (the database may not enforce the legacy FK in some environments).
            }

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
            // Check if user is logged in via Identity
            var currentUserList = await _userManager.GetUserAsync(User);
            if (currentUserList == null)
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
                .Where(r => r.UserID == currentUserList.Id)
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

            // Check if user is logged in via Identity
            var currentUserDetails = await _userManager.GetUserAsync(User);
            if (currentUserDetails == null)
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
            if (reservation.UserID != currentUserDetails.Id)
            {
                return Forbid();
            }

            return View(reservation);
        }

        // GET: Reservation/Reschedule/5
        public async Task<IActionResult> Reschedule(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Please login to reschedule a reservation.";
                return RedirectToAction("Login", "Account", new { returnUrl = $"/Reservation/Details/{id}" });
            }

            var reservation = await _context.Reservations
                .Include(r => r.Flight)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation == null) return NotFound();
            if (reservation.UserID != currentUser.Id) return Forbid();

            var vm = new ARS.ViewModels.RescheduleInitViewModel
            {
                Reservation = reservation,
                NewDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1))
            };

            return View(vm);
        }

        // POST: Reservation/RescheduleSearch/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RescheduleSearch(int id, DateOnly newDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Please login to reschedule a reservation.";
                return RedirectToAction("Login", "Account");
            }

            var reservation = await _context.Reservations
                .Include(r => r.Flight)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation == null) return NotFound();
            if (reservation.UserID != currentUser.Id) return Forbid();

            var passengers = reservation.NumAdults + reservation.NumChildren + reservation.NumSeniors;

            // Find candidate flights matching the original route and available on the requested date
            var originCityId = reservation.Flight?.OriginCityID ?? 0;
            var destinationCityId = reservation.Flight?.DestinationCityID ?? 0;

            var flightsQuery = _context.Flights
                .Include(f => f.OriginCity)
                .Include(f => f.DestinationCity)
                .Include(f => f.Schedules)
                .Include(f => f.Reservations)
                .Where(f => f.OriginCityID == originCityId && f.DestinationCityID == destinationCityId);

            var flights = await flightsQuery.ToListAsync();

            var results = new List<ARS.ViewModels.FlightResultItem>();

            foreach (var f in flights)
            {
                var schedule = f.Schedules.FirstOrDefault(s => s.Date == newDate);
                if (schedule == null) continue;

                var bookedSeats = f.Reservations
                    .Where(r => r.TravelDate == newDate && r.Status != "Cancelled")
                    .Sum(r => r.NumAdults + r.NumChildren + r.NumSeniors);

                // If the current reservation occupies seats on the same flight/date, exclude it from the count
                if (reservation.FlightID == f.FlightID && reservation.TravelDate == newDate)
                {
                    bookedSeats -= (reservation.NumAdults + reservation.NumChildren + reservation.NumSeniors);
                }

                var availableSeats = f.TotalSeats - bookedSeats;
                if (availableSeats < passengers) continue;

                // Pricing
                var daysBeforeDeparture = (newDate.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days;
                var timingMultiplier = daysBeforeDeparture switch
                {
                    >= 30 => 0.80m,
                    >= 15 => 1.00m,
                    >= 7 => 1.20m,
                    _ => 1.50m
                };
                var classMultiplier = reservation.Class switch
                {
                    "Business" => 2.0m,
                    "First" => 3.5m,
                    _ => 1.0m
                };
                var basePrice = f.BaseFare;
                var finalPrice = basePrice * classMultiplier * timingMultiplier * passengers;

                results.Add(new ARS.ViewModels.FlightResultItem
                {
                    FlightID = f.FlightID,
                    FlightNumber = f.FlightNumber,
                    OriginCity = f.OriginCity?.CityName ?? "",
                    OriginAirportCode = f.OriginCity?.AirportCode ?? "",
                    DestinationCity = f.DestinationCity?.CityName ?? "",
                    DestinationAirportCode = f.DestinationCity?.AirportCode ?? "",
                    DepartureTime = schedule.Date.ToDateTime(TimeOnly.FromDateTime(f.DepartureTime)),
                    ArrivalTime = schedule.Date.ToDateTime(TimeOnly.FromDateTime(f.ArrivalTime)),
                    Duration = f.Duration,
                    AircraftType = f.AircraftType,
                    AvailableSeats = availableSeats,
                    BasePrice = basePrice,
                    FinalPrice = finalPrice,
                    ScheduleID = schedule.ScheduleID
                });
            }

            var vm = new ARS.ViewModels.RescheduleSearchResultViewModel
            {
                Reservation = reservation,
                NewDate = newDate,
                Passengers = passengers,
                Class = reservation.Class,
                Flights = results
            };

            return View("RescheduleResults", vm);
        }

        // GET: Reservation/ConfirmReschedule?reservationId=1&flightId=2&scheduleId=3&newDate=2025-11-16
        public async Task<IActionResult> ConfirmReschedule(int reservationId, int flightId, int scheduleId, DateOnly newDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var reservation = await _context.Reservations
                .Include(r => r.Payments)
                .Include(r => r.Flight)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null) return NotFound();
            if (reservation.UserID != currentUser.Id) return Forbid();

            var flight = await _context.Flights.FindAsync(flightId);
            if (flight == null) return NotFound();

            // compute new total price
            var passengers = reservation.NumAdults + reservation.NumChildren + reservation.NumSeniors;
            var daysBefore = (newDate.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days;
            var timingMultiplier = daysBefore switch
            {
                >= 30 => 0.80m,
                >= 15 => 1.00m,
                >= 7 => 1.20m,
                _ => 1.50m
            };
            var classMultiplier = reservation.Class switch
            {
                "Business" => 2.0m,
                "First" => 3.5m,
                _ => 1.0m
            };
            var newTotal = Math.Round(flight.BaseFare * classMultiplier * timingMultiplier * passengers, 2);

            var totalPaid = (reservation.Payments ?? Enumerable.Empty<Payment>()).Where(p => p.TransactionStatus == "Completed").Sum(p => p.Amount);
            var difference = Math.Round(newTotal - totalPaid, 2);

            var vm = new ARS.ViewModels.ConfirmRescheduleViewModel
            {
                Reservation = reservation,
                NewFlight = flight,
                NewScheduleID = scheduleId,
                NewDate = newDate,
                NewTotal = newTotal,
                TotalPaid = totalPaid,
                Difference = difference
            };

            return View(vm);
        }

        // POST: Reservation/ConfirmReschedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReschedulePost(int reservationId, int flightId, int scheduleId, DateOnly newDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var reservation = await _context.Reservations
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null) return NotFound();
            if (reservation.UserID != currentUser.Id) return Forbid();

            var flight = await _context.Flights.FindAsync(flightId);
            if (flight == null) return NotFound();

            var passengers = reservation.NumAdults + reservation.NumChildren + reservation.NumSeniors;
            var daysBefore = (newDate.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days;
            var timingMultiplier = daysBefore switch
            {
                >= 30 => 0.80m,
                >= 15 => 1.00m,
                >= 7 => 1.20m,
                _ => 1.50m
            };
            var classMultiplier = reservation.Class switch
            {
                "Business" => 2.0m,
                "First" => 3.5m,
                _ => 1.0m
            };
            var newTotal = Math.Round(flight.BaseFare * classMultiplier * timingMultiplier * passengers, 2);

            var totalPaid = (reservation.Payments ?? Enumerable.Empty<Payment>()).Where(p => p.TransactionStatus == "Completed").Sum(p => p.Amount);
            var difference = Math.Round(newTotal - totalPaid, 2);

            // If difference > 0, create a pending payment record for the balance due
            if (difference > 0)
            {
                var payment = new Payment
                {
                    ReservationID = reservation.ReservationID,
                    Amount = difference,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = "RescheduleDue",
                    TransactionStatus = "Pending",
                    TransactionRefNo = null
                };
                _context.Payments.Add(payment);
            }
            else if (difference < 0)
            {
                // process refund for the overpaid amount
                var refundAmount = Math.Abs(difference);
                var refundPercent = totalPaid > 0 ? Math.Round((refundAmount / totalPaid) * 100m, 2) : 0m;
                var refund = new Refund
                {
                    ReservationID = reservation.ReservationID,
                    RefundAmount = refundAmount,
                    RefundDate = DateTime.Now,
                    RefundPercentage = refundPercent
                };
                _context.Refunds.Add(refund);

                // Mark completed payments as refunded (simple approach)
                foreach (var p in (reservation.Payments ?? Enumerable.Empty<Payment>()).Where(p => p.TransactionStatus == "Completed"))
                {
                    p.TransactionStatus = "Refunded";
                }
            }

            // Update reservation to the new flight/date/schedule
            reservation.FlightID = flightId;
            reservation.ScheduleID = scheduleId;
            reservation.TravelDate = newDate;
            reservation.ConfirmationNumber = GenerateConfirmationNumber();
            reservation.Status = "Rescheduled";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = difference > 0
                ? $"Reservation updated. An additional payment of ${difference:N2} is required to complete the reschedule."
                : difference < 0
                    ? $"Reservation updated. A refund of ${Math.Abs(difference):N2} will be processed."
                    : "Reservation updated. No price difference.";

            return RedirectToAction("Details", new { id = reservationId });
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
