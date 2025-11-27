using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ARS.Data;
using ARS.Models;
using Microsoft.AspNetCore.Identity;
using ARS.ViewModels;

namespace ARS.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public PaymentController(ApplicationDbContext context, UserManager<User> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        // GET: Payment/Index?reservationId=7
        public async Task<IActionResult> Index(int reservationId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Please login to make a payment.";
                return RedirectToAction("Login", "Account", new { returnUrl = $"/Payment?reservationId={reservationId}" });
            }

            var reservation = await _context.Reservations
                .Include(r => r.Flight)
                    .ThenInclude(f => f!.OriginCity)
                .Include(r => r.Flight)
                    .ThenInclude(f => f!.DestinationCity)
                .Include(r => r.Payments)
                .Include(r => r.Schedule)
                .Include(r => r.Legs)
                    .ThenInclude(l => l.Flight)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null)
            {
                return NotFound();
            }

            // Allow access if user owns reservation OR user is admin
            var isAdmin = User.IsInRole("Admin");
            if (reservation.UserID != currentUser.Id && !isAdmin)
            {
                return Forbid();
            }

            // Calculate amounts
            var totalPaid = reservation.Payments?
                .Where(p => p.TransactionStatus == "Completed")
                .Sum(p => p.Amount) ?? 0;

            var pendingPayments = reservation.Payments?
                .Where(p => p.TransactionStatus == "Pending")
                .ToList() ?? new List<Payment>();

            var amountDue = pendingPayments.Sum(p => p.Amount);

            // If no pending payments exist, create one based on reservation details
            if (amountDue <= 0 && reservation.Status == "Pending")
            {
                // Calculate the price for this reservation
                var flight = reservation.Flight ?? await _context.Flights.FindAsync(reservation.FlightID);
                
                if (flight != null)
                {
                    var passengers = reservation.NumAdults + reservation.NumChildren + reservation.NumSeniors;
                    var travelDate = reservation.TravelDate;
                    var daysBefore = (travelDate.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days;
                    
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
                    
                    // For multi-leg reservations, calculate based on all legs
                    decimal totalPrice = 0;
                    if (reservation.Legs != null && reservation.Legs.Any())
                    {
                        foreach (var leg in reservation.Legs)
                        {
                            var legFlight = leg.Flight ?? await _context.Flights.FindAsync(leg.FlightID);
                            if (legFlight != null)
                            {
                                var legDaysBefore = (leg.TravelDate.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days;
                                var legTimingMultiplier = legDaysBefore switch
                                {
                                    >= 30 => 0.80m,
                                    >= 15 => 1.00m,
                                    >= 7 => 1.20m,
                                    _ => 1.50m
                                };
                                totalPrice += legFlight.BaseFare * classMultiplier * legTimingMultiplier * passengers;
                            }
                        }
                    }
                    else
                    {
                        totalPrice = flight.BaseFare * classMultiplier * timingMultiplier * passengers;
                    }
                    
                    totalPrice = Math.Round(totalPrice, 2);
                    
                    // Create the initial payment record
                    var initialPayment = new Payment
                    {
                        ReservationID = reservation.ReservationID,
                        Amount = totalPrice,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = "Pending",
                        TransactionStatus = "Pending",
                        TransactionRefNo = null
                    };
                    
                    _context.Payments.Add(initialPayment);
                    await _context.SaveChangesAsync();
                    
                    amountDue = totalPrice;
                    pendingPayments = new List<Payment> { initialPayment };
                }
            }

            if (amountDue <= 0)
            {
                TempData["InfoMessage"] = "No pending payment for this reservation.";
                return RedirectToAction("Details", "Reservation", new { id = reservationId });
            }

            var viewModel = new PaymentViewModel
            {
                Reservation = reservation,
                TotalPaid = totalPaid,
                AmountDue = amountDue,
                PendingPayments = pendingPayments,
                PayPalClientId = _configuration["PayPal:ClientId"] ?? ""
            };

            return View(viewModel);
        }

        // POST: Payment/ProcessPayPal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayPal(int reservationId, string orderId, string payerId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var reservation = await _context.Reservations
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null)
            {
                return Json(new { success = false, message = "Reservation not found" });
            }

            // Verify user owns reservation
            if (reservation.UserID != currentUser.Id && !User.IsInRole("Admin"))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            // Get pending payments
            var pendingPayments = reservation.Payments?
                .Where(p => p.TransactionStatus == "Pending")
                .ToList() ?? new List<Payment>();

            if (!pendingPayments.Any())
            {
                return Json(new { success = false, message = "No pending payments" });
            }

            // Update all pending payments to completed
            foreach (var payment in pendingPayments)
            {
                payment.TransactionStatus = "Completed";
                payment.PaymentMethod = "PayPal";
                payment.TransactionRefNo = orderId;
                payment.PaymentDate = DateTime.Now;
            }

            // Update reservation status to Confirmed if it was Pending
            if (reservation.Status == "Pending")
            {
                reservation.Status = "Confirmed";
            }

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "Payment processed successfully",
                redirectUrl = Url.Action("Details", "Reservation", new { id = reservationId })
            });
        }

        // POST: Payment/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int reservationId)
        {
            TempData["InfoMessage"] = "Payment was cancelled.";
            return RedirectToAction("Details", "Reservation", new { id = reservationId });
        }
    }
}
