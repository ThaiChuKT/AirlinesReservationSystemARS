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

            // If the client selected a persisted seat id, validate and reserve it
            if (model.SelectedSeatId.HasValue)
            {
                // load flight to validate seat layout ownership
                var flight = await _context.Flights.FindAsync(model.FlightID);
                if (flight == null)
                {
                    ModelState.AddModelError(string.Empty, "Flight not found.");
                    return View(model);
                }

                var seat = await _context.Seats.FindAsync(model.SelectedSeatId.Value);
                if (seat == null)
                {
                    ModelState.AddModelError("SelectedSeat", "Selected seat not found.");
                    return View(model);
                }

                // Ensure the seat belongs to the flight's seat layout (if defined)
                if (flight.SeatLayoutId.HasValue && seat.SeatLayoutId != flight.SeatLayoutId)
                {
                    ModelState.AddModelError("SelectedSeat", "Selected seat is not valid for this flight.");
                    return View(model);
                }

                // Check if the seat is already taken for this schedule (excluding cancelled)
                var seatTaken = await _context.Reservations
                    .AnyAsync(r => r.ScheduleID == schedule.ScheduleID && r.SeatId == seat.SeatId && r.Status != "Cancelled");

                if (seatTaken)
                {
                    ModelState.AddModelError("SelectedSeat", "That seat has just been booked by someone else. Please choose a different seat.");
                    return View(model);
                }

                reservation.SeatId = seat.SeatId;
                reservation.SeatLabel = seat.Label;
            }

            // Fallback: if only a label was provided (legacy), store it
            else if (!string.IsNullOrEmpty(model.SelectedSeat))
            {
                reservation.SeatLabel = model.SelectedSeat;
            }

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

            // Preserve selected seat (if provided) so it can be shown on the confirmation page.
            if (!string.IsNullOrEmpty(model.SelectedSeat))
            {
                TempData["SelectedSeat"] = model.SelectedSeat;
            }

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

        // GET: Reservation/GetSeatMap?flightId=1&travelDate=2025-11-16
        [HttpGet]
        public async Task<IActionResult> GetSeatMap(int flightId, DateOnly travelDate)
        {
            var flight = await _context.Flights
                .FirstOrDefaultAsync(f => f.FlightID == flightId);

            if (flight == null) return NotFound();

            // If the flight references a SeatLayout, use the persisted seats
            if (flight.SeatLayoutId.HasValue)
            {
                var seats = await _context.Seats
                    .Where(s => s.SeatLayoutId == flight.SeatLayoutId.Value)
                    .OrderBy(s => s.RowNumber)
                    .ThenBy(s => s.Column)
                    .ToListAsync();

                // find schedule for the date (if exists)
                var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.FlightID == flightId && s.Date == travelDate);
                var reservedIds = new HashSet<int>();
                if (schedule != null)
                {
                    reservedIds = await _context.Reservations
                        .Where(r => r.ScheduleID == schedule.ScheduleID && r.Status != "Cancelled" && r.SeatId != null)
                        .Select(r => r.SeatId!.Value)
                        .ToHashSetAsync();
                }

                var rowsResult = seats
                    .GroupBy(s => s.RowNumber)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        row = g.Key,
                        seats = g.Select(s => new
                        {
                            id = s.SeatId,
                            label = s.Label,
                            col = s.Column,
                            row = s.RowNumber,
                            cabin = s.CabinClass.ToString(),
                            available = !reservedIds.Contains(s.SeatId)
                        }).ToList()
                    }).ToList();

                // seats per row is variable; include max count
                var seatsPerRow = seats.GroupBy(s => s.RowNumber).Select(g => g.Count()).DefaultIfEmpty(0).Max();
                var rows = rowsResult.Count;
                return Json(new { rows, seatsPerRow, rowsResult });
            }

            // fallback to the legacy heuristic layout if no SeatLayout is configured
            var totalSeats = flight.TotalSeats;

            // Improved layout: 6 seats per row (A-F) with an aisle between C and D.
            var seatsPerRowLegacy = 6;
            var rowsLegacy = (int)Math.Ceiling(totalSeats / (double)seatsPerRowLegacy);

            // Number of booked seats for that date (not including cancelled)
            var bookedCount = _context.Reservations.Where(r => r.FlightID == flightId && r.TravelDate == travelDate && r.Status != "Cancelled").Count();

            // Determine cabin splits: first class (front few rows), business (next), economy (rest)
            var firstRows = Math.Max(1, rowsLegacy / 10); // ~10% front
            var businessRows = Math.Max(1, rowsLegacy / 5); // ~20% next

            var letters = new[] { 'A', 'B', 'C', 'D', 'E', 'F' };
            var allSeats = new List<(string Id, int Row, string Col)>();
            var idx = 0;
            for (var r = 1; r <= rowsLegacy; r++)
            {
                for (var c = 0; c < seatsPerRowLegacy; c++)
                {
                    if (idx >= totalSeats) break;
                    var label = $"{r}{letters[c]}";
                    allSeats.Add((label, r, letters[c].ToString()));
                    idx++;
                }
            }

            // Mark occupied seats starting from the back of the plane (rear-filled)
            var occupiedSet = new HashSet<string>();
            for (int i = 0; i < bookedCount && i < allSeats.Count; i++)
            {
                var s = allSeats[allSeats.Count - 1 - i];
                occupiedSet.Add(s.Id);
            }

            var rowsResultLegacy = new List<object>();
            foreach (var group in allSeats.GroupBy(s => s.Row).OrderBy(g => g.Key))
            {
                var rowNum = group.Key;
                string cabin = "Economy";
                if (rowNum <= firstRows) cabin = "First";
                else if (rowNum <= firstRows + businessRows) cabin = "Business";

                var seatObjs = group.Select(s => new
                {
                    id = s.Id,
                    label = s.Id,
                    col = s.Col,
                    row = s.Row,
                    cabin,
                    available = !occupiedSet.Contains(s.Id)
                }).ToList();

                rowsResultLegacy.Add(new { row = rowNum, seats = seatObjs });
            }

            return Json(new { rows = rowsLegacy, seatsPerRow = seatsPerRowLegacy, rowsResult = rowsResultLegacy });
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
